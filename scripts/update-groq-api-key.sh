#!/usr/bin/env bash

set -euo pipefail

namespace="test"
secret_name="langchain-service-secrets"
key_name="GROQ_API_KEY"
deployment_name="langchain-service"
rollout_timeout="180s"
new_value="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"

usage() {
  cat <<'EOF'
Usage:
  bash scripts/update-groq-api-key.sh [options]

Options:
  -n, --namespace <name>   Kubernetes namespace. Default: test
  -s, --secret <name>      Secret name. Default: langchain-service-secrets
  -k, --key <name>         Secret data key. Default: GROQ_API_KEY
  -d, --deployment <name>  Deployment to restart. Default: langchain-service
  -t, --timeout <value>    Rollout timeout. Default: 180s
  -v, --value <value>      New secret value. Prefer omitting this and using the secure prompt.
  -h, --help              Show this help.

Examples:
  bash scripts/update-groq-api-key.sh
  GROQ_API_KEY="gsk_..." bash scripts/update-groq-api-key.sh
  bash scripts/update-groq-api-key.sh --namespace test --secret langchain-service-secrets
  bash scripts/update-groq-api-key.sh --deployment langchain-service --timeout 300s

This script patches only one key inside the existing secret, restarts the deployment,
waits for the rollout to finish, and leaves all other secret keys unchanged.
EOF
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -n|--namespace)
      namespace="${2:?Missing value for $1}"
      shift 2
      ;;
    -s|--secret)
      secret_name="${2:?Missing value for $1}"
      shift 2
      ;;
    -k|--key)
      key_name="${2:?Missing value for $1}"
      shift 2
      ;;
    -d|--deployment)
      deployment_name="${2:?Missing value for $1}"
      shift 2
      ;;
    -t|--timeout)
      rollout_timeout="${2:?Missing value for $1}"
      shift 2
      ;;
    -v|--value)
      new_value="${2:?Missing value for $1}"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if ! command -v kubectl >/dev/null 2>&1; then
  echo "kubectl is required but not installed." >&2
  exit 1
fi

if [[ -z "$new_value" ]]; then
  printf "Enter new %s for secret %s/%s: " "$key_name" "$namespace" "$secret_name" >&2
  read -r -s new_value
  printf "\n" >&2
fi

if [[ -z "$new_value" ]]; then
  echo "Secret value cannot be empty." >&2
  exit 1
fi

if ! kubectl get secret "$secret_name" -n "$namespace" >/dev/null 2>&1; then
  echo "Secret $namespace/$secret_name was not found." >&2
  exit 1
fi

if ! kubectl get deployment "$deployment_name" -n "$namespace" >/dev/null 2>&1; then
  echo "Deployment $namespace/$deployment_name was not found." >&2
  exit 1
fi

encoded_value="$(printf '%s' "$new_value" | base64 | tr -d '\n')"
patch_payload="$(printf '{"data":{"%s":"%s"}}' "$key_name" "$encoded_value")"

echo "Patching secret $namespace/$secret_name key $key_name..."
kubectl patch secret "$secret_name" \
  -n "$namespace" \
  --type merge \
  -p "$patch_payload" >/dev/null

echo "Restarting deployment $namespace/$deployment_name..."
kubectl rollout restart "deployment/$deployment_name" -n "$namespace" >/dev/null

echo "Waiting for rollout to finish..."
kubectl rollout status "deployment/$deployment_name" -n "$namespace" --timeout "$rollout_timeout"

current_pod="$(kubectl get pods -n "$namespace" -l "app=$deployment_name" -o jsonpath='{.items[0].metadata.name}')"
current_secret_length="$(kubectl get secret "$secret_name" -n "$namespace" -o jsonpath="{.data.$key_name}" | wc -c | tr -d ' ')"

echo "Done."
echo "Secret updated: $namespace/$secret_name ($key_name)"
echo "Deployment restarted: $namespace/$deployment_name"
echo "Current pod: ${current_pod:-unknown}"
echo "Stored secret payload length: $current_secret_length base64 chars"
echo "Other secret entries were not modified."
