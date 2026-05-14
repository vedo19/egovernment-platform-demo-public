#!/bin/bash

# One-click script to start the entire Kubernetes environment

echo "========================================"
echo "🚀 Starting Full Kubernetes Environment"
echo "========================================"

# 1. Start Minikube and enable addons
echo -e "\n[1/4] Starting Minikube..."
./scripts/k8s-manage.sh start

# 2. Build Docker images inside Minikube
echo -e "\n[2/4] Building Docker images..."
./scripts/k8s-build-local.sh

# 3. Deploy Application Stack (Namespaces, Configs, DBs, Services)
echo -e "\n[3/4] Deploying application stack..."
./scripts/k8s-manage.sh deploy

# 4. Deploy Monitoring Stack (Prometheus, Grafana)
echo -e "\n[4/4] Deploying monitoring stack..."
./scripts/k8s-monitoring-setup.sh

echo "========================================"
echo "✅ Environment startup initiated!"
echo "========================================"
echo "Wait a couple of minutes for all pods to reach 'Running' state."
echo ""
echo "Helpful commands:"
echo "- Check status:  ./scripts/k8s-manage.sh status"
echo "- Grafana UI:    kubectl port-forward svc/grafana 3000:3000 -n monitoring"
echo "- App UI:        Ensure you have 'egovernment.local' in your /etc/hosts mapped to $(minikube ip 2>/dev/null || echo '<minikube ip>')"
echo "========================================"
