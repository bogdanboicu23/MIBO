from agent.tooling.finance import calculate_affordability, get_user_finances
from agent.tooling.news import get_headlines, search_news
from agent.tooling.pomodoro import start_pomodoro
from agent.tooling.products import get_categories, get_product, list_products, search_products
from agent.tooling.spotify import (
    spotify_now_playing,
    spotify_playlists,
    spotify_search,
    spotify_top_artists,
    spotify_top_tracks,
)
from agent.tooling.weather import get_current_weather, get_weather_forecast

ALL_TOOLS = (
    search_products,
    get_product,
    list_products,
    get_categories,
    get_user_finances,
    calculate_affordability,
    get_current_weather,
    get_weather_forecast,
    start_pomodoro,
    spotify_search,
    spotify_now_playing,
    spotify_playlists,
    spotify_top_tracks,
    spotify_top_artists,
    get_headlines,
    search_news,
)

__all__ = [
    "ALL_TOOLS",
    "calculate_affordability",
    "get_categories",
    "get_current_weather",
    "get_headlines",
    "get_product",
    "get_user_finances",
    "get_weather_forecast",
    "list_products",
    "search_news",
    "search_products",
    "spotify_now_playing",
    "spotify_playlists",
    "spotify_search",
    "spotify_top_artists",
    "spotify_top_tracks",
    "start_pomodoro",
]
