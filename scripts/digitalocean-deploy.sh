#!/bin/bash

# DigitalOcean Deployment Script
# Deploys all MIBO services to DigitalOcean Kubernetes

set -e

echo "🚀 Deploying MIBO to DigitalOcean"
echo "================================="

# Configuration
CLUSTER_NAME="mibo-k8s-cluster"
NAMESPACE="mibo"
REGISTRY="registry.digitalocean.com/mibo"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to check command status
check_status() {
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ $1${NC}"
    else
        echo -e "${RED}✗ $1 failed${NC}"
        exit 1
    fi
}

# Step 1: Connect to cluster
echo -e "${YELLOW}Connecting to Kubernetes cluster...${NC}"
doctl kubernetes cluster kubeconfig save $CLUSTER_NAME
check_status "Connected to cluster"

# Step 2: Create namespace if it doesn't exist
echo -e "${YELLOW}Creating namespace...${NC}"
kubectl apply -f k8s/digitalocean/namespace.yaml
check_status "Namespace created"

# Step 3: Apply secrets
echo -e "${YELLOW}Applying secrets...${NC}"
echo -e "${RED}WARNING: Make sure to update secrets.yaml with your actual values!${NC}"
read -p "Have you updated the secrets.yaml file? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    kubectl apply -f k8s/digitalocean/secrets.yaml
    check_status "Secrets applied"
else
    echo "Please update secrets.yaml first!"
    exit 1
fi

# Step 4: Build and push Docker images
echo -e "${YELLOW}Building and pushing Docker images...${NC}"
echo "This will build all microservices and push to DigitalOcean Container Registry"

# Login to registry
doctl registry login

# Build services
SERVICES=("ApiGateway" "IdentityService" "ExpenseService" "NotificationService" "ReportingService")

for service in "${SERVICES[@]}"; do
    echo -e "${YELLOW}Building $service...${NC}"

    # Convert service name to lowercase for Docker image
    service_lower=$(echo "$service" | tr '[:upper:]' '[:lower:]' | tr -d ' ')

    # Build and push
    docker build -t $REGISTRY/$service_lower:latest ./src/MIBO.$service/
    docker push $REGISTRY/$service_lower:latest
    check_status "$service built and pushed"
done

# Build React client
echo -e "${YELLOW}Building React client...${NC}"
docker build -t $REGISTRY/client:latest ./src/MIBO.Client/client/
docker push $REGISTRY/client:latest
check_status "Client built and pushed"

# Step 5: Deploy services to Kubernetes
echo -e "${YELLOW}Deploying services to Kubernetes...${NC}"
kubectl apply -f k8s/digitalocean/api-gateway-deployment.yaml
kubectl apply -f k8s/digitalocean/all-services-deployment.yaml
check_status "Services deployed"

# Step 6: Wait for deployments to be ready
echo -e "${YELLOW}Waiting for deployments to be ready...${NC}"
kubectl wait --for=condition=available --timeout=300s deployment/api-gateway -n $NAMESPACE
kubectl wait --for=condition=available --timeout=300s deployment/identity-service -n $NAMESPACE
kubectl wait --for=condition=available --timeout=300s deployment/expense-service -n $NAMESPACE
kubectl wait --for=condition=available --timeout=300s deployment/notification-service -n $NAMESPACE
kubectl wait --for=condition=available --timeout=300s deployment/reporting-service -n $NAMESPACE
kubectl wait --for=condition=available --timeout=300s deployment/mibo-client -n $NAMESPACE
check_status "All deployments ready"

# Step 7: Get Load Balancer IPs
echo -e "${YELLOW}Getting Load Balancer IPs...${NC}"
echo ""
echo -e "${GREEN}=== Deployment Complete ===${NC}"
echo ""
echo "Service URLs:"
echo "------------"

# Get API Gateway IP
API_IP=$(kubectl get svc api-gateway -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "pending")
echo "API Gateway: http://$API_IP"

# Get Client IP
CLIENT_IP=$(kubectl get svc mibo-client -n $NAMESPACE -o jsonpath='{.status.loadBalancer.ingress[0].ip}' 2>/dev/null || echo "pending")
echo "React Client: http://$CLIENT_IP"

echo ""
echo "Pod Status:"
kubectl get pods -n $NAMESPACE

echo ""
echo -e "${YELLOW}Next Steps:${NC}"
echo "1. Configure your domain DNS to point to the load balancer IPs"
echo "2. Set up SSL certificates (cert-manager is already installed)"
echo "3. Monitor the application: kubectl logs -f deployment/api-gateway -n mibo"
echo "4. Scale services as needed: kubectl scale deployment/api-gateway --replicas=5 -n mibo"

echo ""
echo -e "${GREEN}✅ Deployment complete!${NC}"