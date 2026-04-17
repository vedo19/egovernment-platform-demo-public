#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${1:-$ROOT_DIR/.env.remote}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing env file: $ENV_FILE"
  echo "Create it from template: scripts/env.remote.example"
  exit 1
fi

load_env_file() {
  local file="$1"
  local line key value var

  while IFS= read -r line || [[ -n "$line" ]]; do
    line="${line%$'\r'}"
    [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue

    if [[ "$line" == export[[:space:]]* ]]; then
      line="${line#export }"
    fi

    [[ "$line" != *=* ]] && continue

    key="${line%%=*}"
    value="${line#*=}"

    # Trim whitespace around key.
    key="${key#"${key%%[![:space:]]*}"}"
    key="${key%"${key##*[![:space:]]}"}"

    [[ "$key" =~ ^[A-Za-z_][A-Za-z0-9_]*$ ]] || continue

    # Expand ${VAR} references using already exported values.
    while [[ "$value" =~ \$\{([A-Za-z_][A-Za-z0-9_]*)\} ]]; do
      var="${BASH_REMATCH[1]}"
      value="${value//\$\{$var\}/${!var:-}}"
    done

    export "$key=$value"
  done < "$file"
}

load_env_file "$ENV_FILE"

require_var() {
  local name="$1"
  if [[ -z "${!name:-}" ]]; then
    echo "Missing required variable: $name"
    exit 1
  fi
}

validate_remote_host() {
  local host="$1"
  local label="$2"

  # Render internal DB hostnames like dpg-xxxx are only reachable inside Render private network.
  if [[ "$host" =~ ^dpg- ]] && [[ "$host" != *.* ]]; then
    echo "Invalid $label host for local remote mode: '$host'"
    echo "This looks like a Render INTERNAL hostname and is not resolvable from local Docker."
    echo "Use the external/public Render Postgres hostname from the Render DB dashboard."
    exit 1
  fi

  if command -v getent >/dev/null 2>&1; then
    if ! getent ahosts "$host" >/dev/null 2>&1; then
      echo "Cannot resolve $label host from this machine: '$host'"
      echo "Check .env.remote and use a reachable external hostname."
      exit 1
    fi
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

validate_remote_host "$AuthDb__Host" "AuthDb"
validate_remote_host "$CitizenDb__Host" "CitizenDb"
validate_remote_host "$RequestDb__Host" "RequestDb"
validate_remote_host "$DocumentDb__Host" "DocumentDb"

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

echo ""
echo "============================================================"
echo "Mode: REMOTE DBs"
echo "Env file: $ENV_FILE"
echo "Auth DB:     $AuthDb__Host:$AuthDb__Port/$AuthDb__Database (ssl=$AuthDb__SslMode)"
echo "Citizen DB:  $CitizenDb__Host:$CitizenDb__Port/$CitizenDb__Database (ssl=$CitizenDb__SslMode)"
echo "Request DB:  $RequestDb__Host:$RequestDb__Port/$RequestDb__Database (ssl=$RequestDb__SslMode)"
echo "Document DB: $DocumentDb__Host:$DocumentDb__Port/$DocumentDb__Database (ssl=$DocumentDb__SslMode)"
echo "Gateway routes:"
echo "  AuthService:            $AuthService__Url"
echo "  CitizenService:         $CitizenService__Url"
echo "  ServiceRequestService:  $ServiceRequestService__Url"
echo "  DocumentService:        $DocumentService__Url"
echo "============================================================"

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
