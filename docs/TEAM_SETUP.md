# Team Setup and Migration Guide

This guide is the source of truth for keeping all team members on the same runtime, schema, and data flow.

## 1. Golden Rules

1. Always pull latest code before starting work.
2. Never change database schema manually in SQL clients.
3. Schema changes must be done through EF Core migrations and committed.
4. Run the platform through Docker Compose so everyone uses the same ports and service topology.

## 2. Standard Team Startup

From repository root:

```powershell
git pull
docker compose up -d --build
docker compose ps -a
```

Expected URLs:

1. Frontend: http://localhost:3000
2. API Gateway: http://localhost:5050
3. Auth Service: http://localhost:5001
4. Citizen Service: http://localhost:5002
5. Service Request Service: http://localhost:5003
6. Document Service: http://localhost:5004

## 3. How Migrations Work in This Project

Each service owns its own database and migrations:

1. ServiceRequestService migrations update request_db.
2. DocumentService migrations update document_db.
3. AuthService and CitizenService keep their own histories.

Services run `Database.Migrate()` on startup, so pending migrations are applied automatically when containers start.

This means teammates usually do not need to run `dotnet ef database update` manually when using Docker Compose.

## 4. Team Workflow for New DB Changes

When a teammate changes models:

1. Create migration in that service.
2. Commit model + migration + updated snapshot.
3. Push branch and merge.
4. Everyone pulls and runs `docker compose up -d --build`.
5. On startup, pending migration is applied automatically.

## 5. Creating Migrations Manually

ServiceRequestService:

```powershell
Set-Location src/ServiceRequestService
dotnet ef migrations add MigrationName --output-dir Data/Migrations
```

DocumentService:

```powershell
Set-Location src/DocumentService
dotnet ef migrations add MigrationName --output-dir Data/Migrations
```

## 6. Manual Database Update (Only If Needed)

If you run services outside Docker or need manual apply:

```powershell
Set-Location src/ServiceRequestService
dotnet ef database update

Set-Location ..\DocumentService
dotnet ef database update
```

## 7. Live Data Visibility for Team

To inspect live database data in PostgreSQL clients (DBeaver, pgAdmin):

1. request_db: localhost:5434
2. document_db: localhost:5435
3. citizen_db: localhost:5433
4. auth_db: localhost:5436

Credentials come from `.env` / docker-compose variables.

Important: live data matching means using the same running Docker stack. If someone uses local non-Docker DB, data will differ.

## 8. Reset Scenarios

Stop containers, keep data:

```powershell
docker compose down
```

Stop containers and wipe all DB data (full reset):

```powershell
docker compose down -v
docker compose up -d --build
```

Use full reset only when team agrees, because all local DB data is removed.

## 9. Recovery Checklist When Something Looks Wrong

1. `git status` and confirm correct branch.
2. `docker compose ps -a` and verify services are Up.
3. `docker compose logs service_request_service document_service --tail=200` for migration errors.
4. Rebuild changed services:

```powershell
docker compose up -d --build service_request_service document_service api_gateway frontend
```

5. Hard refresh browser.
6. Re-login to refresh JWT token.

## 10. Postman Step-by-Step Demo Usage

Collection file:

1. `docs/postman/Workflow-Demo.postman_collection.json`

How to run:

1. Import the collection in Postman.
2. Open collection variables and set:
	- baseUrl = http://localhost:5050
	- citizenEmail, citizenPassword
	- officerEmail, officerPassword
	- adminEmail, adminPassword
	- pdfPath to a local PDF file path
3. Run three login requests and copy returned tokens into:
	- citizenToken
	- officerToken
	- adminToken
4. Run Citizen - Create Permit Request and copy returned id into requestId.
5. Run Admin - Assign Officer and set officerId to target officer user id.
6. Run Officer - Request Documents.
7. Run Citizen - Upload Permit PDF.
8. Optionally run Officer - Reject Documents, then Citizen - Reupload Permit PDF.
9. Run Officer - Approve Request.
10. Validate dashboards and list endpoints:
	- Citizen - My Requests
	- Officer - Assigned To Me
	- Admin - All Requests
