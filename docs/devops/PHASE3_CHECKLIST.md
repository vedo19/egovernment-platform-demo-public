# Phase 3 Checklist

Sprint: Weeks 5-6  
Goal: All repository services running in Kubernetes with NGINX Ingress routing.

## Scope Alignment

This repository currently deploys these six services:
- `auth-service`
- `citizen-service`
- `service-request-service`
- `document-service`
- `api-gateway`
- `frontend`

The original plan mentions `application`, `notification`, and `payment` services, which are not separate deployable services in this repo today.

## Task Tracker

- [x] 3.1 Deploy remaining services
  - Status: all six repo services are already deployed in `egovernment` namespace and `Running`.

- [x] 3.2 Configure NGINX Ingress
  - Manifest: `k8s/ingress/ingress.yaml` is applied.
  - Verification: `curl -H "Host: egovernment.local" http://$(minikube ip)/api/auth/me` returns `401` (expected).

- [x] 3.3 Set resource requests/limits
  - Status: service deployments define `requests: cpu 250m / memory 256Mi` and `limits: cpu 500m / memory 512Mi`.

- [x] 3.4 Configure PersistentVolume for document uploads
  - Manifests: `k8s/storage/document-uploads.yaml` + volume mount in `k8s/services/document-service.yaml`.
  - Status: PV/PVC are `Bound`, and `document-service` mounts `/app/uploads`.

- [x] 3.5 Verify inter-service communication through RabbitMQ
  - Verify event flow with a real request lifecycle and confirm publish/consume in RabbitMQ management UI.
  - Done when: messages are visible as produced and consumed for the tested flow.
  - Verified on 2026-05-07 after integration:
    - Queue: `document-service.servicerequest.created`
    - Queue stats: `publish=1`, `deliver=1`, `ack=1`
    - Exchange `egov.events` stats: `publish_in=1`, `publish_out=1`
    - Producer log: `Published RabbitMQ event servicerequest.created ...`
    - Consumer log: `Consumed RabbitMQ event servicerequest.created ...`

- [x] 3.6 Replicate services (2 replicas)
  - Status: `auth-service` is already `2` replicas.
  - Note: this repo uses `service-request-service` (not `application-service`), and it is already `2` replicas.

## Current Focus

Primary remaining item for this repo: `3.5` (RabbitMQ event-flow verification).
