#!/bin/bash

# Kubernetes Management Script
# Purpose: Start/Stop Minikube and apply all manifests

ACTION=$1

function show_usage {
    echo "Usage: ./scripts/k8s-manage.sh [start|stop|deploy|status]"
    echo "  start   - Starts minikube and enables necessary addons"
    echo "  stop    - Stops minikube"
    echo "  deploy  - Applies all namespaces, storage, and service manifests"
    echo "  status  - Shows the status of all pods across namespaces"
}

if [[ -z "$ACTION" ]]; then
    show_usage
    exit 1
fi

case "$ACTION" in
    start)
        echo "Starting Minikube..."
        minikube start --cpus=2 --memory=4096
        echo "Enabling addons..."
        minikube addons enable ingress
        minikube addons enable metrics-server
        ;;
    stop)
        echo "Stopping Minikube..."
        minikube stop
        ;;
    deploy)
        echo "Applying Namespaces..."
        kubectl apply -f k8s/namespaces/
        
        echo "Applying Configs..."
        kubectl apply -f k8s/config/
        
        echo "Applying Storage..."
        kubectl apply -f k8s/storage/
        
        echo "Applying Postgres..."
        kubectl apply -f k8s/postgres/
        
        echo "Applying RabbitMQ..."
        kubectl apply -f k8s/rabbitmq/
        
        echo "Applying Services..."
        kubectl apply -f k8s/services/
        
        echo "Applying Ingress..."
        kubectl apply -f k8s/ingress/
        ;;
    status)
        echo "--- Pods in egovernment ---"
        kubectl get pods -n egovernment
        echo ""
        echo "--- Pods in messaging ---"
        kubectl get pods -n messaging
        echo ""
        echo "--- Pods in monitoring ---"
        kubectl get pods -n monitoring
        ;;
    *)
        show_usage
        exit 1
        ;;
esac
