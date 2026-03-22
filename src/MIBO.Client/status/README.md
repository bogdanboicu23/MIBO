# MIBO Status

Status frontend for platform health, external services and audit history.

## Local development

```bash
npm install
npm run dev
```

Optional environment variables:

- `VITE_API_SERVER_URL` default: `http://localhost:8080`

## Production

- Public host: `https://status.mibo.monster`
- API origin: `https://api.mibo.monster`
- Docker build accepts `VITE_API_SERVER_URL` and falls back to the production API host.

Deployment is handled by the GitHub Actions workflow in `.github/workflows/deploy-status.yml`.
