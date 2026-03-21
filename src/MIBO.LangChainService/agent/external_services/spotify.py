from __future__ import annotations

from functools import lru_cache
from typing import Any

from agent.external_services.action_service import get_action_service_client
from agent.external_services.base import ActionServiceQueryDefinition, SupportsActionQueries

SPOTIFY_SEARCH_QUERY = ActionServiceQueryDefinition(
    data_source_id="spotify.search",
    handler="spotify.search",
)
SPOTIFY_NOW_PLAYING_QUERY = ActionServiceQueryDefinition(
    data_source_id="spotify.now_playing",
    handler="spotify.now_playing",
)
SPOTIFY_PLAYLISTS_QUERY = ActionServiceQueryDefinition(
    data_source_id="spotify.playlists",
    handler="spotify.playlists",
)
SPOTIFY_TOP_TRACKS_QUERY = ActionServiceQueryDefinition(
    data_source_id="spotify.top_tracks",
    handler="spotify.top_tracks",
)
SPOTIFY_TOP_ARTISTS_QUERY = ActionServiceQueryDefinition(
    data_source_id="spotify.top_artists",
    handler="spotify.top_artists",
)


class SpotifyService:
    def __init__(self, action_queries: SupportsActionQueries) -> None:
        self._action_queries = action_queries

    async def search(self, query: str, type: str = "track", limit: int = 10, user_id: str = "") -> dict[str, Any]:
        result = await self._action_queries.query(
            SPOTIFY_SEARCH_QUERY,
            args={"query": query, "type": type, "limit": limit, "user_id": user_id},
        )
        return result if isinstance(result, dict) else {}

    async def now_playing(self, user_id: str = "") -> dict[str, Any]:
        result = await self._action_queries.query(
            SPOTIFY_NOW_PLAYING_QUERY,
            args={"user_id": user_id},
        )
        return result if isinstance(result, dict) else {}

    async def playlists(self, limit: int = 20, user_id: str = "") -> dict[str, Any]:
        result = await self._action_queries.query(
            SPOTIFY_PLAYLISTS_QUERY,
            args={"limit": limit, "user_id": user_id},
        )
        return result if isinstance(result, dict) else {}

    async def top_tracks(self, time_range: str = "medium_term", limit: int = 10, user_id: str = "") -> dict[str, Any]:
        result = await self._action_queries.query(
            SPOTIFY_TOP_TRACKS_QUERY,
            args={"time_range": time_range, "limit": limit, "user_id": user_id},
        )
        return result if isinstance(result, dict) else {}

    async def top_artists(self, time_range: str = "medium_term", limit: int = 10, user_id: str = "") -> dict[str, Any]:
        result = await self._action_queries.query(
            SPOTIFY_TOP_ARTISTS_QUERY,
            args={"time_range": time_range, "limit": limit, "user_id": user_id},
        )
        return result if isinstance(result, dict) else {}


@lru_cache(maxsize=1)
def get_spotify_service() -> SpotifyService:
    return SpotifyService(get_action_service_client())
