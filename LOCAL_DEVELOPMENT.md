# Local Development Setup Guide

This guide helps team members set up and run the entire e-Government platform locally on their machine.

## Prerequisites

- **Docker Desktop** (includes Docker & Docker Compose)
  - [Download for Windows/Mac](https://www.docker.com/products/docker-desktop)
  - [Install on Linux](https://docs.docker.com/engine/install/ubuntu/)
- **.NET SDK 10.0** - [Download](https://dotnet.microsoft.com/download)
- **Node.js 18+** - [Download](https://nodejs.org/)
- **Git**

## Quick Start (5 minutes)

```bash
# 1. Clone and enter the repository
git clone <repo-url>
cd egovernment-platform-demo-public

# 2. Create local environment file
cp .env.example .env.local

# 3. Start all services with Docker Compose
docker-compose up -d

# 4. Run database migrations (first time only)
./scripts/setup-dev.sh

# Done! Services are running
```

## What Gets Started

| Service | URL | Port | Purpose |
|---------|-----|------|---------|
| **API Gateway** | http://localhost:5050 | 5050 | Main API entry point |
| **Auth Service** | http://localhost:5001 | 5001 | User authentication |
| **Citizen Service** | http://localhost:5002 | 5002 | Citizen profile management |
| **Service Request Service** | http://localhost:5003 | 5003 | Request workflow |
| **Document Service** | http://localhost:5004 | 5004 | Document handling |
| **Frontend** | http://localhost:3000 | 3000 | Web application |
| **Auth DB** | localhost:5436 | 5436 | PostgreSQL (auth) |
| **Citizen DB** | localhost:5433 | 5433 | PostgreSQL (citizen) |
| **Request DB** | localhost:5434 | 5434 | PostgreSQL (request) |
| **Document DB** | localhost:5435 | 5435 | PostgreSQL (document) |

## Step-by-Step Setup

### 1. Create `.env.local` File

Copy the template and customize for local development:

```bash
cp .env.example .env.local
```

**Key settings for local development** (most are already correct):
```dotenv
# Keep these defaults for local dev
JWT_SECRET_KEY=R8+o6ZlnBpZnulNiEAOiMlFwpF7po0ulT25IUrLY9ucdMQ73wikS5yghC6SBooXx
POSTGRES_PASSWORD=postgres_dev
ADMIN_EMAIL=admin@government.gov
ADMIN_PASSWORD=Admin123!
ADMIN_FULLNAME=System Administrator
```

### 2. Start All Services

```bash
# Start all services in background
docker-compose up -d

# (or foreground to see logs)
docker-compose up

# Check status
docker-compose ps
```

### 3. Verify Services Are Running

```bash
# Test API Gateway
curl http://localhost:5050/health

# Test Auth Service  
curl http://localhost:5001/health

# Open frontend in browser
open http://localhost:3000
# or
firefox http://localhost:3000
```

### 4. Run Database Migrations

The first time you start, database schemas must be created:

```bash
# Automatic setup (recommended)
./scripts/setup-dev.sh

# OR manual setup for each service
dotnet ef database update --project src/AuthService
dotnet ef database update --project src/CitizenService
dotnet ef database update --project src/ServiceRequestService
dotnet ef database update --project src/DocumentService
```

## Common Development Tasks

### View Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f auth_service
docker-compose logs -f citizen_service

# Follow new logs only
docker-compose logs -f --since 5m
```

### Restart a Service

```bash
# Restart specific service (keeps data)
docker-compose restart citizen_service

# Rebuild and restart (useful after code changes)
docker-compose up -d --build citizen_service
```

### Rebuild All Services

```bash
# Rebuild all containers (if code changed significantly)
docker-compose up -d --build
```

### Access Database Directly

```bash
# Connect to Auth DB
psql -h localhost -p 5436 -U postgres -d auth_db
# Password: postgres_dev

# Or use any PostgreSQL client:
# - pgAdmin: http://localhost:5050 (if added to docker-compose)
# - DBeaver
# - VS Code PostgreSQL extension
```

### Stop All Services

```bash
# Stop (keeps data)
docker-compose stop

# Stop and remove containers (keeps volumes)
docker-compose down

# Remove everything including data
docker-compose down -v
```

## Local Development (No Docker)

If you prefer to run services directly on your machine for faster iteration:

### 1. Start Only Databases

```bash
# Start only database containers
docker-compose up -d auth_db citizen_db request_db document_db
```

### 2. Run Services Locally

```bash
# In separate terminals, run each service:

# Terminal 1: Auth Service
cd src/AuthService
dotnet build
ASPNETCORE_ENVIRONMENT=Development \
  ConnectionStrings__DefaultConnection="Host=localhost;Port=5436;Database=auth_db;Username=postgres;Password=postgres_dev" \
  Jwt__Key="R8+o6ZlnBpZnulNiEAOiMlFwpF7po0ulT25IUrLY9ucdMQ73wikS5yghC6SBooXx" \
  dotnet run

# Terminal 2: Citizen Service
cd src/CitizenService
dotnet build
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Terminal 3: Service Request Service
cd src/ServiceRequestService
dotnet build
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Terminal 4: Document Service
cd src/DocumentService
dotnet build
ASPNETCORE_ENVIRONMENT=Development dotnet run

# Terminal 5: API Gateway
cd src/ApiGateway
dotnet build
ASPNETCORE_ENVIRONMENT=Development dotnet run
```

### 3. Debug in Visual Studio

Since `.NET` is used, you can also open `gradprojdemo.sln` in Visual Studio:

```bash
# On Windows
start gradprojdemo.sln

# On Mac
open gradprojdemo.sln

# Or on Linux
code .  # for VS Code
```

Then in Visual Studio:
- Set breakpoints in code
- Press **F5** to start debugging
- Services will use `appsettings.Development.json` configuration
- Use `.env.local` for environment variables

## Environment Variable Reference

All services read from these environment variables. Docker Compose loads `.env.local` automatically.

### Auth Service
```
AuthDb__Host=auth_db              (hostname in Docker, localhost:5436 direct)
AuthDb__Port=5432
AuthDb__Database=auth_db
AuthDb__Username=postgres
AuthDb__Password=${POSTGRES_PASSWORD}
Jwt__Key=${JWT_SECRET_KEY}
```

### Citizen Service
```
CitizenDb__Host=citizen_db
CitizenDb__Port=5432
CitizenDb__Database=citizen_db
CitizenDb__Username=postgres
CitizenDb__Password=${POSTGRES_PASSWORD}
Jwt__Key=${JWT_SECRET_KEY}
```

### Service Request Service
```
RequestDb__Host=request_db
RequestDb__Port=5432
RequestDb__Database=request_db
RequestDb__Username=postgres
RequestDb__Password=${POSTGRES_PASSWORD}
Jwt__Key=${JWT_SECRET_KEY}
```

### Document Service
```
DocumentDb__Host=document_db
DocumentDb__Port=5432
DocumentDb__Database=document_db
DocumentDb__Username=postgres
DocumentDb__Password=${POSTGRES_PASSWORD}
Jwt__Key=${JWT_SECRET_KEY}
```

## Troubleshooting

### ❌ "Connection Refused" on localhost:5001

**Solution:** Services may still be starting. Check status:
```bash
docker-compose ps
# Wait for "running" status, then retry
docker-compose logs -f auth_service
```

### ❌ "Database connection failed"

**Solution:** Migrations haven't run. Execute:
```bash
./scripts/setup-dev.sh
# or individual commands from Step 4 above
```

### ❌ "Port 5050 already in use"

**Solution:** Another service is using that port. Either:
```bash
# Stop conflicting service
docker-compose down

# OR use different ports (edit docker-compose.yml)
```

### ❌ "Cannot find .env.local"

**Solution:** Create it from the template:
```bash
cp .env.example .env.local
```

### ❌ Docker build fails

**Solution:** Clear cache and rebuild:
```bash
docker-compose down
docker system prune -a
docker-compose up -d --build
```

### ❌ Containers exit immediately

**Solution:** Check logs for errors:
```bash
docker-compose logs --tail=50 <service-name>
```

## Tips for Team Development

### 1. Before Committing

```bash
# Ensure all tests pass
dotnet test

# Run linting
dotnet format (if configured)
```

### 2. Branching Workflow

```bash
# Create feature branch
git checkout -b feature/citizen-profile-update

# Make changes, test locally
docker-compose up -d
# ... develop and test ...

# Push and create PR
git push origin feature/citizen-profile-update
```

### 3. Database Seeding (Coming Soon)

If the team needs test data:
- Add seed script in `scripts/seed-data.sql`
- Run after migrations: `psql -h localhost -U postgres < scripts/seed-data.sql`

### 4. Stay in Sync

```bash
# Pull latest changes
git pull origin main

# Rebuild if dependencies changed
docker-compose up -d --build
```

## Next Steps

- **Frontend Development:** See [src/frontend/README.md](src/frontend/README.md)
- **API Documentation:** See [TECHNICAL_ARCHITECTURE.md](docs/TECHNICAL_ARCHITECTURE.md)
- **Deployment:** See [RENDER_DEPLOYMENT.md](docs/RENDER_DEPLOYMENT.md) or [SHARED_CLUSTER_DEPLOYMENT.md](docs/SHARED_CLUSTER_DEPLOYMENT.md)

## Questions?

Ask in Slack or review this guide's working directory for examples.
