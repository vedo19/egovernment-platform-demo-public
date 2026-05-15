#!/bin/bash

# Monitoring Setup Script
# Purpose: Deploy Prometheus and Grafana manifests

echo "Applying Monitoring RBAC..."
kubectl apply -f k8s/monitoring/prometheus-rbac.yaml

echo "Applying Prometheus Config..."
kubectl apply -f k8s/monitoring/prometheus-config.yaml

echo "Applying Prometheus Deployment..."
kubectl apply -f k8s/monitoring/prometheus-deployment.yaml

echo "Applying Grafana Data Sources..."
kubectl apply -f k8s/monitoring/grafana-datasources.yaml

echo "Applying Grafana Dashboards..."
kubectl apply -f k8s/monitoring/grafana-dashboards-config.yaml

echo "Applying Grafana Deployment..."
kubectl apply -f k8s/monitoring/grafana-deployment.yaml

echo ""
echo "Monitoring stack applied. Use 'kubectl get pods -n monitoring' to check status."
echo "To access Grafana: kubectl port-forward svc/grafana 3000:3000 -n monitoring"
echo "To access Prometheus: kubectl port-forward svc/prometheus 9090:9090 -n monitoring"
