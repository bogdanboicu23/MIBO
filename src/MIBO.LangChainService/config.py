from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="", extra="ignore")

    app_name: str = "mibo-langchain-planner"
    # Groq
    GROQ_API_KEY: str
    GROQ_MODEL: str = "openai/gpt-oss-120b"
    GROQ_TEMPERATURE: float = 0.2
    GROQ_TIMEOUT_SECONDS: int = 20
    GROQ_MAX_TOKENS: int = 1200

    # Planner behavior
    PLANNER_MAX_STEPS: int = 8
    PLANNER_REPAIR_ATTEMPTS: int = 2  # how many times we ask model to repair invalid JSON
    PLANNER_STRICT_JSON: bool = True  # enforce JSON-only
    PLANNER_PROMPT_MAX_CHARS: int = 12000
    ENABLE_PLUGIN_INTENT: bool = True
    ENABLE_PLUGIN_POST_PROCESSING: bool = True

    # LangGraph + context
    CONTEXT_CONFIG_DIR: str = "../MIBO.ConversationService/config"
    DEEP_SEARCH_MAX_TOOLS: int = 12
    DEEP_SEARCH_MAX_COMPONENTS: int = 8
    DEEP_SEARCH_MAX_ACTION_ROUTES: int = 8
    ENABLE_DEEP_REASONING: bool = True
    EXTERNAL_CONTEXT_ALLOWED_HOSTS: list[str] = []
    EXTERNAL_CONTEXT_MAX_URLS: int = 3
    EXTERNAL_CONTEXT_TIMEOUT_SECONDS: int = 5
    EXTERNAL_CONTEXT_MAX_CHARS: int = 1200

    # Conversation memory (in-process)
    MEMORY_PROMPT_WINDOW: int = 8
    MEMORY_PLAN_WINDOW: int = 8
    MEMORY_SUMMARY_CHAR_LIMIT: int = 1200

    # Server
    PORT: int = 8088


settings = Settings()
