from __future__ import annotations

import asyncio
import contextlib
import json
import logging
from uuid import uuid4

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import StreamingResponse

from agent.graph import build_graph
from models.schemas import ChatRequest

logger = logging.getLogger(__name__)
app = FastAPI(title="Agent Service", version="1.0.0")
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

workflow = build_graph()


@app.post("/chat")
async def chat(request: ChatRequest) -> StreamingResponse:
    if not request.message.strip():
        raise HTTPException(status_code=400, detail="Message is required.")

    session_id = request.session_id.strip() or str(uuid4())
    queue: asyncio.Queue[str | None] = asyncio.Queue()

    async def emit(event: dict) -> None:
        payload = json.dumps(event, ensure_ascii=False)
        await queue.put(f"data: {payload}\n\n")

    async def run_pipeline() -> None:
        try:
            if not request.session_id.strip():
                await emit({"type": "session", "session_id": session_id})

            initial_state = {
                "session_id": session_id,
                "user_message": request.message,
                "conversation_history": [],
                "intent": {},
                "plan": {},
                "tool_results": [],
                "final_response": {},
                "stream_callback": emit,
            }

            result = await workflow.ainvoke(initial_state)
            await emit({"type": "done", "content": json.dumps(result.get("final_response", {}), ensure_ascii=False)})
        except Exception as exc:  # pragma: no cover
            logger.exception("Pipeline execution failed")
            fallback_response = {
                "text": "I ran into an error while processing your request.",
                "components": [],
            }
            await emit({"type": "done", "content": json.dumps(fallback_response, ensure_ascii=False)})
        finally:
            await queue.put(None)

    async def event_stream():
        task = asyncio.create_task(run_pipeline())
        try:
            while True:
                item = await queue.get()
                if item is None:
                    break
                yield item
        except asyncio.CancelledError:  # pragma: no cover
            task.cancel()
            raise
        finally:
            if not task.done():
                task.cancel()
                with contextlib.suppress(asyncio.CancelledError):
                    await task

    return StreamingResponse(
        event_stream(),
        media_type="text/event-stream",
        headers={
            "Cache-Control": "no-cache",
            "Connection": "keep-alive",
            "X-Accel-Buffering": "no",
        },
    )
