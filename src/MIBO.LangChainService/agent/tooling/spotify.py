from __future__ import annotations

from typing import Any

from langchain_core.tools import tool

from agent.external_services.spotify import get_spotify_service

spotify_service = get_spotify_service()


@tool
async def spotify_search(query: str, type: str = "track", limit: int = 10, user_id: str = "") -> dict[str, Any]:
    """Search the Spotify catalog for tracks, artists, albums, or playlists."""
    return await spotify_service.search(query=query, type=type, limit=limit, user_id=user_id)


@tool
async def spotify_now_playing(user_id: str = "") -> dict[str, Any]:
    """Get the user's currently playing track on Spotify."""
    return await spotify_service.now_playing(user_id=user_id)


@tool
async def spotify_playlists(limit: int = 20, user_id: str = "") -> dict[str, Any]:
    """Get the user's Spotify playlists."""
    return await spotify_service.playlists(limit=limit, user_id=user_id)


@tool
async def spotify_top_tracks(time_range: str = "medium_term", limit: int = 10, user_id: str = "") -> dict[str, Any]:
    """Get the user's top tracks on Spotify. time_range can be short_term, medium_term, or long_term."""
    return await spotify_service.top_tracks(time_range=time_range, limit=limit, user_id=user_id)


@tool
async def spotify_top_artists(time_range: str = "medium_term", limit: int = 10, user_id: str = "") -> dict[str, Any]:
    """Get the user's top artists on Spotify. time_range can be short_term, medium_term, or long_term."""
    return await spotify_service.top_artists(time_range=time_range, limit=limit, user_id=user_id)
