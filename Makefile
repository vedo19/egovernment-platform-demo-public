.PHONY: help setup up down logs restart clean rebuild test dev-local dev-remote-db

# Colors
BLUE := \033[0;34m
GREEN := \033[0;32m
NC := \033[0m # No Color

help: ## Show this help message
	@echo "$(BLUE)e-Government Platform - Development Commands$(NC)"
	@echo ""
	@echo "Setup & Initialization:"
	@echo "  make setup        - Complete setup (env + db migrations)"
	@echo "  make env          - Create .env.local from template"
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
	@echo "  make dev-local    - Start backend stack with local Docker DBs"
	@echo "  make dev-remote-db- Start backend stack with remote DBs"
	@echo ""
	@echo "Examples:"
	@echo "  make logs-citizen_service        # View citizen service logs"
	@echo "  make restart-auth_service        # Restart auth service"

## Setup targets
setup: env up migrate ## Complete setup (creates env, starts services, runs migrations)
	@echo "$(GREEN)✓ Development environment ready!$(NC)"

env: ## Create .env.local from template
	@if [ ! -f .env.local ]; then \
		cp .env.example .env.local; \
		echo "$(GREEN)✓ Created .env.local$(NC)"; \
	else \
		echo ".env.local already exists"; \
	fi

migrate: ## Run database migrations for all services
	@echo "Running migrations..."
	@for service in AuthService CitizenService ServiceRequestService DocumentService; do \
		echo "Migrating $$service..."; \
		cd src/$$service && dotnet ef database update && cd ../..; \
	done
	@echo "$(GREEN)✓ All migrations complete$(NC)"

## Docker Compose targets
up: ## Start all services in background
	docker-compose up -d --build
	@echo "$(GREEN)✓ Services started${NC}"
	@echo "View logs with: make logs"

up-fg: ## Start all services in foreground (shows logs)
	docker-compose up --build

down: ## Stop all services (keeps volumes/data)
	docker-compose down
	@echo "$(GREEN)✓ Services stopped (data preserved)$(NC)"

clean: ## Stop services and remove all data
	docker-compose down -v
	@echo "$(GREEN)✓ All services and data removed$(NC)"

rebuild: ## Rebuild all Docker images
	docker-compose up -d --build
	@echo "$(GREEN)✓ Services rebuilt and restarted$(NC)"

## Logging targets
logs: ## Show logs from all services
	docker-compose logs -f

logs-auth: ## Show auth service logs
	docker-compose logs -f auth_service

logs-citizen: ## Show citizen service logs
	docker-compose logs -f citizen_service

logs-request: ## Show service request service logs
	docker-compose logs -f service_request_service

logs-document: ## Show document service logs
	docker-compose logs -f document_service

logs-gateway: ## Show API gateway logs
	docker-compose logs -f api_gateway

logs-frontend: ## Show frontend logs
	docker-compose logs -f frontend

## Service management targets
ps: ## Show running containers
	docker-compose ps

restart-auth: ## Restart auth service
	docker-compose restart auth_service

restart-citizen: ## Restart citizen service
	docker-compose restart citizen_service

restart-request: ## Restart service request service
	docker-compose restart service_request_service

restart-document: ## Restart document service
	docker-compose restart document_service

restart-gateway: ## Restart API gateway
	docker-compose restart api_gateway

restart-frontend: ## Restart frontend
	docker-compose restart frontend

## Development targets
test: ## Run all .NET tests
	dotnet test

dev-local: ## Start local backend stack with local Docker DBs
	./scripts/dev-local.sh

dev-remote-db: ## Start local backend stack with remote DBs using .env.remote
	./scripts/dev-remote-db.sh

format: ## Format all code with dotnet format
	dotnet format

db-connect: ## Connect to auth database with psql
	psql -h localhost -p 5436 -U postgres -d auth_db

health: ## Check all service health
	@echo "Checking services..."
	@curl -s http://localhost:5001/health > /dev/null && echo "$(GREEN)✓ Auth Service${NC}" || echo "✗ Auth Service"
	@curl -s http://localhost:5002/health > /dev/null && echo "$(GREEN)✓ Citizen Service${NC}" || echo "✗ Citizen Service"
	@curl -s http://localhost:5003/health > /dev/null && echo "$(GREEN)✓ Request Service${NC}" || echo "✗ Request Service"
	@curl -s http://localhost:5004/health > /dev/null && echo "$(GREEN)✓ Document Service${NC}" || echo "✗ Document Service"
	@curl -s http://localhost:5050/health > /dev/null && echo "$(GREEN)✓ API Gateway${NC}" || echo "✗ Gateway"
	@echo ""
	@echo "Frontend: http://localhost:3000"

## Default target
.DEFAULT_GOAL := help
