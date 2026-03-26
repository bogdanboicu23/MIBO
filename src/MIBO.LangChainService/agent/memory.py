from __future__ import annotations

import logging
import os
from datetime import datetime, timezone
from typing import Any

from langchain_core.messages import HumanMessage, SystemMessage
from motor.motor_asyncio import AsyncIOMotorClient, AsyncIOMotorCollection, AsyncIOMotorCursor

from agent.groq_provider import GroqKeyPool, invoke_with_rotation
from agent.prompts import CONTEXT_COMPACTION_SYSTEM_PROMPT
from agent.state import PipelineState
from agent.token_utils import count_tokens, truncate_to_tokens

logger = logging.getLogger(__name__)

_MONGO_CLIENT: AsyncIOMotorClient | None = None


def _utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def _get_setting(*names: str, default: str | None = None) -> str | None:
    for name in names:
        value = os.getenv(name)
        if value and value.strip():
            return value.strip()

    return default


def get_conversations_collection() -> AsyncIOMotorCollection:
    global _MONGO_CLIENT

    if _MONGO_CLIENT is None:
        mongo_uri = _get_setting("MONGO_URI", "Mongo__ConnectionString")
        if not mongo_uri:
            raise RuntimeError("Mongo connection string missing. Set MONGO_URI or Mongo__ConnectionString.")

        _MONGO_CLIENT = AsyncIOMotorClient(mongo_uri)

    database_name = _get_setting("MONGO_DATABASE", "Mongo__Database", default="mibo")
    collection_name = _get_setting(
        "MONGO_CONVERSATIONS_COLLECTION",
        "Mongo__ConversationsCollection",
        default="conversations",
    )
    return _MONGO_CLIENT[database_name][collection_name]


def get_messages_collection() -> AsyncIOMotorCollection:
    global _MONGO_CLIENT

    if _MONGO_CLIENT is None:
        mongo_uri = _get_setting("MONGO_URI", "Mongo__ConnectionString")
        if not mongo_uri:
            raise RuntimeError("Mongo connection string missing. Set MONGO_URI or Mongo__ConnectionString.")

        _MONGO_CLIENT = AsyncIOMotorClient(mongo_uri)

    database_name = _get_setting("MONGO_DATABASE", "Mongo__Database", default="mibo")
    collection_name = _get_setting(
        "MONGO_MESSAGES_COLLECTION",
        "Mongo__MessagesCollection",
        default="messages",
    )
    return _MONGO_CLIENT[database_name][collection_name]


def render_history_for_prompt(messages: list[dict[str, Any]]) -> str:
    if not messages:
        return "No previous conversation."

    return "\n".join(f"{message.get('role', 'unknown')}: {message.get('content', '')}" for message in messages)


def _to_utc_datetime(value: Any) -> datetime | None:
    if isinstance(value, datetime):
        return value.replace(tzinfo=timezone.utc) if value.tzinfo is None else value.astimezone(timezone.utc)

    if isinstance(value, str) and value.strip():
        candidate = value.strip()
        if candidate.endswith("Z"):
            candidate = f"{candidate[:-1]}+00:00"

        try:
            parsed = datetime.fromisoformat(candidate)
        except ValueError:
            return None

        return parsed.replace(tzinfo=timezone.utc) if parsed.tzinfo is None else parsed.astimezone(timezone.utc)

    return None


def _to_utc_iso(value: Any) -> str:
    parsed = _to_utc_datetime(value)
    return parsed.isoformat() if parsed is not None else _utc_now_iso()


def _select_legacy_messages(stored_messages: list[dict[str, Any]], token_budget: int) -> list[dict[str, Any]]:
    selected: list[dict[str, Any]] = []
    consumed_tokens = 0

    for message in reversed(stored_messages):
        content = message.get("content", "")
        message_tokens = int(message.get("tokens_approx") or count_tokens(content))
        if selected and consumed_tokens + message_tokens > token_budget:
            break

        normalized_message = {
            "role": message.get("role", "user"),
            "content": content,
            "timestamp": message.get("timestamp", _utc_now_iso()),
            "tokens_approx": message_tokens,
        }
        selected.append(normalized_message)
        consumed_tokens += message_tokens

    selected.reverse()
    return selected


def _build_message_filter(session_id: str, document: dict[str, Any]) -> dict[str, Any]:
    message_filter: dict[str, Any] = {"conversationId": session_id}
    summary_cutoff = _to_utc_datetime(document.get("summary_last_message_at"))
    if summary_cutoff is not None:
        message_filter["createdAt"] = {"$gt": summary_cutoff}

    return message_filter


async def _select_message_documents(cursor: AsyncIOMotorCursor, token_budget: int) -> list[dict[str, Any]]:
    selected: list[dict[str, Any]] = []
    consumed_tokens = 0

    async for message in cursor:
        content = str(message.get("text") or message.get("content") or "")
        message_tokens = count_tokens(content)
        if selected and consumed_tokens + message_tokens > token_budget:
            break

        selected.append(
            {
                "role": str(message.get("role") or "user"),
                "content": content,
                "timestamp": _to_utc_iso(message.get("createdAt") or message.get("timestamp")),
                "tokens_approx": message_tokens,
            }
        )
        consumed_tokens += message_tokens

    selected.reverse()
    return selected


async def load_conversation_window(session_id: str, token_budget: int = 6000) -> list[dict[str, Any]]:
    collection = get_conversations_collection()
    document = await collection.find_one({"_id": session_id}) or {}

    messages = await _select_message_documents(
        get_messages_collection()
        .find(_build_message_filter(session_id, document))
        .sort("createdAt", -1),
        token_budget,
    )

    if not messages:
        messages = _select_legacy_messages(document.get("messages", []), token_budget)

    summary = (document.get("summary") or "").strip()
    if summary:
        messages = [
            {
                "role": "system",
                "content": summary,
                "timestamp": document.get("updated_at", _utc_now_iso()),
                "tokens_approx": count_tokens(summary),
            },
            *messages,
        ]

    return messages


async def memory_loader_node(state: PipelineState) -> PipelineState:
    state["conversation_history"] = await load_conversation_window(state["session_id"])
    return state


async def memory_saver_node(state: PipelineState) -> PipelineState:
    # ConversationService already persists user and assistant messages in the MIBO schema.
    # The agent only manages context compaction metadata on the conversation document.
    return state


async def _summarise_or_truncate(prompt_text: str) -> str:
    """Use the Groq key pool to summarise messages; fall back to truncation
    when no keys are configured or all keys are exhausted."""
    if GroqKeyPool().size == 0:
        return truncate_to_tokens(prompt_text, 800)
    try:
        response = await invoke_with_rotation(
            [
                SystemMessage(content=CONTEXT_COMPACTION_SYSTEM_PROMPT),
                HumanMessage(content=prompt_text),
            ]
        )
        return str(response.content).strip()
    except Exception:
        logger.warning("All Groq keys failed during context compaction — falling back to truncation.")
        return truncate_to_tokens(prompt_text, 800)


async def compact_context(session_id: str) -> list[dict[str, Any]]:
    conversations_collection = get_conversations_collection()
    document = await conversations_collection.find_one({"_id": session_id})
    if not document:
        return []

    messages_collection = get_messages_collection()
    live_messages = await messages_collection.find(_build_message_filter(session_id, document)).sort("createdAt", 1).to_list(length=400)

    if len(live_messages) >= 4:
        midpoint = max(1, len(live_messages) // 2)
        older_messages = live_messages[:midpoint]
        cutoff = older_messages[-1].get("createdAt")

        existing_summary = (document.get("summary") or "").strip()
        prompt_parts: list[str] = []
        if existing_summary:
            prompt_parts.append(f"Existing summary:\n{existing_summary}")

        prompt_parts.append("Messages to compress:")
        prompt_parts.extend(
            f"{item.get('role', 'unknown')}: {item.get('text', '') or item.get('content', '')}"
            for item in older_messages
        )

        prompt_text = truncate_to_tokens("\n".join(prompt_parts), 6000)

        summary = await _summarise_or_truncate(prompt_text)

        await conversations_collection.update_one(
            {"_id": session_id},
            {
                "$set": {
                    "summary": summary,
                    "summary_last_message_at": _to_utc_iso(cutoff),
                    "updated_at": _utc_now_iso(),
                }
            },
        )

        return await load_conversation_window(session_id)

    stored_messages: list[dict[str, Any]] = document.get("messages", [])
    if len(stored_messages) < 4:
        return await load_conversation_window(session_id)

    midpoint = max(1, len(stored_messages) // 2)
    older_messages = stored_messages[:midpoint]
    newer_messages = stored_messages[midpoint:]

    existing_summary = (document.get("summary") or "").strip()
    prompt_parts: list[str] = []
    if existing_summary:
        prompt_parts.append(f"Existing summary:\n{existing_summary}")

    prompt_parts.append("Messages to compress:")
    prompt_parts.extend(f"{item.get('role', 'unknown')}: {item.get('content', '')}" for item in older_messages)

    prompt_text = truncate_to_tokens("\n".join(prompt_parts), 6000)

    summary = await _summarise_or_truncate(prompt_text)

    await conversations_collection.update_one(
        {"_id": session_id},
        {
            "$set": {
                "summary": summary,
                "messages": newer_messages,
                "updated_at": _utc_now_iso(),
            }
        },
    )

    return await load_conversation_window(session_id)
