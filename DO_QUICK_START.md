# DigitalOcean Quick Start Guide

## Prerequisites
✅ DigitalOcean Kubernetes Cluster created
✅ DigitalOcean Container Registry created
✅ `doctl` CLI installed

## Step 1: Get Your DigitalOcean Info

1. **Get your Registry name:**
```bash
doctl registry get
# Look for the "Name" field
```

2. **Get your Cluster name:**
```bash
doctl kubernetes cluster list
# Look for the "Name" field
```

3. **Create an API token:**
- Go to [DigitalOcean API Tokens](https://cloud.digitalocean.com/account/api/tokens)
- Generate new token with read/write access
- Save it securely

## Step 2: Configure GitHub Secrets

Add this secret to your GitHub repository:
- Go to Settings → Secrets and variables → Actions
- Add: `DIGITALOCEAN_ACCESS_TOKEN` = your API token

## Step 3: Update Configuration Files

### Update the GitHub Actions workflow:
Edit `.github/workflows/deploy-do-simple.yml`:
```yaml
env:
  DO_REGISTRY_NAME: your-actual-registry-name  # Replace with your registry
  CLUSTER_NAME: your-actual-cluster-name       # Replace with your cluster
```

### Update the deployment script:
Edit `deploy-to-do.sh`:
```bash
REGISTRY_NAME="your-actual-registry-name"  # Replace with your registry
CLUSTER_NAME="your-actual-cluster-name"    # Replace with your cluster
```

### Update Kubernetes files:
Edit `k8s/simple/api-gateway.yaml` and `k8s/simple/client.yaml`:
```yaml
image: registry.digitalocean.com/YOUR_REGISTRY_NAME/api-gateway:latest
```

## Step 4: Deploy Manually (First Time)

```bash
# Make script executable
chmod +x deploy-to-do.sh

# Run deployment
./deploy-to-do.sh
```

## Step 5: Automatic Deployment

Push to main branch:
```bash
git add .
git commit -m "Setup DigitalOcean deployment"
git push origin main
```

GitHub Actions will automatically:
1. Build Docker images
2. Push to your registry
3. Deploy to Kubernetes

## Step 6: Check Your Deployment

```bash
# Connect to cluster
doctl kubernetes cluster kubeconfig save your-cluster-name

# Check pods
kubectl get pods

# Check services (get URLs)
kubectl get svc

# View logs
kubectl logs deployment/api-gateway
kubectl logs deployment/client
```

## Getting Your App URLs

After deployment, get your app URLs:
```bash
# API Gateway URL
kubectl get svc api-gateway -o jsonpath='{.status.loadBalancer.ingress[0].ip}'

# Client URL
kubectl get svc client -o jsonpath='{.status.loadBalancer.ingress[0].ip}'
```

## Troubleshooting

### Cannot connect to cluster
```bash
doctl kubernetes cluster kubeconfig save your-cluster-name
```

### Cannot push images
```bash
doctl registry login
```

### Pods not starting
```bash
kubectl describe pod <pod-name>
kubectl logs <pod-name>
```

### Update deployment with new image
```bash
kubectl rollout restart deployment/api-gateway
kubectl rollout restart deployment/client
```

## Next Steps

Once basic deployment works:
1. Add other microservices to the deployment
2. Set up database (Managed PostgreSQL)
3. Configure domain names
4. Add SSL certificates
5. Set up monitoring

## Cost

Basic setup costs (monthly):
- Kubernetes: ~$40 (smallest 2-node cluster)
- Container Registry: $5
- Load Balancers: $12 each
- **Total: ~$70/month**

---

**That's it!** Your app should be running on DigitalOcean. 🎉