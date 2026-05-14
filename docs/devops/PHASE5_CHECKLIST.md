# Phase 5: Monitoring & Observability (Sprint 5–6 — Weeks 9–12)

Goal: Prometheus scraping all services, Grafana dashboards live and alerting configured.

## Checklist

| #   | Task                                   | Details                                                                                                  | Status  |
| --- | -------------------------------------- | -------------------------------------------------------------------------------------------------------- | ------- |
| 5.1 | Implement Prometheus Metrics           | Add prometheus-net.AspNetCore to all services, expose /metrics endpoint.                                 | ✅ Done |
| 5.2 | Deploy Prometheus to K8s               | Create Deployment + ConfigMap in monitoring namespace with kubernetes_sd_configs for pod auto-discovery. | ✅ Done |
| 5.3 | Deploy Grafana to K8s                  | Create Deployment + Service in monitoring namespace. Configure Prometheus as default data source.        | ✅ Done |
| 5.4 | Build Service Health dashboard         | Panels: request rate, P95 latency, 5xx error rate per service.                                           | ✅ Done |
| 5.5 | Build .NET Runtime Internals dashboard | Panels: heap/non-heap memory, GC pause duration, active threads per service.                             | ✅ Done |
| 5.6 | Build Infrastructure dashboard         | Panels: pod CPU usage, pod memory usage, node resource consumption.                                      | ✅ Done |
| 5.7 | Build RabbitMQ dashboard               | Panels: queue depth, publish/consume rates, consumer count.                                              | ✅ Done |
| 5.8 | Configure alert rules                  | 5xx > 5%, P95 > 2000ms, heap > 80%, pod restarts > 3/hr, queue depth > 500.                              | ✅ Done |

## Implementation Details

### Prometheus Configuration

Prometheus will be configured to use Kubernetes Service Discovery (`kubernetes_sd_configs`) to automatically find and scrape pods annotated for scraping.

### Grafana Dashboards

- **Service Health:** Focused on the "Four Golden Signals" (Latency, Traffic, Errors, Saturation).
- **.NET Runtime Internals:** Deep dive into .NET runtime metrics (Runtime/Garbage Collection).
- **Infrastructure:** Resource utilization at the pod and node levels.
- **Messaging:** Monitoring RabbitMQ queues and exchange performance.

### Alerting Rules

Alerts will be defined in Prometheus `PrometheusRule` or via Grafana Alerting, targeting critical thresholds that impact user experience or system stability.

## Verification Commands

### Check Metrics Exposure

```bash
# Verify prometheus metrics endpoint for a specific service (example: auth-service)
kubectl exec -it <pod-name> -n egovernment -- curl localhost:8080/metrics
```

### Access Grafana

```bash
# Port-forward to access Grafana locally
kubectl port-forward svc/grafana 3000:3000 -n monitoring
```
