.PHONY: help setup up down logs restart clean rebuild test dev-local dev-remote-db

# Colors
BLUE := \033[0;34m
GREEN := \033[0;32m
NC := \033[0m # No Color

ENV_LOCAL ?= .env.local
ENV_REMOTE ?= .env.remote
COMPOSE_LOCAL := docker compose --env-file $(ENV_LOCAL)

help: ## Show this help message
	@echo "$(BLUE)e-Government Platform - Development Commands$(NC)"
	@echo ""
	@echo "Setup & Initialization:"
	@echo "  make setup        - Complete setup (env + db migrations)"
	@echo "  make env          - Create $(ENV_LOCAL) from template"
	@echo "  make migrate      - Run database migrations"
	@echo ""
	@echo "Running Services:"
	@echo "  make up           - Start all services (background)"
	@echo "  make up-fg        - Start all services (foreground/logs)"
	@echo "  make down         - Stop all services (keep data)"
	@echo "  make clean        - Stop services and remove data"
	@echo "  make rebuild      - Rebuild all containers"
	@echo ""
	@echo "Monitoring & Debugging:"
	@echo "  make logs         - Show logs from all services"
	@echo "  make logs-<svc>   - Show logs from specific service"
	@echo "  make ps           - Show running containers"
	@echo "  make restart-<svc>- Restart specific service"
	@echo ""
	@echo "Development:"
	@echo "  make test         - Run all .NET tests"
	@echo "  make db-connect   - Connect to auth database (psql)"
	@echo "  make health       - Check service health"
	@echo "  make dev-local    - Start backend stack with local Docker DBs ($(ENV_LOCAL))"
	@echo "  make dev-remote-db- Start backend stack with remote DBs ($(ENV_REMOTE))"
	@echo ""
	@echo "Examples:"
	@echo "  make logs-citizen_service        # View citizen service logs"
	@echo "  make restart-auth_service        # Restart auth service"

## Setup targets
setup: env up migrate ## Complete setup (creates env, starts services, runs migrations)
	@echo "$(GREEN)✓ Development environment ready!$(NC)"

env: ## Create .env.local from template
	@if [ ! -f $(ENV_LOCAL) ]; then \
		cp .env.example $(ENV_LOCAL); \
		echo "$(GREEN)✓ Created $(ENV_LOCAL)$(NC)"; \
	else \
		echo "$(ENV_LOCAL) already exists"; \
	fi

migrate: ## Run database migrations for all services
	@echo "Running migrations..."
	@for service in AuthService CitizenService ServiceRequestService DocumentService; do \
		echo "Migrating $$service..."; \
		cd src/$$service && dotnet ef database update && cd ../..; \
	done
	@echo "$(GREEN)✓ All migrations complete$(NC)"

## Docker Compose targets
up: env ## Start all services in background
	$(COMPOSE_LOCAL) up -d --build
	@echo "$(GREEN)✓ Services started${NC}"
	@echo "View logs with: make logs"

up-fg: env ## Start all services in foreground (shows logs)
	$(COMPOSE_LOCAL) up --build

down: ## Stop all services (keeps volumes/data)
	$(COMPOSE_LOCAL) down
	@echo "$(GREEN)✓ Services stopped (data preserved)$(NC)"

clean: ## Stop services and remove all data
	$(COMPOSE_LOCAL) down -v
	@echo "$(GREEN)✓ All services and data removed$(NC)"

rebuild: env ## Rebuild all Docker images
	$(COMPOSE_LOCAL) up -d --build
	@echo "$(GREEN)✓ Services rebuilt and restarted$(NC)"

## Logging targets
logs: ## Show logs from all services
	$(COMPOSE_LOCAL) logs -f

logs-auth: ## Show auth service logs
	$(COMPOSE_LOCAL) logs -f auth_service

logs-citizen: ## Show citizen service logs
	$(COMPOSE_LOCAL) logs -f citizen_service

logs-request: ## Show service request service logs
	$(COMPOSE_LOCAL) logs -f service_request_service

logs-document: ## Show document service logs
	$(COMPOSE_LOCAL) logs -f document_service

logs-gateway: ## Show API gateway logs
	$(COMPOSE_LOCAL) logs -f api_gateway

logs-frontend: ## Show frontend logs
	$(COMPOSE_LOCAL) logs -f frontend

## Service management targets
ps: ## Show running containers
	$(COMPOSE_LOCAL) ps

restart-auth: ## Restart auth service
	$(COMPOSE_LOCAL) restart auth_service

restart-citizen: ## Restart citizen service
	$(COMPOSE_LOCAL) restart citizen_service

restart-request: ## Restart service request service
	$(COMPOSE_LOCAL) restart service_request_service

restart-document: ## Restart document service
	$(COMPOSE_LOCAL) restart document_service

restart-gateway: ## Restart API gateway
	$(COMPOSE_LOCAL) restart api_gateway

restart-frontend: ## Restart frontend
	$(COMPOSE_LOCAL) restart frontend

## Development targets
test: ## Run all .NET tests
	dotnet test

dev-local: ## Start local backend stack with local Docker DBs
	./scripts/dev-local.sh $(ENV_LOCAL)

dev-remote-db: ## Start local backend stack with remote DBs using .env.remote
	./scripts/dev-remote-db.sh $(ENV_REMOTE)

format: ## Format all code with dotnet format
	dotnet format

db-connect: ## Connect to auth database with psql
	psql -h localhost -p 5436 -U postgres -d auth_db

health: ## Check all service health
	@echo "Checking services..."
	@curl -s http://localhost:5001/health > /dev/null && printf '%b\n' "$(GREEN)✓ Auth Service$(NC)" || printf '%b\n' "✗ Auth Service"
	@curl -s http://localhost:5002/health > /dev/null && printf '%b\n' "$(GREEN)✓ Citizen Service$(NC)" || printf '%b\n' "✗ Citizen Service"
	@curl -s http://localhost:5003/health > /dev/null && printf '%b\n' "$(GREEN)✓ Request Service$(NC)" || printf '%b\n' "✗ Request Service"
	@curl -s http://localhost:5004/health > /dev/null && printf '%b\n' "$(GREEN)✓ Document Service$(NC)" || printf '%b\n' "✗ Document Service"
	@curl -s http://localhost:5050/health > /dev/null && printf '%b\n' "$(GREEN)✓ API Gateway$(NC)" || printf '%b\n' "✗ Gateway"
	@echo ""
	@echo "Frontend: http://localhost:3000"

## Default target
.DEFAULT_GOAL := help
