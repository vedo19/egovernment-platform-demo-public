#!/bin/bash

# Local Development Setup Script
# This script initializes the development environment and runs database migrations
# Usage: ./scripts/setup-dev.sh

set -e

echo "╔════════════════════════════════════════════════════════════╗"
echo "║     e-Government Platform - Local Development Setup        ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Check prerequisites
echo -e "${BLUE}[1/5] Checking prerequisites...${NC}"
if ! command -v docker &> /dev/null; then
    echo -e "${RED}✗ Docker not found. Please install Docker Desktop.${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Docker found${NC}"

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}✗ .NET SDK not found. Please install .NET SDK 10.0${NC}"
    exit 1
fi
echo -e "${GREEN}✓ .NET SDK found${NC}"

# Create .env.local if not exists
echo ""
echo -e "${BLUE}[2/5] Setting up environment file...${NC}"
if [ ! -f .env.local ]; then
    echo "Creating .env.local from template..."
    cp .env.example .env.local
    echo -e "${GREEN}✓ Created .env.local${NC}"
else
    echo -e "${YELLOW}→ .env.local already exists${NC}"
fi

# Start Docker services
echo ""
echo -e "${BLUE}[3/5] Starting Docker services...${NC}"
echo "Starting databases and services (this may take 30-60 seconds)..."
docker-compose up -d --build

echo "Waiting for databases to be ready..."
sleep 10

# Verify services are running
echo -e "${BLUE}[4/5] Verifying services...${NC}"
docker-compose ps

# Run migrations
echo ""
echo -e "${BLUE}[5/5] Running database migrations...${NC}"

SERVICES=("AuthService" "CitizenService" "ServiceRequestService" "DocumentService")

for service in "${SERVICES[@]}"; do
    echo ""
    echo "Migrating $service database..."
    cd "src/$service"
    
    if dotnet ef database update --no-build 2>/dev/null; then
        echo -e "${GREEN}✓ $service database migrated${NC}"
    else
        echo -e "${YELLOW}→ Retrying $service migration...${NC}"
        dotnet build > /dev/null 2>&1
        dotnet ef database update
        echo -e "${GREEN}✓ $service database migrated${NC}"
    fi
    
    cd ../../
done

# Special handling for tracking database URL
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5434;Database=request_db;Username=postgres;Password=postgres_dev"

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo -e "${GREEN}      ✓ Development environment is ready!          ${NC}"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "Access your services:"
echo "  • Frontend:        http://localhost:3000"
echo "  • API Gateway:     http://localhost:5050"
echo "  • Auth Service:    http://localhost:5001"
echo "  • Citizen Service: http://localhost:5002"
echo "  • Request Service: http://localhost:5003"
echo "  • Document Service:http://localhost:5004"
echo ""
echo "Database connections:"
echo "  • Auth DB:     localhost:5436 (postgres/postgres_dev)"
echo "  • Citizen DB:  localhost:5433 (postgres/postgres_dev)"
echo "  • Request DB:  localhost:5434 (postgres/postgres_dev)"
echo "  • Document DB: localhost:5435 (postgres/postgres_dev)"
echo ""
echo "Useful commands:"
echo "  • View logs:       docker-compose logs -f"
echo "  • Restart service: docker-compose restart <service-name>"
echo "  • Stop all:        docker-compose down"
echo "  • Stop + remove:   docker-compose down -v"
echo ""
echo "For more help, see LOCAL_DEVELOPMENT.md"
echo ""
