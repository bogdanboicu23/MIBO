#!/bin/bash

# DigitalOcean Setup Script for MIBO Platform
# This script sets up all DigitalOcean resources needed for the MIBO platform

set -e

echo "🚀 MIBO DigitalOcean Setup Script"
echo "================================="

# Check if doctl is installed
if ! command -v doctl &> /dev/null; then
    echo "Installing doctl CLI..."
    curl -sL https://github.com/digitalocean/doctl/releases/download/v1.98.0/doctl-1.98.0-linux-amd64.tar.gz | tar -xzv
    sudo mv doctl /usr/local/bin
fi

# Authenticate with DigitalOcean
echo "Authenticating with DigitalOcean..."
doctl auth init

# Variables
CLUSTER_NAME="mibo-k8s-cluster"
REGION="nyc3"
NODE_SIZE="s-2vcpu-4gb"
NODE_COUNT="3"
REGISTRY_NAME="mibo"
DB_NAME="mibo-db"
SPACES_NAME="mibo-storage"

echo "Creating resources in region: $REGION"

# Step 1: Create Container Registry
echo "1. Creating Container Registry..."
doctl registry create $REGISTRY_NAME --region $REGION || echo "Registry already exists"

# Step 2: Create Kubernetes Cluster
echo "2. Creating Kubernetes Cluster..."
doctl kubernetes cluster create $CLUSTER_NAME \
    --region $REGION \
    --node-pool "name=worker-pool;size=$NODE_SIZE;count=$NODE_COUNT;auto-scale=true;min-nodes=2;max-nodes=10" \
    --maintenance-window "any=00:00" \
    --set-current-context \
    --wait || echo "Cluster already exists"

# Step 3: Create Managed PostgreSQL Database
echo "3. Creating Managed PostgreSQL Database..."
doctl databases create $DB_NAME \
    --engine pg \
    --region $REGION \
    --size db-s-1vcpu-1gb \
    --version 15 \
    --num-nodes 1 || echo "Database already exists"

# Step 4: Create Spaces (Object Storage)
echo "4. Creating Spaces bucket..."
# Note: Spaces creation via API requires additional setup
echo "Please create Spaces bucket manually via DigitalOcean Console:"
echo "  Name: $SPACES_NAME"
echo "  Region: $REGION"
echo "  CDN: Enable"
echo ""

# Step 5: Configure kubectl
echo "5. Configuring kubectl..."
doctl kubernetes cluster kubeconfig save $CLUSTER_NAME

# Step 6: Create namespace
echo "6. Creating Kubernetes namespace..."
kubectl apply -f k8s/digitalocean/namespace.yaml

# Step 7: Install NGINX Ingress Controller
echo "7. Installing NGINX Ingress Controller..."
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.8.1/deploy/static/provider/do/deploy.yaml

# Step 8: Install cert-manager for SSL
echo "8. Installing cert-manager..."
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Wait for cert-manager to be ready
echo "Waiting for cert-manager to be ready..."
kubectl wait --for=condition=ready pod -l app.kubernetes.io/instance=cert-manager -n cert-manager --timeout=300s

# Step 9: Configure Container Registry Secret
echo "9. Configuring Container Registry secret..."
doctl registry kubernetes-manifest | kubectl apply -f -
kubectl patch serviceaccount default -n mibo -p '{"imagePullSecrets": [{"name": "registry-mibo"}]}'

# Step 10: Get database connection string
echo "10. Retrieving database connection details..."
DB_ID=$(doctl databases list --format ID --no-header | grep $DB_NAME || echo "")
if [ ! -z "$DB_ID" ]; then
    doctl databases connection $DB_ID --format Host,Port,User,Password,Database
fi

# Step 11: Create Load Balancer
echo "11. Load Balancer will be created automatically when services are deployed"

# Step 12: Setup monitoring (optional)
echo "12. Setting up monitoring..."
kubectl apply -f https://raw.githubusercontent.com/digitalocean/marketplace-kubernetes/master/stacks/kubernetes-monitoring-stack/yaml/kubernetes-monitoring-stack.yaml || echo "Monitoring stack already installed"

echo ""
echo "✅ DigitalOcean setup complete!"
echo ""
echo "Next steps:"
echo "1. Update k8s/digitalocean/secrets.yaml with your actual values"
echo "2. Apply secrets: kubectl apply -f k8s/digitalocean/secrets.yaml"
echo "3. Deploy services: kubectl apply -f k8s/digitalocean/"
echo "4. Configure DNS to point to your load balancer IPs"
echo ""
echo "Useful commands:"
echo "- View cluster: doctl kubernetes cluster list"
echo "- View nodes: kubectl get nodes"
echo "- View pods: kubectl get pods -n mibo"
echo "- View services: kubectl get svc -n mibo"
echo "- Get load balancer IP: kubectl get svc api-gateway -n mibo -o jsonpath='{.status.loadBalancer.ingress[0].ip}'"