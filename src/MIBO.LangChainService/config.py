from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_prefix="", extra="ignore")

    app_name: str = "mibo-langchain-planner"
    # Groq
    GROQ_API_KEY: str
    GROQ_MODEL: str = "llama-3.3-70b-versatile"
    GROQ_TEMPERATURE: float = 0.2
    GROQ_TIMEOUT_SECONDS: int = 20
    GROQ_MAX_TOKENS: int = 1200

    # Planner behavior
    PLANNER_MAX_STEPS: int = 8
    PLANNER_REPAIR_ATTEMPTS: int = 2  # how many times we ask model to repair invalid JSON
    PLANNER_STRICT_JSON: bool = True  # enforce JSON-only

    # Server
    PORT: int = 8088


settings = Settings()