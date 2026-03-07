from __future__ import annotations

from typing import Any, Dict, List

import httpx

from config import settings


def fetch_external_context(conversation_context: Dict[str, Any]) -> List[Dict[str, Any]]:
    urls = conversation_context.get("externalContextUrls", [])
    if not isinstance(urls, list) or not urls:
        return []

    allowed_hosts = {h.strip().lower() for h in settings.EXTERNAL_CONTEXT_ALLOWED_HOSTS if h.strip()}
    snippets: List[Dict[str, Any]] = []

    for raw_url in urls[: settings.EXTERNAL_CONTEXT_MAX_URLS]:
        if not isinstance(raw_url, str) or not raw_url.startswith(("http://", "https://")):
            continue

        host = raw_url.split("://", 1)[1].split("/", 1)[0].lower()
        if allowed_hosts and host not in allowed_hosts:
            continue

        try:
            with httpx.Client(timeout=settings.EXTERNAL_CONTEXT_TIMEOUT_SECONDS) as client:
                resp = client.get(raw_url)
            if resp.status_code >= 400:
                continue
            snippets.append(
                {
                    "url": raw_url,
                    "status": resp.status_code,
                    "excerpt": resp.text.strip()[: settings.EXTERNAL_CONTEXT_MAX_CHARS],
                }
            )
        except Exception:
            continue

    return snippets
