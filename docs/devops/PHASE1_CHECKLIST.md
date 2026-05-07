# Phase 1 Checklist

Sprint: Weeks 1-2  
Goal: Bootstrap local Kubernetes environment and establish baseline CI.

## Task Tracker

- [ ] 1.1 Install and verify Minikube
  - Command:
    ```bash
    minikube start --cpus=4 --memory=7000 --disk-size=30g --driver=docker
    kubectl cluster-info
    kubectl get nodes
    ```
  - Done when: `kubectl get nodes` shows `Ready`.

- [ ] 1.2 Enable Minikube addons
  - Command:
    ```bash
    minikube addons enable ingress
    minikube addons enable metrics-server
    minikube addons list
    ```
  - Done when: both addons are `enabled`.

- [ ] 1.3 Create Kubernetes namespaces
  - Manifests:
    - `k8s/namespaces/egovernment.yaml`
    - `k8s/namespaces/egovernment-db.yaml`
    - `k8s/namespaces/messaging.yaml`
    - `k8s/namespaces/monitoring.yaml`
  - Command:
    ```bash
    kubectl apply -f k8s/namespaces/
    kubectl get ns
    ```
  - Done when: `egovernment`, `egovernment-db`, `messaging`, `monitoring` exist (plus ingress namespace).

- [ ] 1.4 Confirm monorepo structure
  - Required paths:
    - `src/`
    - `src/frontend/`
    - `k8s/`
    - `docs/`
    - `.github/workflows/`
    - `Makefile`
  - Done when: team can clone and navigate structure without setup confusion.

- [ ] 1.5 Scaffold CI pipeline baseline
  - Workflow: `.github/workflows/auth-service-ci.yml`
  - Expected steps:
    - checkout
    - runtime setup
    - dependency restore
    - build
  - Done when: push to `main` triggers successful workflow run.

- [ ] 1.6 Configure local DNS for ingress
  - Command:
    ```bash
    echo "$(minikube ip) egovernment.local" | sudo tee -a /etc/hosts
    ```
  - Verify:
    ```bash
    curl http://egovernment.local
    ```
  - Done when: request resolves against Minikube ingress.

- [x] 1.7 Document Architecture Decision Record (ADR)
  - Location: `docs/devops/adr/`
  - Suggested file: `docs/devops/adr/0001-platform-decisions.md`
  - Cover:
    - Minikube
    - Docker
    - GitHub Actions
    - Prometheus/Grafana
  - Done when: ADR is committed and linked from team docs.

## Notes

- Current project stack is `.NET + Node`, so CI runtime setup should match that stack.
- If Docker memory is below requested Minikube memory, lower `--memory` (for example `7000`) or increase Docker allocation.
