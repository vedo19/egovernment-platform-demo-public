# 🚀 Quick Start Card

## 1) First-time setup

If you need the prepared environment examples, `musss2003` will send the `.env.local` and `.env.remote` examples.

```bash
make setup
```

`make setup` creates `.env.local` if needed, starts the local Docker stack, and runs migrations.

---

## 2) Pick the right mode

| Mode | Command | What it uses |
|------|---------|--------------|
| Local Docker DBs | `make dev-local` | `.env.local` + local Postgres containers |
| Remote DBs | `make dev-remote-db` | `.env.remote` + external DBs |
| Full compose startup | `make up` | `.env.local` and the standard compose stack |

Use `make dev-local` for the normal team setup. Use `make dev-remote-db` only when you want the services to talk to external databases configured in `.env.remote`.
The frontend is usually run locally with `npm run dev`; `make restart-frontend` only restarts the Docker Compose frontend service when you are using `make up`.

---

## 3) Common `make` commands

```bash
make help            # Show all available commands
make setup           # Create env + start services + run migrations
make up              # Start the compose stack
make up-fg           # Start the compose stack in the foreground
make rebuild         # Rebuild and restart containers
make down            # Stop services but keep data
make clean           # Stop services and remove all volumes/data
make health          # Check service health endpoints
```

---

## 4) Service-specific commands

```bash
make logs-auth       # Auth service logs
make logs-citizen    # Citizen service logs
make logs-request    # Service request service logs
make logs-document   # Document service logs
make logs-gateway    # API gateway logs
make logs-frontend   # Frontend logs

make restart-auth    # Restart auth service
make restart-citizen # Restart citizen service
make restart-request # Restart service request service
make restart-document # Restart document service
make restart-gateway # Restart API gateway
make restart-frontend # Restart Docker Compose frontend service
```

Tip: the auth service is the one to inspect first if login or user fetching fails.

---

## 5) URLs and ports

| Service | URL | Port |
|---------|-----|------|
| Frontend | http://localhost:3000 | 3000 |
| API Gateway | http://localhost:5050 | 5050 |
| Auth Service | http://localhost:5001 | 5001 |
| Citizen Service | http://localhost:5002 | 5002 |
| Service Request Service | http://localhost:5003 | 5003 |
| Document Service | http://localhost:5004 | 5004 |

| Database | Host | Port | Default Credentials |
|----------|------|------|---------------------|
| Auth | localhost | 5436 | postgres / postgres_dev |
| Citizen | localhost | 5433 | postgres / postgres_dev |
| Request | localhost | 5434 | postgres / postgres_dev |
| Document | localhost | 5435 | postgres / postgres_dev |

---

## 6) If something breaks

```bash
make logs
make logs-auth
make clean && make dev-local
```

If you are using remote DB mode, verify `.env.remote` points to a public/external database hostname. Internal Render `dpg-...` hosts will not work from local Docker.

**Last Updated:** April 2026
