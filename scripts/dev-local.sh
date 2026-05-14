#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="${1:-$ROOT_DIR/.env.local}"

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

if [[ -f "$ENV_FILE" ]]; then
  load_env_file "$ENV_FILE"
fi

# Local development defaults (Dockerized PostgreSQL)
: "${POSTGRES_PASSWORD:=postgres_dev}"
: "${JWT_SECRET_KEY:=dev_only_change_me_please_minimum_32_chars}"
: "${ADMIN_EMAIL:=admin@government.gov}"
: "${ADMIN_PASSWORD:=Admin123!}"
: "${ADMIN_FULLNAME:=System Administrator}"

: "${AuthDb__Host:=auth_db}"
: "${AuthDb__Port:=5432}"
: "${AuthDb__Database:=auth_db}"
: "${AuthDb__Username:=postgres}"
: "${AuthDb__Password:=$POSTGRES_PASSWORD}"
: "${AuthDb__SslMode:=Disable}"

: "${CitizenDb__Host:=citizen_db}"
: "${CitizenDb__Port:=5432}"
: "${CitizenDb__Database:=citizen_db}"
: "${CitizenDb__Username:=postgres}"
: "${CitizenDb__Password:=$POSTGRES_PASSWORD}"
: "${CitizenDb__SslMode:=Disable}"

: "${RequestDb__Host:=request_db}"
: "${RequestDb__Port:=5432}"
: "${RequestDb__Database:=request_db}"
: "${RequestDb__Username:=postgres}"
: "${RequestDb__Password:=$POSTGRES_PASSWORD}"
: "${RequestDb__SslMode:=Disable}"

: "${DocumentDb__Host:=document_db}"
: "${DocumentDb__Port:=5432}"
: "${DocumentDb__Database:=document_db}"
: "${DocumentDb__Username:=postgres}"
: "${DocumentDb__Password:=$POSTGRES_PASSWORD}"
: "${DocumentDb__SslMode:=Disable}"

# Keep local gateway routing inside the docker network
: "${AuthService__Url:=http://auth_service:8080}"
: "${CitizenService__Url:=http://citizen_service:8080}"
: "${ServiceRequestService__Url:=http://service_request_service:8080}"
: "${DocumentService__Url:=http://document_service:8080}"
: "${Cors__AllowedOrigins__0:=http://localhost:3000}"
: "${Cors__AllowedOrigins__1:=http://localhost:5173}"

cd "$ROOT_DIR"

echo ""
echo "============================================================"
echo "Mode: LOCAL Docker DBs"
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

echo "Starting local development stack (local Docker databases)..."
docker compose up -d --build \
  auth_db citizen_db request_db document_db \
  auth_service citizen_service service_request_service document_service api_gateway

echo
echo "Local backend stack is running:"
echo "  - API Gateway: http://localhost:5050"
echo "  - Auth Service: http://localhost:5001"
echo "  - Citizen Service: http://localhost:5002"
echo "  - Service Request Service: http://localhost:5003"
echo "  - Document Service: http://localhost:5004"
echo
echo "Frontend recommendation for local dev (hot reload):"
echo "  cd src/frontend && npm ci && npm run dev"
echo "  (opens at http://localhost:5173)"
