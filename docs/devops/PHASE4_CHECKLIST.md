# Phase 4: CI/CD Pipeline Completion (Sprint 3–4 — Weeks 5–8)

Goal: Fully automated build-test-push pipeline for all 6 services.

## Checklist

| #   | Task                             | Details                                                                                             | Status    |
| --- | -------------------------------- | --------------------------------------------------------------------------------------------------- | --------- |
| 4.1 | Finalise CI workflow per service | Create 6 workflow files with path filters (services/X/\*\*), .NET cache, build + test steps         | ✅ Done   |
| 4.2 | Add JUnit test reporting         | Integrate dorny/test-reporter@v1 with always() condition to publish test results even on failure    | ✅ Done   |
| 4.3 | Add Docker build-and-push job    | Job 2: login to ghcr.io, build multi-stage image, push with latest + SHA tags. Only on main branch  | ✅ Done   |
| 4.4 | Configure branch protection      | Require CI pass before merge to main. Require PR reviews                                            | 🔲 Manual |
| 4.5 | Verify pipeline speed target     | Full pipeline run must complete in < 10 minutes (KPI). Optimise .NET cache and Docker layer caching | 🔲 Verify |
| 4.6 | Add coverage reporting           | Configure coverage in CI, publish coverage report. Target > 70%                                     | ✅ Done   |

## Workflow Files Created

1. `.github/workflows/auth-service-ci.yml` - Auth Service
2. `.github/workflows/api-gateway-ci.yml` - API Gateway
3. `.github/workflows/citizen-service-ci.yml` - Citizen Service
4. `.github/workflows/document-service-ci.yml` - Document Service
5. `.github/workflows/service-request-service-ci.yml` - Service Request Service
6. `.github/workflows/frontend-ci.yml` - Frontend (React/Vite)
7. `.github/workflows/branch-protection-check.yml` - Branch protection verification

## Manual Configuration Required

### 4.4 Branch Protection Rules

Configure in GitHub Settings > Branches > Branch protection rules:

1. **Protect main branch**
   - Require pull request reviews before merging: 1 review
   - Require status checks to pass before merging: Select all CI workflows
   - Require conversation resolution before merging
   - Include administrators (optional, disable for stricter enforcement)

2. **Required Status Checks:**
   - auth-service-ci
   - api-gateway-ci
   - citizen-service-ci
   - document-service-ci
   - service-request-service-ci
   - frontend-ci

### 4.5 Pipeline Speed Verification

Run the workflows and verify total execution time is < 10 minutes. Optimizations applied:

- .NET SDK caching via actions/setup-dotnet
- npm caching for frontend
- Docker layer caching via GitHub Actions cache (type=gha)
- Docker Buildx with cache-to mode=max

## Coverage Targets

All .NET services configured to generate Cobertura coverage reports. Target > 70% line coverage.

## GitHub Container Registry

Images will be pushed to:

- `ghcr.io/<owner>/egovernment-platform-demo-public/auth-service`
- `ghcr.io/<owner>/egovernment-platform-demo-public/api-gateway`
- `ghcr.io/<owner>/egovernment-platform-demo-public/citizen-service`
- `ghcr.io/<owner>/egovernment-platform-demo-public/document-service`
- `ghcr.io/<owner>/egovernment-platform-demo-public/service-request-service`
- `ghcr.io/<owner>/egovernment-platform-demo-public/frontend`
