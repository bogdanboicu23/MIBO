# syntax=docker/dockerfile:1.6
FROM python:3.11-slim AS runtime

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_NO_CACHE_DIR=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1 \
    CONTEXT_CONFIG_DIR=/app/config \
    PORT=8088

WORKDIR /app

# system deps (wget doar pentru HEALTHCHECK)
RUN apt-get update && apt-get install -y --no-install-recommends \
    ca-certificates \
    wget \
  && rm -rf /var/lib/apt/lists/*

# deps first (cache layer)
COPY requirements.txt /app/requirements.txt
RUN python -m pip install --upgrade pip \
 && python -m pip install -r /app/requirements.txt

# app code (+ bundled base context config for planner)
COPY . /app

# non-root
RUN useradd -m -u 10001 appuser \
 && chown -R appuser:appuser /app
USER appuser

EXPOSE 8088

# container healthcheck (K8s probes are still recommended)
HEALTHCHECK --interval=10s --timeout=5s --retries=10 \
  CMD wget -qO- http://127.0.0.1:${PORT}/langchain/healthz || exit 1

# IMPORTANT: main.py is at /app/main.py => "main:app"
CMD ["sh", "-c", "python -m uvicorn main:app --host 0.0.0.0 --port ${PORT} --proxy-headers"]
