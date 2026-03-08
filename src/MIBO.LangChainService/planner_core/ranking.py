from __future__ import annotations

import json
import re
from typing import Any, Dict, List


def tokenize(text: str) -> set[str]:
    return {t for t in re.findall(r"[a-zA-Z0-9_\.]+", (text or "").lower()) if len(t) > 1}


def rank_candidates(
    items: List[Dict[str, Any]],
    query: str,
    name_key: str,
    desc_key: str,
    limit: int,
) -> List[Dict[str, Any]]:
    if not items:
        return []

    q_tokens = tokenize(query)
    scored: List[tuple[float, Dict[str, Any]]] = []

    for item in items:
        name = str(item.get(name_key, ""))
        desc = item.get(desc_key, "")
        desc_text = desc if isinstance(desc, str) else json.dumps(desc, ensure_ascii=False)
        text = f"{name} {desc_text}".lower()
        t_tokens = tokenize(text)

        overlap = len(q_tokens.intersection(t_tokens))
        prefix_bonus = 1.0 if any(name.lower().startswith(tok) for tok in q_tokens if tok) else 0.0
        domain_bonus = 1.5 if ("finance" in text and "finance" in query.lower()) else 0.0
        domain_bonus += 1.5 if ("shop" in text and any(k in query.lower() for k in ["product", "buy", "cart"])) else 0.0
        score = float(overlap) + prefix_bonus + domain_bonus
        scored.append((score, item))

    scored.sort(key=lambda x: x[0], reverse=True)
    selected = [x[1] for x in scored[: max(1, limit)] if x[0] > 0]
    if selected:
        return selected

    return items[: max(1, limit)]
