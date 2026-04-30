# Neon Database Setup Guide

This guide explains how your team uses Neon PostgreSQL for shared development and testing.

## What is Neon?

**Neon** is a serverless PostgreSQL database platform. Instead of running PostgreSQL locally in Docker containers, your team connects to cloud-hosted databases.

**Benefits:**
- ✅ No local resource usage (less RAM/CPU requirements)
- ✅ Shared database for the team (consistent test data)
- ✅ Always accessible (work from anywhere)
- ✅ Automatic backups and scalability
- ✅ Easy integration with deployment platforms (Render, Vercel, etc.)

## Current Setup

Your Neon account has been provisioned with:

| Database | Database Name | Mode | Details |
|----------|---------------|------|---------|
| All Services | `neondb` | Pooler | Shared across all 4 services |

**Connection Details:**
```
Host: ep-proud-resonance-anqwjg59-pooler.c-6.us-east-1.aws.neon.tech
Port: 5432
Username: neondb_owner
Database: neondb (shared)
Region: us-east-1 (AWS)
SSL: Required ✓
```

## For Your Team: Local Setup with Neon

### Step 1: Clone and Setup (First Time Only)

```bash
# Clone repository
git clone <repo-url>
cd egovernment-platform-demo-public

# Use Neon instead of Docker
cp .env.neon .env.local

# Start services (API gateway, frontend, etc. still in Docker)
docker-compose up -d

# Run migrations
./scripts/setup-dev.sh
```

### Step 2: Verify Connection

```bash
# Check service logs
docker-compose logs -f auth_service

# Should see: "Successfully connected to Neon"
# Instead of: "Connecting to localhost:5436"
```

### Step 3: Access Your Data

All your data is now in Neon. You can access it via:

**Option A: Neon Web Console**
1. Go to [https://console.neon.tech](https://console.neon.tech)
2. Select your project
3. Open the SQL Editor
4. Run queries directly

**Option B: Command Line (psql)**

```bash
# Install psql (if needed)
# macOS: brew install libpq
# Ubuntu: sudo apt install postgresql-client
# Windows: Download PostgreSQL (includes psql)

# Connect to Neon
psql "postgresql://neondb_owner:npg_VmcSO6B0xCEQ@ep-proud-resonance-anqwjg59-pooler.c-6.us-east-1.aws.neon.tech/neondb?sslmode=require"

# Now run SQL queries
SELECT * FROM "Users" LIMIT 5;
SELECT COUNT(*) FROM "CitizenProfiles";
```

**Option C: DBeaver / pgAdmin**

Visual database clients can connect using the same connection details above.

## Team Workflows

### Shared Test Data

Since everyone uses the same database, test data is shared:

```bash
# Your changes are immediately visible to teammates
# They don't need to re-run migrations

# Example: Admin creates test citizen profile
# Teammate (in another country!) can immediately query it
```

### Reset Database (if corrupted)

If test data gets messy, **reset the shared Neon database**:

```bash
# ⚠️  WARNING: This deletes ALL data
# Contact team lead before doing this

# From Neon console:
# 1. Go to project settings
# 2. "Reset database"
# 3. Rerun migrations: ./scripts/setup-dev.sh
```

### Multiple Databases for Different Environments

Currently you have **1 shared database** (`neondb`). For production-grade setup:

- **Development** → Use current shared `neondb` for team dev
- **Staging** → Separate `neondb-staging` database (optional)
- **Production** → Separate `neondb-prod` database (optional)

To create additional databases:
1. Go to [https://console.neon.tech](https://console.neon.tech)
2. Click "+ Create a database"
3. Update `.env` files with new connection strings

## Switching Between Docker and Neon

### To Use Neon (Cloud)

```bash
cp .env.neon .env.local
docker-compose up -d
```

### To Use Docker Locally

```bash
cp .env.example .env.local
docker-compose up -d --build
```

## Common Neon Tasks

### Check Database Size

```sql
-- In Neon console or psql
SELECT pg_size_pretty(pg_database.datsize) 
FROM pg_database 
WHERE datname = 'neondb';
```

### View Recent Queries

```sql
-- Neon provides query analytics in the web console
-- Go to "Monitoring" → "Query statistics"
```

### Export Data

```bash
# Backup current database
pg_dump "postgresql://neondb_owner:npg_VmcSO6B0xCEQ@ep-proud-resonance-anqwjg59-pooler.c-6.us-east-1.aws.neon.tech/neondb?sslmode=require" > neondb_backup.sql

# Restore later
psql "postgresql://neondb_owner:npg_VmcSO6B0xCEQ@ep-proud-resonance-anqwjg59-pooler.c-6.us-east-1.aws.neon.tech/neondb?sslmode=require" < neondb_backup.sql
```

## Troubleshooting Neon Connection

### ❌ "Authentication failed"

**Cause:** Wrong password or username

**Solution:** Verify in `.env.local`:
```bash
# Should be:
CitizenDb__Password=npg_VmcSO6B0xCEQ
CitizenDb__Username=neondb_owner
```

### ❌ "SSL certificate verification failed"

**Cause:** SSL mode mismatch

**Solution:** Verify in `.env.local`:
```bash
CitizenDb__SslMode=Require
```

### ❌ "Connection refused"

**Cause:** Neon endpoint might be temporarily down

**Solution:**
1. Check [Neon status](https://status.neon.tech)
2. Try connecting via web console first
3. If that works, check your `.env.local` settings

### ❌ "Too many connections"

**Cause:** Connection pool exhausted

**Solution:** The pooler endpoint automatically handles this, but if it persists:
1. Restart services: `docker-compose restart`
2. Contact Neon support

## Cost Considerations

Neon Free Tier includes:
- ✅ 3 databases
- ✅ 10GB storage
- ✅ Unlimited connections (with pooling)
- ✅ Automatic backups

**When to upgrade:** If your database grows beyond 10GB.

## Next Steps

1. ✅ **Copy configuration:** `cp .env.neon .env.local`
2. ✅ **Start services:** `docker-compose up -d`
3. ✅ **Run migrations:** `./scripts/setup-dev.sh`
4. ✅ **Verify connection:** Check logs and web console
5. 📚 **Read:** [LOCAL_DEVELOPMENT.md](LOCAL_DEVELOPMENT.md)
6. 💬 **Questions?** Ask in team Slack

---

**Neon Console:** [https://console.neon.tech](https://console.neon.tech)
**Documentation:** [https://neon.tech/docs](https://neon.tech/docs)
