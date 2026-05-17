# Phase 2 Checklist

Sprint: Weeks 3-4  
Goal: Dockerize services and stand up database/messaging infrastructure on Kubernetes.

## Scope Alignment

This repository uses `.NET + Node`, not Java/Gradle services.  
Phase 2 tasks below are mapped to the current stack.

## Task Tracker

- [x] 2.1 Standardize multi-stage service Dockerfiles
  - Target: backend services use 2-stage Dockerfiles (`sdk` build -> `aspnet` runtime), non-root user, container-friendly runtime flags.
  - Verify:
    ```bash
    docker build -f src/AuthService/Dockerfile -t auth-service:dev .
    docker build -f src/CitizenService/Dockerfile -t citizen-service:dev .
    ```
  - Done when: builds complete successfully and images start without root user warnings.

- [x] 2.2 Complete local `docker-compose.yml` stack
  - Must include:
    - RabbitMQ
    - 6 PostgreSQL instances (`auth`, `citizen`, `request`, `document`, `audit`, `notification`)
    - all application services (`auth`, `citizen`, `service-request`, `document`, `api-gateway`, `frontend`)
  - Verify:
    ```bash
    docker compose up -d --build
    docker compose ps
    ```
  - Done when: full stack starts locally with healthy dependencies.

- [x] 2.3 Deploy PostgreSQL StatefulSets (K8s)
  - Namespace: `egovernment-db`
  - Manifests: `k8s/postgres/*.yaml`
  - Verify:
    ```bash
    kubectl apply -f k8s/postgres/
    kubectl get statefulsets -n egovernment-db
    ```
  - Done when: 6 PostgreSQL StatefulSets are `Ready`.

- [x] 2.4 Deploy RabbitMQ StatefulSet (K8s)
  - Namespace: `messaging`
  - Manifest: `k8s/rabbitmq/rabbitmq.yaml`
  - Verify:
    ```bash
    kubectl apply -f k8s/rabbitmq/
    kubectl get statefulsets -n messaging
    kubectl port-forward -n messaging svc/rabbitmq 15672:15672
    ```
  - Done when: RabbitMQ management UI is reachable on `http://localhost:15672`.

- [x] 2.5 Create/verify K8s ConfigMaps and Secrets
  - Manifests:
    - `k8s/config/configmap-app.yaml`
    - `k8s/config/secret-app.yaml`
  - Verify:
    ```bash
    kubectl apply -f k8s/config/
    kubectl get configmaps -n egovernment
    kubectl get secrets -n egovernment
    ```
  - Done when: required config and secret entries exist for app services.

- [x] 2.6 Build images inside Minikube runtime
  - Build at minimum: `auth-service`, `citizen-service` (recommended: all backend + frontend images used by manifests).
  - Command:
    ```bash
    eval "$(minikube -p minikube docker-env)"
    docker build -f src/AuthService/Dockerfile -t auth-service:dev .
    docker build -f src/CitizenService/Dockerfile -t citizen-service:dev .
    ```
  - Verify:
    ```bash
    minikube ssh -- docker images | grep -E 'auth-service|citizen-service'
    ```
  - Done when: images are visible in Minikube Docker daemon.

- [x] 2.7 Deploy first 2 services to Kubernetes
  - Services: `auth-service`, `citizen-service`
  - Verify:
    ```bash
    kubectl apply -f k8s/services/auth-service.yaml
    kubectl apply -f k8s/services/citizen-service.yaml
    kubectl get pods -n egovernment
    kubectl port-forward -n egovernment svc/auth-service 5001:8080
    kubectl port-forward -n egovernment svc/citizen-service 5002:8080
    ```
  - Health checks:
    ```bash
    curl -sS http://localhost:5001/healthz
    curl -sS http://localhost:5002/healthz
    ```
  - Done when: both deployments are `Running`/`Ready` and health endpoints return `200`.

## Notes

- If pods are stuck in `ErrImagePull/ImagePullBackOff`, rebuild images with `minikube docker-env` and restart deployments.
- If pods are `Running` but not `Ready`, inspect probe paths in `k8s/services/*.yaml` and check container logs.
