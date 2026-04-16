# 🚀 Quick Start Card - Print This!

## First Time Setup
```bash
# 1. Copy environment template
cp .env.example .env.local

# 2. Start everything in one command
make setup
```
**Done!** All services + databases running in ~60 seconds.

---

## Access Your Services

| Service | URL | Port |
|---------|-----|------|
| **Frontend** | http://localhost:3000 | 3000 |
| **API Gateway** | http://localhost:5050 | 5050 |
| **Auth Service** | http://localhost:5001 | 5001 |
| **Citizen Service** | http://localhost:5002 | 5002 |
| **Request Service** | http://localhost:5003 | 5003 |
| **Document Service** | http://localhost:5004 | 5004 |

---

## Databases

| Database | Host | Port | Default Credentials |
|----------|------|------|---------------------|
| Auth | localhost | 5436 | postgres / postgres_dev |
| Citizen | localhost | 5433 | postgres / postgres_dev |
| Request | localhost | 5434 | postgres / postgres_dev |
| Document | localhost | 5435 | postgres / postgres_dev |

Connect with any PostgreSQL client (pgAdmin, DBeaver, psql, etc.)

---

## Daily Development Commands

```bash
# View all available commands
make help

# Start services
make up

# View logs (all or specific)
make logs
make logs-citizen

# Rebuild (after dependency changes)
make rebuild

# Stop everything
make down

# Full cleanup (removes data)
make clean

# Restart a single service
make restart-auth
make restart-citizen
make restart-request
make restart-document
```

---

## During Development

### 1. After Pulling Changes
```bash
make rebuild
```

### 2. To Debug a Service in VS Code
```bash
# Terminal 1: Start databases only
make up

# Terminal 2: Debug in VS Code
# Open gradprojdemo.sln and press F5
# Services will use localhost + .env.local
```

### 3. Check If Services Are Healthy
```bash
make health
```

### 4. Database Issues
```bash
# Connect directly
make db-connect

# Etc.: Run migrations manually
dotnet ef database update --project src/CitizenService
```

---

## Stuck? Common Issues

| Problem | Solution |
|---------|----------|
| **Port already in use** | `docker-compose down` then retry |
| **Connection refused** | Wait 30s for startup, then `make logs` |
| **DB migrations fail** | `make migrate` manually or check `make logs` |
| **Services exit** | Run `make logs` to see error messages |
| **Nothing working** | Nuclear option: `make clean && make setup` |

---

## For Detailed Help
See [LOCAL_DEVELOPMENT.md](LOCAL_DEVELOPMENT.md) for:
- Complete prerequisites
- Local machine setup (no Docker)
- Environment variable reference
- Troubleshooting guide
- Team best practices

---

**Last Updated:** April 2026
