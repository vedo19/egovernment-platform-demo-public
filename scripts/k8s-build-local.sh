#!/bin/bash

# Script to build .NET services and load them into Minikube
# Note: Run this from the project root

echo "Configuring shell to use Minikube's Docker daemon..."
eval $(minikube docker-env)

echo "Building AuthService..."
docker build -t auth-service:dev -f src/AuthService/Dockerfile .

echo "Building ApiGateway..."
docker build -t api-gateway:dev -f src/ApiGateway/Dockerfile .

echo "Building CitizenService..."
docker build -t citizen-service:dev -f src/CitizenService/Dockerfile .

echo "Building DocumentService..."
docker build -t document-service:dev -f src/DocumentService/Dockerfile .

echo "Building ServiceRequestService..."
docker build -t service-request-service:dev -f src/ServiceRequestService/Dockerfile .

echo "Building NotificationService..."
docker build -t notification-service:dev -f src/NotificationService/Dockerfile .

echo "Building Frontend..."
docker build -t frontend:dev src/frontend

echo ""
echo "Images built and loaded into Minikube. Restarting deployments..."
kubectl rollout restart deployment -n egovernment
