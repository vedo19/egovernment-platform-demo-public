# Shared Cluster Deployment Guide

This repository now has two deployment modes:

- Local development with Docker Compose or Minikube.
- Shared cluster deployment for colleagues, using one Kubernetes cluster and one public or internal hostname.

The shared-cluster flow is the one to use when nobody should run the app locally.

For shared deployment, keep the ingress endpoint in `INGRESS_HOST` and avoid hardcoding real public IP addresses in repository files.

## Automated Paths

The repo now uses two deployment workflows on `main`:

- `backend-deploy.yml` for backend, gateway, and cluster infrastructure changes.
- `frontend-deploy.yml` for frontend-only changes.
- `cluster-bootstrap.yml` for first-time cluster setup (manual run).
- `preflight-check.yml` for manual prerequisite validation.

The deploy workflows also support `workflow_dispatch` for manual redeploys.

Both deploy workflows run preflight checks as a required gate before deployment starts.

All workflows share reusable setup logic via:

- `.github/actions/setup-ghcr-kube/action.yml`

Path filtering keeps the deploys focused:

- Frontend changes under `src/frontend/**` trigger only the frontend workflow.
- Backend service, gateway, Kubernetes, storage, and ingress changes trigger the backend workflow.

## What Changes For Shared Cluster

For shared environments, this repository uses the Kubernetes manifests as templates and applies production-safe overrides during CI/CD.

These overrides are applied automatically by:

- `backend-deploy.yml`
- `frontend-deploy.yml`

### Local vs Shared Behavior

| Area | Local/Minikube | Shared Cluster |
|---|---|---|
| Image source | `*:dev` image tags | GHCR image tags using commit SHA |
| App environment | `ASPNETCORE_ENVIRONMENT=Development` | `ASPNETCORE_ENVIRONMENT=Production` |
| Ingress host | `egovernment.local` | `INGRESS_HOST` from repository secret or workflow input, or a public IPv4-based host for direct access |
| Document storage | hostPath PV (`k8s/storage/document-uploads.yaml`) | cluster PVC (no hostPath) |
| Secrets source | placeholder local file values | generated from GitHub repository secrets |

### Service Coverage

Currently deployed by this repo:

- Auth Service
- Citizen Service
- Document Service
- Service Request Service
- API Gateway
- Frontend

Not included yet:

- Notification Service
- Payment Service

## Required Cluster Prerequisites

Your cluster must already have:

- NGINX Ingress Controller installed.
- A default StorageClass or a named StorageClass for document uploads.
- DNS pointing the public hostname to the ingress controller external address, or a direct public IP-based host if you are not using DNS.
- Read/write access to GitHub Container Registry.
- A Kubernetes kubeconfig stored as a GitHub secret for CI deployment.

## What To Prepare While The Instance Boots

Use this checklist to finish everything except the final deploy:

1. Prepare the GitHub repository secrets.
	- `KUBE_CONFIG_B64`
	- `POSTGRES_PASSWORD`
	- `JWT_SECRET_KEY`
	- `ADMIN_EMAIL`
	- `ADMIN_PASSWORD`
	- `ADMIN_FULLNAME`
	- `RABBITMQ_DEFAULT_USER`
	- `RABBITMQ_DEFAULT_PASS`
	- `INGRESS_HOST`

2. Prepare the OCI networking rules.
	- Public subnet with public IPv4 enabled.
	- Route table with `0.0.0.0/0 -> Internet Gateway`.
	- Ingress TCP 22 restricted to your current IP /32.
	- Ingress TCP 80 and 443 allowed for public access.
	- Ingress TCP 6443 only if GitHub-hosted Actions must reach the cluster API.

3. Prepare the cluster endpoint kubeconfig.
	- Use a kubeconfig that embeds cert data instead of local file paths.
	- Generate it from a reachable cluster with:

```bash
kubectl config view --raw --flatten | base64 -w 0
```

4. Prepare the deployment order.
	- Run `preflight-check.yml` first.
	- Run `cluster-bootstrap.yml` only if the cluster is brand new.
	- Run `backend-deploy.yml`.
	- Run `frontend-deploy.yml`.

5. Prepare the verification commands.

```bash
kubectl get pods -n egovernment
kubectl get pods -n egovernment-db
kubectl get pods -n messaging
kubectl get ingress -n egovernment
```

## GitHub Secrets Needed

Create these repository secrets:

- `KUBE_CONFIG_B64` - base64-encoded kubeconfig for the shared cluster.
- `POSTGRES_PASSWORD` - password used by all PostgreSQL instances.
- `JWT_SECRET_KEY` - JWT signing key.
- `ADMIN_EMAIL` - initial admin email.
- `ADMIN_PASSWORD` - initial admin password.
- `ADMIN_FULLNAME` - initial admin display name.
- `RABBITMQ_DEFAULT_USER` - RabbitMQ username.
- `RABBITMQ_DEFAULT_PASS` - RabbitMQ password.
- `INGRESS_HOST` - the public hostname or public IPv4-based host colleagues will open in a browser.

For the document service, the mounted path remains `/app/uploads` in both modes.

## Deployment Order

Use this order in the shared cluster:

1. Apply namespaces.
2. Apply config maps.
3. Create secrets from GitHub Actions or manually from the secret values.
4. Apply PostgreSQL StatefulSets.
5. Apply RabbitMQ.
6. Apply the document upload PVC.
7. Build and push backend images.
8. Apply backend service deployments and Ingress.
9. Build and push the frontend image.
10. Apply the frontend deployment.
11. Verify pods, services, and ingress routing.

If the frontend has not been deployed yet, the ingress root path will stay unavailable until the frontend workflow runs at least once.

## First-Time Bootstrap

For a brand-new cluster, run `cluster-bootstrap.yml` once before normal deployments.

It creates:

- namespaces,
- shared config and secrets,
- PostgreSQL stateful resources,
- RabbitMQ,
- the document uploads PVC,
- and ingress.

After bootstrap, use `backend-deploy.yml` and `frontend-deploy.yml` for routine updates.

## Verification Checklist

Run these checks after deploy:

```bash
kubectl get pods -n egovernment
kubectl get svc -n egovernment
kubectl get pods -n egovernment-db
kubectl get pods -n messaging
kubectl get ingress -n egovernment
```

Expected behavior:

- Auth and service-request should have 2 replicas.
- Document service should start with the PVC mounted at `/app/uploads`.
- Gateway should answer `/health`.
- `GET /api/auth/me` without a token should return `401`.
- The frontend should load from the ingress hostname, not from localhost.

You can also run the dedicated preflight workflow manually before any deployment:

- `preflight-check.yml`

## Colleague Access Pattern

Colleagues should only need:

- A browser.
- Network access to the shared cluster ingress hostname.
- A valid user account in the app.

They should not need Docker, Minikube, or local source code.

## Production-Style Version

What this repo does today is acceptable for a shared demo cluster, but a production deployment would usually look different.

### Demo Flow In This Repo

1. A single OCI VM hosts the Kubernetes control plane and workloads.
2. GitHub Actions deploys to that cluster using a kubeconfig secret.
3. The Kubernetes API is reachable from the CI runner during deployment.
4. Namespaces, bootstrap resources, and app workloads are applied in pipeline steps.
5. Ingress is exposed through the public IP or a simple public hostname.

### Production Flow I Would Use Instead

1. Provision infrastructure with Terraform or another IaC tool.
2. Use a managed Kubernetes service or a private cluster with controlled access.
3. Run GitHub Actions on a self-hosted runner inside the same network as the cluster.
4. Keep the Kubernetes API private; do not expose `6443` to the public internet.
5. Store secrets in a managed secret store and sync them into the cluster.
6. Apply cluster add-ons and namespaces through a hardened bootstrap job, then promote app changes separately.
7. Use DNS, TLS, and ingress managed through a stable environment-specific configuration.

### Exact Changes From The Current Flow

- Replace the public GitHub-hosted deployment path with a self-hosted runner.
- Remove the need for `KUBE_CONFIG_B64` to point at a public endpoint.
- Close public `6443` access after bootstrap.
- Move OCI networking setup into IaC instead of manual console steps.
- Split bootstrap from app deployment more strictly, so app releases do not depend on infrastructure warm-up.
- Replace the ad hoc bootstrap secrets flow with managed secret injection.

### What Stays The Same

- Namespace-based separation between app, database, and messaging resources.
- Frontend and backend deploy separation.
- Ingress-based routing to the app.
- Health checks and rollout verification.

### Recommended Next Step If You Want Production Parity

1. Add Terraform for OCI networking and the VM or cluster.
2. Move CI to a self-hosted runner in the same private network.
3. Stop exposing the Kubernetes API publicly.
4. Replace the current bootstrap secret values with a managed secret workflow.

## Nearly Production-Level Without Paying Anything

If the goal is to stay free while getting close to a production setup, keep the current architecture shape but move the operational boundary inward.

### Keep

1. The single OCI VM running k3s.
2. The split between `backend-deploy` and `frontend-deploy`.
3. Namespace isolation for app, database, and messaging components.
4. Ingress-based access through one stable public entry point.
5. Rollout checks and preflight validation in GitHub Actions.

### Change

1. Run GitHub Actions on a self-hosted runner on the same VM, or on a second free VM in the same private network.
2. Keep the Kubernetes API private to the runner instead of exposing `6443` broadly.
3. Point deployment workflows at the private cluster endpoint or a local kubeconfig on the runner.
4. Add TLS for ingress with a free certificate source such as Let\'s Encrypt.
5. Move runtime secrets out of workflow inputs and into cluster-managed secrets or sealed secrets.

### Remove

1. Public access to the Kubernetes API after bootstrap.
2. Any workflow assumption that a GitHub-hosted runner can always reach the cluster directly.
3. Manual console-driven cluster setup once the base VM is provisioned.
4. Ad hoc secret handling in deployment jobs.

### What This Gives You

1. A deployment model that behaves like production without paying for managed Kubernetes.
2. A smaller attack surface because the control plane is not public.
3. A CI/CD path that matches how real environments usually deploy.
4. A setup that remains realistic for a public demo, class project, or portfolio system.
