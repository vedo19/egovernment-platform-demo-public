# Render Deployment Guide

This project can be deployed to Render using the repository blueprint file at `render.yaml`.

## 1. Push these changes to GitHub

Render reads blueprint config from your GitHub repository, so make sure this branch is pushed.

## 2. Create services from Blueprint

In Render:
1. Open your target project.
2. Click New and choose Blueprint.
3. Select this repository and branch.
4. Render will detect `render.yaml` and create 10 resources:
   - 6 web services: frontend, api-gateway, auth-service, citizen-service, service-request-service, document-service
   - 4 PostgreSQL databases: auth-db, citizen-db, request-db, document-db

## 3. Fill required secret/env values

Render will ask for all env vars with `sync: false`.

Use the following values:
- `Jwt__Key` on all backend services: same strong secret (min 32 chars)
- `Admin__Email` on auth-service: your admin email
- `Admin__Password` on auth-service: strong admin password
- `Cors__AllowedOrigins__0` on api-gateway: your frontend public URL, for example `https://frontend.onrender.com`
- `VITE_API_URL` on frontend: your gateway public URL, for example `https://api-gateway.onrender.com`

Important:
- The same `Jwt__Key` must be used in auth-service, citizen-service, service-request-service, and document-service.
- `VITE_API_URL` is a build-time variable. If you change it later, trigger a new frontend deploy.

## 4. Deploy order

Render usually deploys all resources, but the safest manual retry order is:
1. Databases
2. auth-service, citizen-service, service-request-service, document-service
3. api-gateway
4. frontend

## 5. Verify after deploy

- Open frontend URL.
- Login with the admin user (`Admin__Email` / `Admin__Password`).
- Check gateway health endpoint: `https://<api-gateway-domain>/health`.

## 6. Common issues

### 502 from gateway

Usually means one downstream service is still building or unhealthy.

Check:
- api-gateway logs
- auth-service/citizen-service/service-request-service/document-service health (`/healthz`)

### Local `dev-remote-db` fails with `Name or service not known`

This usually means `AuthDb__Host` (or another DB host) is set to a Render internal hostname like `dpg-...`.

That hostname only resolves inside Render's private network. For local development, use the public/external Render Postgres hostname from the database dashboard.

### CORS error in browser

Set `Cors__AllowedOrigins__0` on api-gateway exactly to the frontend URL (scheme + host).

### 401 token errors across services

Ensure `Jwt__Key` is exactly the same on all four backend services.
