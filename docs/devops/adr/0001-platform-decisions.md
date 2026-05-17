# ADR-0001: Platform Decisions for Local Cluster and CI

- Status: Accepted
- Date: 2026-05-07
- Scope: Local development platform, CI baseline, and observability direction

## Context

The project requires:

1. A reproducible local Kubernetes environment for all team members.
2. A practical CI baseline that validates code on push/PR.
3. A clear path for production-like monitoring in later phases.

## Decision 1: Minikube for local Kubernetes

We use Minikube as the standard local Kubernetes cluster.

### Why

- Simple onboarding and single-command startup.
- Works well with our local Docker-based workflow.
- Supports ingress, metrics-server, and standard Kubernetes manifests.

### Tradeoffs

- Single-node local cluster differs from production HA topology.
- Host-specific resource constraints can affect startup performance.

## Decision 2: Docker as container runtime/build tool

We use Docker images for all services and local orchestration.

### Why

- Consistent runtime packaging across backend, gateway, and frontend.
- Native fit with Minikube docker driver and Kubernetes deployments.
- Aligns with current repository Dockerfiles and Compose usage.

### Tradeoffs

- Image pull/build delays on slow networks.
- Desktop vs Engine differences can impact Linux developer experience.

## Decision 3: GitHub Actions for CI/CD automation

We use GitHub Actions as the CI/CD platform.

### Why

- Tight integration with repository events (push/PR).
- Supports multi-workflow quality gates and deployment pipelines.
- Minimal operational overhead for the team.

### Tradeoffs

- Requires careful secrets management.
- Runner environment differences can expose ordering/locale edge cases if scripts are not deterministic.

## Decision 4: Prometheus + Grafana for observability

We standardize on Prometheus for metrics collection and Grafana for dashboards.

### Why

- De facto Kubernetes-native monitoring stack.
- Strong ecosystem and easy extension as services grow.
- Enables shared visibility for service health, latency, and resource usage.

### Tradeoffs

- Additional cluster resources and maintenance overhead.
- Requires dashboard and alert curation to be truly useful.

## Consequences

- Team bootstrap is standardized around Minikube + Docker + Kubernetes manifests.
- CI quality checks run in GitHub Actions as the default gate.
- Observability implementation in later phases should target Prometheus/Grafana integration.

## Related

- `docs/devops/PHASE1_CHECKLIST.md`
- `k8s/`
- `.github/workflows/`
