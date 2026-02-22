# syntax=docker/dockerfile:1.6
FROM python:3.11-slim AS runtime

# --- Runtime env ---
ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    PORT=8088

WORKDIR /app

# --- System deps (minim) ---
# wget -> util pentru HEALTHCHECK (și îți ține compatibilitatea cu compose-ul tău)
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    wget \
  && rm -rf /var/lib/apt/lists/*

# --- Dependencies (layer cache) ---
COPY requirements.txt /app/requirements.txt
RUN python -m pip install --upgrade pip \
 && python -m pip install -r /app/requirements.txt

# --- App code ---
COPY . /app

# --- Security: non-root user ---
RUN useradd -m -u 10001 appuser \
 && chown -R appuser:appuser /app
USER appuser

EXPOSE 8088

# Optional: container-level healthcheck (K8s probes tot sunt recomandate)
HEALTHCHECK --interval=10s --timeout=5s --retries=10 \
  CMD wget -qO- http://127.0.0.1:${PORT}/healthz || exit 1

# --- Run FastAPI ---
# ai/main.py -> "ai.main:app"
CMD ["sh", "-c", "python -m uvicorn ai.main:app --host 0.0.0.0 --port ${PORT}"]