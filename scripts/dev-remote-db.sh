#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${1:-$ROOT_DIR/.env.remote}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing env file: $ENV_FILE"
  echo "Create it from template: scripts/env.remote.example"
  exit 1
fi

set -a
# shellcheck disable=SC1090
source "$ENV_FILE"
set +a

require_var() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "Missing required variable: $name"
    exit 1
  fi
}

# Shared settings
require_var JWT_SECRET_KEY
require_var ADMIN_EMAIL
require_var ADMIN_PASSWORD

# Remote DB settings required for each service
for var in \
  AuthDb__Host AuthDb__Port AuthDb__Database AuthDb__Username AuthDb__Password \
  CitizenDb__Host CitizenDb__Port CitizenDb__Database CitizenDb__Username CitizenDb__Password \
  RequestDb__Host RequestDb__Port RequestDb__Database RequestDb__Username RequestDb__Password \
  DocumentDb__Host DocumentDb__Port DocumentDb__Database DocumentDb__Username DocumentDb__Password; do
  require_var "$var"
done

: "${AuthDb__SslMode:=Require}"
: "${CitizenDb__SslMode:=Require}"
: "${RequestDb__SslMode:=Require}"
: "${DocumentDb__SslMode:=Require}"

# Keep gateway routing to local dockerized services in this mode
: "${AuthService__Url:=http://auth_service:8080}"
: "${CitizenService__Url:=http://citizen_service:8080}"
: "${ServiceRequestService__Url:=http://service_request_service:8080}"
: "${DocumentService__Url:=http://document_service:8080}"
: "${Cors__AllowedOrigins__0:=http://localhost:3000}"
: "${Cors__AllowedOrigins__1:=http://localhost:5173}"

cd "$ROOT_DIR"

echo "Starting backend stack with REMOTE databases (no local postgres containers)..."
# --no-deps avoids bringing up local postgres containers due depends_on
# Existing DB containers (if running) are stopped to avoid confusion.
docker compose stop auth_db citizen_db request_db document_db >/dev/null 2>&1 || true
docker compose up -d --build --no-deps \
  auth_service citizen_service service_request_service document_service api_gateway

echo
echo "Remote DB backend stack is running:"
echo "  - API Gateway: http://localhost:5050"
echo "  - Auth Service: http://localhost:5001"
echo "  - Citizen Service: http://localhost:5002"
echo "  - Service Request Service: http://localhost:5003"
echo "  - Document Service: http://localhost:5004"
echo
echo "Frontend recommendation for local dev (hot reload):"
echo "  cd src/frontend && npm ci && npm run dev"
echo "  (opens at http://localhost:5173)"
