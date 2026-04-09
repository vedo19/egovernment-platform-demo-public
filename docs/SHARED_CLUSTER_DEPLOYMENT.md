# Shared Cluster Deployment Guide

This repository now has two deployment modes:

- Local development with Docker Compose or Minikube.
- Shared cluster deployment for colleagues, using one Kubernetes cluster and one public or internal hostname.

The shared-cluster flow is the one to use when nobody should run the app locally.

## What Changes For Shared Cluster

Use the current Kubernetes manifests as the base, then make these deployment-time changes:

- Replace `:dev` image references with registry images, for example `ghcr.io/<owner>/egovernment-platform-demo-public/auth-service:<tag>`.
- Replace the Minikube-only document storage volume with a cluster-backed PVC.
- Keep `ASPNETCORE_ENVIRONMENT=Production` in the shared config map.
- Do not apply the local `k8s/storage/document-uploads.yaml` hostPath PV in shared environments.
- Use a real DNS name instead of `egovernment.local` for Ingress.

The repo currently has no notification or payment service, so the shared deployment covers only:

- Auth Service
- Citizen Service
- Document Service
- Service Request Service
- API Gateway
- Frontend

## Required Cluster Prerequisites

Your cluster must already have:

- NGINX Ingress Controller installed.
- A default StorageClass or a named StorageClass for document uploads.
- DNS pointing the public hostname to the ingress controller external address.
- Read/write access to GitHub Container Registry.
- A Kubernetes kubeconfig stored as a GitHub secret for CI deployment.

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
- `INGRESS_HOST` - the public hostname colleagues will open in a browser.

## Exact Manifest Changes For Shared Cluster

The repo-local manifests stay useful for Minikube, but the shared cluster should apply these changes at deploy time:

| Area | Local Manifest | Shared Cluster Change |
|---|---|---|
| Images | `image: auth-service:dev` and similar | Use GHCR image names with immutable tags |
| Environment | `ASPNETCORE_ENVIRONMENT=Development` | `ASPNETCORE_ENVIRONMENT=Production` |
| Document storage | `k8s/storage/document-uploads.yaml` hostPath PV | Replace with PVC only and bind to cluster storage |
| Ingress host | `egovernment.local` | Use the real `INGRESS_HOST` value |
| Secrets | Repo-local placeholder secret file | Generate the secret in CI from GitHub Secrets |

For the document service, the mounted path stays `/app/uploads`.

## Deployment Order

Use this order in the shared cluster:

1. Apply namespaces.
2. Apply config maps.
3. Create secrets from GitHub Actions or manually from the secret values.
4. Apply PostgreSQL StatefulSets.
5. Apply RabbitMQ.
6. Apply the document upload PVC.
7. Build and push images.
8. Apply service deployments.
9. Apply Ingress.
10. Verify pods, services, and ingress routing.

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

## Colleague Access Pattern

Colleagues should only need:

- A browser.
- Network access to the shared cluster ingress hostname.
- A valid user account in the app.

They should not need Docker, Minikube, or local source code.
