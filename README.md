# E-Government Microservices Platform

A full-stack e-government system that lets citizens apply for permits, request official documents, file complaints, and track their status — while admins and officers manage all incoming requests. Built with microservice architecture, each service has its own database, Docker container, and API.

---

## What's Inside

| Component | Technology | Port |
|-----------|-----------|------|
| **Frontend** | React (Vite) served by nginx | `3000` |
| **API Gateway** | ASP.NET Core + Ocelot | `5050` |
| **Auth Service** | ASP.NET Core 10 + PostgreSQL | `5001` |
| **Citizen Service** | ASP.NET Core 10 + PostgreSQL | `5002` |
| **Service Request Service** | ASP.NET Core 10 + PostgreSQL | `5003` |
| **Document Service** | ASP.NET Core 10 + PostgreSQL | `5004` |

All communication between the frontend and backend goes through the **API Gateway** on port 5050. The gateway forwards JWT tokens and routes requests to the correct microservice.

---

## Prerequisites — What You Need to Install

Before you can run this project, you need the following software installed on your machine.

### 1. Git

Used to clone (download) the project from GitHub.

- **Windows:** Download from https://git-scm.com/download/win — run the installer, keep defaults.
- **Mac:** Run `xcode-select --install` in Terminal, or download from https://git-scm.com/download/mac
- **Linux:** `sudo apt install git` (Ubuntu/Debian) or `sudo dnf install git` (Fedora)

Verify: Open a terminal and run:
```bash
git --version
```

### 2. Docker Desktop

Docker runs all the databases, services, and the frontend in isolated containers. This is the **only tool you strictly need** to run the project.

- **Windows / Mac:** Download from https://www.docker.com/products/docker-desktop/ — install and start Docker Desktop.
- **Linux:** Follow https://docs.docker.com/engine/install/ for your distro, then install Docker Compose.

After installing, make sure Docker is **running** (you should see the Docker whale icon in your system tray / menu bar).

Verify:
```bash
docker --version
docker compose version
```

### 3. .NET 10 SDK (only needed for development / running without Docker)

If you want to run individual services outside of Docker (for debugging or development):

- Download from https://dotnet.microsoft.com/download/dotnet/10.0
- Choose the **SDK** (not Runtime) for your OS.

Verify:
```bash
dotnet --version
```
Expected output: `10.0.xxx`

### 4. Node.js 22+ (only needed for frontend development)

If you want to modify the React frontend code and run it locally with hot-reload:

- Download the LTS version from https://nodejs.org/
- The installer includes `npm` (the package manager).

Verify:
```bash
node --version
npm --version
```

---

## Step-by-Step: How to Run the Project

### Step 1: Clone the Repository

Open a terminal (PowerShell on Windows, Terminal on Mac/Linux) and run:

```bash
git clone https://github.com/vedo19/egovernment-platform-demo.git
cd egovernment-platform-demo
```

### Step 2: Start Docker Desktop

Make sure Docker Desktop is running. You should see the whale icon in your taskbar.

### Step 3: Create Local .env File

Copy the environment template and customize values for your machine:

```bash
cp .env.example .env
```

Windows PowerShell alternative:

```powershell
Copy-Item .env.example .env
```

Then edit `.env` and set at least:
- `JWT_SECRET_KEY` (minimum 32 chars)
- `POSTGRES_PASSWORD`
- `ADMIN_EMAIL`
- `ADMIN_PASSWORD`

### Step 4: Build and Start All Containers

From the project root directory, run:

```bash
docker compose up --build -d
```

This will:
1. Download PostgreSQL images for all 4 databases
2. Build all 4 .NET microservices
3. Build the React frontend
4. Build the API Gateway
5. Start everything (10 containers total)

**First run will take 3–5 minutes** (downloading images + building). Subsequent runs are much faster.

### Step 5: Verify Everything is Running

```bash
docker compose ps
```

You should see 10 containers, all with status "Up":
- `auth_db`, `citizen_db`, `request_db`, `document_db` — databases
- `auth_service`, `citizen_service`, `service_request_service`, `document_service` — microservices
- `api_gateway` — the gateway
- `frontend` — the React app

### Step 6: Open the Application

Open your web browser and go to:

```
http://localhost:3000
```

You will see the login page.

**First admin account is created automatically:**
- Email: set with `ADMIN_EMAIL` in your local `.env`
- Password: set with `ADMIN_PASSWORD` in your local `.env`

Log in with this account to access the Admin Dashboard. From there you can manage users and promote citizens to officers.

To create citizen accounts, click "Register" — all new registrations are created as Citizens. The admin can then promote users to Officer or Admin from the Users tab.

### Step 7: Stop the Application

When you're done, stop all containers:

```bash
docker compose down
```

To also delete the databases (start fresh next time):

```bash
docker compose down -v
```

---

## Running Individual Services for Development

If you want to modify and debug a specific service:

### Backend Service

```bash
cd src/AuthService
dotnet run
```

The service will start on its configured port. You'll need a PostgreSQL database running (either via Docker or locally).

### Frontend (with hot-reload)

```bash
cd src/frontend
npm install
npm run dev
```

Opens at `http://localhost:5173` with hot-reload. Make sure the backend containers are running (`docker compose up -d`).

---

## Project Structure

```
egovernment-platform-demo/
├── docker-compose.yml              # Defines all 10 containers
├── .env.example                    # Environment variables template
├── README.md                       # This file
│
└── src/
    ├── AuthService/                # User registration, login, JWT tokens
    │   ├── Controllers/
    │   ├── Services/
    │   ├── Models/
    │   ├── DTOs/
    │   └── Dockerfile
    │
    ├── CitizenService/             # Citizen profile management
    │   ├── Controllers/
    │   ├── Services/
    │   ├── Models/
    │   ├── DTOs/
    │   └── Dockerfile
    │
    ├── ServiceRequestService/      # Permits & complaints
    │   ├── Controllers/
    │   ├── Services/
    │   ├── Models/
    │   ├── DTOs/
    │   └── Dockerfile
    │
    ├── DocumentService/            # Official document requests
    │   ├── Controllers/
    │   ├── Services/
    │   ├── Models/
    │   ├── DTOs/
    │   └── Dockerfile
    │
    ├── ApiGateway/                 # Ocelot gateway — routes all traffic
    │   ├── ocelot.json
    │   ├── Program.cs
    │   └── Dockerfile
    │
    └── frontend/                   # React SPA
        ├── src/
        │   ├── api/                # Axios client & API service functions
        │   ├── context/            # Auth state management
        │   ├── components/         # Layout, ProtectedRoute
        │   └── pages/              # Login, Register, Dashboards
        ├── nginx.conf
        └── Dockerfile
```

---

## API Endpoints Quick Reference

All endpoints are accessed through the gateway at `http://localhost:5050`.

### Auth Service — `/api/auth/`
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/auth/register` | Public | Register new citizen |
| POST | `/api/auth/login` | Public | Login, returns JWT |
| GET | `/api/auth/me` | Authenticated | Get current user |
| GET | `/api/auth/users` | Admin | List all users |
| GET | `/api/auth/users/{id}` | Admin | Get user by ID |
| PUT | `/api/auth/users/{id}/role` | Admin | Change user role |

### Citizen Service — `/api/citizens/`
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/citizens/profile` | Citizen | Create profile |
| GET | `/api/citizens/profile` | Citizen | Get own profile |
| PUT | `/api/citizens/profile` | Citizen | Update own profile |
| GET | `/api/citizens` | Admin | List all citizens |

### Service Request Service — `/api/servicerequests/`
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/servicerequests` | Citizen | Submit request |
| GET | `/api/servicerequests/my-requests` | Citizen | Own requests |
| GET | `/api/servicerequests/my-assignments` | Officer | Assigned to me |
| GET | `/api/servicerequests` | Admin/Officer | All requests |
| PUT | `/api/servicerequests/{id}/status` | Admin/Officer | Update status |
| PUT | `/api/servicerequests/{id}/assign` | Admin | Assign officer |

### Document Service — `/api/documents/`
| Method | Endpoint | Access | Description |
|--------|----------|--------|-------------|
| POST | `/api/documents` | Citizen | Request document |
| GET | `/api/documents/my-documents` | Citizen | Own documents |
| GET | `/api/documents/my-assignments` | Officer | Assigned to me |
| GET | `/api/documents` | Admin/Officer | All documents |
| PUT | `/api/documents/{id}/status` | Admin/Officer | Update status |
| PUT | `/api/documents/{id}/assign` | Admin | Assign officer |

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `docker compose` not found | Make sure Docker Desktop is running. On older Docker, try `docker-compose` (with a hyphen). |
| Port already in use | Stop any other services using ports 3000, 5050, 5001-5004, 5433-5436. |
| Containers start but API returns errors | Wait 10-15 seconds after `docker compose up` for databases to initialize. |
| Frontend shows blank page | Clear browser cache and localStorage (`F12` > Application > Clear Storage). |
| "Connection refused" from frontend | Make sure the API Gateway container is running on port 5050. |

---

## Future Roadmap

- CI/CD pipeline with GitHub Actions
- Kubernetes deployment manifests
- Container orchestration with Helm charts
- Monitoring and logging (Prometheus, Grafana, ELK)
- Billing/Payment microservice

---

## Accessing the Databases Locally

Each microservice has its own PostgreSQL database running in a Docker container. You can connect to any of them from your local machine.

### Using pgAdmin or any PostgreSQL client

| Database | Host | Port | Username | Password | Database Name |
|----------|------|------|----------|----------|---------------|
| Auth DB | `localhost` | `5436` | `postgres` | from `.env` (`POSTGRES_PASSWORD`) | `auth_db` |
| Citizen DB | `localhost` | `5433` | `postgres` | from `.env` (`POSTGRES_PASSWORD`) | `citizen_db` |
| Request DB | `localhost` | `5434` | `postgres` | from `.env` (`POSTGRES_PASSWORD`) | `request_db` |
| Document DB | `localhost` | `5435` | `postgres` | from `.env` (`POSTGRES_PASSWORD`) | `document_db` |

### Using psql from the command line

If you have `psql` installed locally:

```bash
# Connect to auth database
psql -h localhost -p 5436 -U postgres -d auth_db

# Connect to citizen database
psql -h localhost -p 5433 -U postgres -d citizen_db

# Connect to service request database
psql -h localhost -p 5434 -U postgres -d request_db

# Connect to document database
psql -h localhost -p 5435 -U postgres -d document_db
```

### Using psql inside Docker (no local install needed)

```bash
# Connect to auth database via its container
docker exec -it auth_db psql -U postgres -d auth_db

# Connect to citizen database
docker exec -it citizen_db psql -U postgres -d citizen_db

# Connect to request database
docker exec -it request_db psql -U postgres -d request_db

# Connect to document database
docker exec -it document_db psql -U postgres -d document_db
```

### Useful SQL commands once connected

```sql
-- List all tables
\dt

-- See table structure
\d "Users"
\d "ServiceRequests"
\d "Documents"

-- Query data
SELECT * FROM "Users";
SELECT * FROM "ServiceRequests" ORDER BY "CreatedAt" DESC;
SELECT * FROM "Documents" ORDER BY "CreatedAt" DESC;

-- Exit psql
\q
```

### Using pgAdmin (GUI)

1. Download and install [pgAdmin](https://www.pgadmin.org/download/)
2. Open pgAdmin and click "Add New Server"
3. In the **General** tab, give it a name (e.g., "Auth DB")
4. In the **Connection** tab, enter the host/port/username/password from the table above
5. Click Save — you'll see the database with its tables in the left panel
6. Repeat for each database (ports 5436, 5433, 5434, 5435)
