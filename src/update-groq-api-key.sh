#!/usr/bin/env bash

set -euo pipefail

namespace="test"
secret_name="langchain-service-secrets"
deployment_name="langchain-service"
rollout_timeout="180s"
new_value=""

usage() {
  cat <<'EOF'
Usage:
  bash src/update-groq-api-key.sh [options]

Options:
  -n, --namespace <name>   Kubernetes namespace. Default: test
  -s, --secret <name>      Secret name. Default: langchain-service-secrets
  -d, --deployment <name>  Deployment to restart. Default: langchain-service
  -t, --timeout <value>    Rollout timeout. Default: 180s
  -v, --value <value>      Comma-separated API keys. Prefer the secure prompt.
  -h, --help               Show this help.

Examples:
  # Interactive prompt (recommended)
  bash src/update-groq-api-key.sh

  # Pass keys directly
  bash src/update-groq-api-key.sh -v "gsk_key1,gsk_key2,gsk_key3"

This script sets GROQ_API_KEYS (comma-separated) in the secret, removes the
old single GROQ_API_KEY entry, restarts the deployment, and waits for rollout.
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
  printf "Enter comma-separated GROQ API keys (e.g. gsk_key1,gsk_key2): " >&2
  read -r -s new_value
  printf "\n" >&2
fi

if [[ -z "$new_value" ]]; then
  echo "Value cannot be empty." >&2
  exit 1
fi

# Count keys for confirmation
key_count=$(echo "$new_value" | tr ',' '\n' | grep -c .)
echo "Detected $key_count API key(s)."

if ! kubectl get secret "$secret_name" -n "$namespace" >/dev/null 2>&1; then
  echo "Secret $namespace/$secret_name was not found." >&2
  exit 1
fi

if ! kubectl get deployment "$deployment_name" -n "$namespace" >/dev/null 2>&1; then
  echo "Deployment $namespace/$deployment_name was not found." >&2
  exit 1
fi

encoded_value="$(printf '%s' "$new_value" | base64 | tr -d '\n')"

# Set GROQ_API_KEYS and remove the old single-key GROQ_API_KEY entry
patch_payload="$(printf '{"data":{"GROQ_API_KEYS":"%s","GROQ_API_KEY":null}}' "$encoded_value")"

echo "Patching secret $namespace/$secret_name ..."
kubectl patch secret "$secret_name" \
  -n "$namespace" \
  --type merge \
  -p "$patch_payload" >/dev/null

echo "Restarting deployment $namespace/$deployment_name ..."
kubectl rollout restart "deployment/$deployment_name" -n "$namespace" >/dev/null

echo "Waiting for rollout to finish ..."
kubectl rollout status "deployment/$deployment_name" -n "$namespace" --timeout "$rollout_timeout"

current_pod="$(kubectl get pods -n "$namespace" -l "app=$deployment_name" -o jsonpath='{.items[0].metadata.name}' 2>/dev/null || echo unknown)"

echo ""
echo "Done."
echo "  Secret:     $namespace/$secret_name"
echo "  Key:        GROQ_API_KEYS ($key_count key(s))"
echo "  Deployment: $namespace/$deployment_name"
echo "  Pod:        $current_pod"
