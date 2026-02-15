#!/bin/bash

echo "PostgreSQL Deployment Script for MIBO"
echo "======================================"

# Check if kubectl is configured
if ! kubectl cluster-info &>/dev/null; then
    echo "Error: kubectl is not configured. Please configure kubectl first."
    exit 1
fi

# Function to deploy PostgreSQL
deploy_postgres() {
    local ENV=$1
    echo ""
    echo "Deploying PostgreSQL to $ENV namespace..."

    # Apply the PostgreSQL deployment
    kubectl apply -f postgres-${ENV}.yaml

    # Wait for PostgreSQL to be ready
    echo "Waiting for PostgreSQL to be ready in $ENV..."
    kubectl wait --for=condition=ready pod -l app=postgres -n $ENV --timeout=120s

    if [ $? -eq 0 ]; then
        echo "✓ PostgreSQL is ready in $ENV namespace"
    else
        echo "✗ PostgreSQL failed to start in $ENV namespace"
        return 1
    fi

    # Create JWT secret for IdentityService
    echo "Creating JWT secret for IdentityService in $ENV..."
    JWT_KEY=$(openssl rand -base64 32)
    kubectl create secret generic identity-secrets \
        --from-literal=jwt-key="$JWT_KEY" \
        --namespace=$ENV \
        --dry-run=client -o yaml | kubectl apply -f -

    echo "✓ JWT secret created for $ENV"
    echo "  JWT Key (save this securely): $JWT_KEY"

    return 0
}

# Main menu
echo ""
echo "Select deployment option:"
echo "1) Deploy to TEST namespace"
echo "2) Deploy to PROD namespace"
echo "3) Deploy to both TEST and PROD"
echo "4) Check deployment status"
echo "5) Exit"
echo ""
read -p "Enter your choice (1-5): " choice

case $choice in
    1)
        deploy_postgres "test"
        ;;
    2)
        echo ""
        echo "⚠️  WARNING: Production deployment!"
        echo "Make sure you have changed the default password in postgres-prod.yaml"
        read -p "Continue with production deployment? (yes/no): " confirm
        if [ "$confirm" == "yes" ]; then
            deploy_postgres "prod"
        else
            echo "Production deployment cancelled."
        fi
        ;;
    3)
        deploy_postgres "test"
        echo ""
        echo "⚠️  WARNING: Production deployment next!"
        echo "Make sure you have changed the default password in postgres-prod.yaml"
        read -p "Continue with production deployment? (yes/no): " confirm
        if [ "$confirm" == "yes" ]; then
            deploy_postgres "prod"
        else
            echo "Production deployment cancelled."
        fi
        ;;
    4)
        echo ""
        echo "TEST namespace status:"
        kubectl get pods -l app=postgres -n test
        kubectl get svc postgres -n test
        kubectl get secret identity-secrets -n test &>/dev/null && echo "✓ JWT secret exists in test" || echo "✗ JWT secret missing in test"

        echo ""
        echo "PROD namespace status:"
        kubectl get pods -l app=postgres -n prod
        kubectl get svc postgres -n prod
        kubectl get secret identity-secrets -n prod &>/dev/null && echo "✓ JWT secret exists in prod" || echo "✗ JWT secret missing in prod"
        ;;
    5)
        echo "Exiting..."
        exit 0
        ;;
    *)
        echo "Invalid choice. Exiting..."
        exit 1
        ;;
esac

echo ""
echo "======================================"
echo "Deployment complete!"
echo ""
echo "Next steps:"
echo "1. Run database migrations for IdentityService"
echo "2. Restart the IdentityService and CalendarAgent deployments"
echo "   kubectl rollout restart deployment identity-service -n test"
echo "   kubectl rollout restart deployment calendar-agent -n test"
echo ""
echo "To test PostgreSQL connection:"
echo "kubectl run -it --rm psql --image=postgres:16-alpine --namespace=test -- psql postgresql://postgres:test-postgres-pwd-2024@postgres:5432/mibo_identity_test"