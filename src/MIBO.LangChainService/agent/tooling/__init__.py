from agent.tooling.finance import calculate_affordability, get_user_finances
from agent.tooling.pomodoro import start_pomodoro
from agent.tooling.products import get_categories, get_product, list_products, search_products
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
)

__all__ = [
    "ALL_TOOLS",
    "calculate_affordability",
    "get_categories",
    "get_current_weather",
    "get_product",
    "get_user_finances",
    "get_weather_forecast",
    "list_products",
    "search_products",
    "start_pomodoro",
]
