# Migration Guide: Azure AKS to DigitalOcean Kubernetes

## Overview
This guide explains how to migrate your MIBO deployment from Azure AKS to DigitalOcean Kubernetes (DOKS).

## Prerequisites
- DigitalOcean account
- `doctl` CLI installed
- `kubectl` installed

## Step 1: Create DigitalOcean Kubernetes Cluster

```bash
# Install DigitalOcean CLI
brew install doctl  # macOS
# or
snap install doctl  # Linux

# Authenticate
doctl auth init

# Create cluster (adjust region and size as needed)
doctl kubernetes cluster create mibo-cluster \
  --region fra1 \
  --version latest \
  --node-pool "name=default;size=s-2vcpu-4gb;count=3"

# Available regions: nyc1, nyc3, sfo3, ams3, fra1, lon1, sgp1, etc.
# Available sizes: s-1vcpu-2gb, s-2vcpu-4gb, s-4vcpu-8gb, etc.
```

## Step 2: Get and Configure Kubeconfig

```bash
# Save cluster config
doctl kubernetes cluster kubeconfig save mibo-cluster

# Verify connection
kubectl get nodes

# Base64 encode for GitHub Secrets
cat ~/.kube/config | base64 -w 0 > kubeconfig-do.txt
```

## Step 3: Update GitHub Secrets

1. Go to GitHub repository → Settings → Secrets and variables → Actions
2. Add/Update these secrets:
   - `KUBECONFIG_DEV`: Contents of kubeconfig-do.txt
   - `KUBECONFIG_STAGING`: (if using staging cluster)
   - `KUBECONFIG_PROD`: (if using production cluster)

## Step 4: Create Storage Class for DigitalOcean

Create `deploy/base/do-storage-class.yaml`:

```yaml
apiVersion: storage.k8s.io/v1
kind: StorageClass
metadata:
  name: do-block-storage
  annotations:
    storageclass.kubernetes.io/is-default-class: "true"
provisioner: dobs.csi.digitalocean.com
parameters:
  type: pd-ssd
allowVolumeExpansion: true
```

## Step 5: Update Database Configurations

If using PostgreSQL with persistent storage, update PVC to use DO storage:

Create `deploy/base/postgres-pvc.yaml`:

```yaml
apiVersion: v1
kind: PersistentVolumeClaim
metadata:
  name: postgres-pvc
  namespace: mibo
spec:
  accessModes:
    - ReadWriteOnce
  storageClassName: do-block-storage
  resources:
    requests:
      storage: 10Gi
```

## Step 6: Setup Load Balancer

DigitalOcean automatically provisions Load Balancers for Services of type LoadBalancer.

Update `deploy/base/api-gateway-service.yaml`:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: api-gateway
  namespace: mibo
  annotations:
    # DigitalOcean specific annotations
    service.beta.kubernetes.io/do-loadbalancer-protocol: "tcp"
    service.beta.kubernetes.io/do-loadbalancer-size-slug: "lb-small"
    service.beta.kubernetes.io/do-loadbalancer-disable-lets-encrypt-dns-records: "false"
spec:
  type: LoadBalancer
  selector:
    app: api-gateway
  ports:
    - port: 80
      targetPort: 8080
      protocol: TCP
```

## Step 7: Domain and SSL Configuration

### Option A: Use DigitalOcean's Let's Encrypt Integration

```yaml
apiVersion: v1
kind: Service
metadata:
  annotations:
    service.beta.kubernetes.io/do-loadbalancer-certificate-id: "your-cert-id"
    service.beta.kubernetes.io/do-loadbalancer-redirect-http-to-https: "true"
```

### Option B: Use Cert-Manager

```bash
# Install cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.13.0/cert-manager.yaml

# Create ClusterIssuer for Let's Encrypt
cat <<EOF | kubectl apply -f -
apiVersion: cert-manager.io/v1
kind: ClusterIssuer
metadata:
  name: letsencrypt-prod
spec:
  acme:
    server: https://acme-v02.api.letsencrypt.org/directory
    email: your-email@example.com
    privateKeySecretRef:
      name: letsencrypt-prod
    solvers:
    - http01:
        ingress:
          class: nginx
EOF
```

## Step 8: Container Registry Options

### Option A: Use DigitalOcean Container Registry

```bash
# Create registry
doctl registry create mibo-registry

# Login to registry
doctl registry login

# Tag and push images
docker tag mibo-api-gateway:latest registry.digitalocean.com/mibo-registry/api-gateway:latest
docker push registry.digitalocean.com/mibo-registry/api-gateway:latest
```

### Option B: Continue Using GitHub Container Registry

No changes needed - DOKS can pull from ghcr.io. Just ensure images are public or create pull secret:

```bash
kubectl create secret docker-registry ghcr-secret \
  --docker-server=ghcr.io \
  --docker-username=YOUR_GITHUB_USERNAME \
  --docker-password=YOUR_GITHUB_TOKEN \
  --namespace=mibo
```

## Step 9: Update CI/CD Pipeline (Optional)

If you want to add DigitalOcean-specific features:

```yaml
# In .github/workflows/ci-cd-pipeline.yml
- name: Install doctl
  uses: digitalocean/action-doctl@v2
  with:
    token: ${{ secrets.DIGITALOCEAN_ACCESS_TOKEN }}

- name: Save DigitalOcean kubeconfig
  run: doctl kubernetes cluster kubeconfig save mibo-cluster
```

## Step 10: Database Options

### Option A: Use DigitalOcean Managed Databases

```bash
# Create managed PostgreSQL
doctl databases create mibo-db \
  --engine pg \
  --region fra1 \
  --size db-s-1vcpu-1gb \
  --version 15

# Get connection details
doctl databases connection mibo-db
```

### Option B: Run PostgreSQL in Kubernetes

Use the PVC configuration from Step 5.

## Step 11: Monitoring and Logging

DigitalOcean provides built-in monitoring:

```bash
# Enable monitoring
doctl kubernetes cluster update mibo-cluster \
  --enable-monitoring
```

## Cost Comparison

### DigitalOcean (Approximate Monthly)
- 3x s-2vcpu-4gb nodes: $72
- Load Balancer: $12
- Block Storage (100GB): $10
- **Total: ~$94/month**

### Azure AKS (Approximate Monthly)
- 3x Standard_B2s: ~$90
- Load Balancer: $25
- Storage (100GB): $5
- **Total: ~$120/month**

## Migration Checklist

- [ ] Create DigitalOcean Kubernetes cluster
- [ ] Configure kubectl access
- [ ] Update GitHub Secrets with new kubeconfig
- [ ] Deploy services to new cluster
- [ ] Configure DNS to point to new Load Balancer
- [ ] Test all services
- [ ] Monitor for 24-48 hours
- [ ] Decommission Azure resources

## Rollback Plan

Keep Azure resources running in parallel until confident:
1. Deploy to both clusters
2. Use DNS to control traffic
3. Gradually shift traffic to DigitalOcean
4. Keep Azure as backup for 1 week
5. Decommission Azure after verification

## Additional Resources

- [DigitalOcean Kubernetes Documentation](https://docs.digitalocean.com/products/kubernetes/)
- [doctl CLI Reference](https://docs.digitalocean.com/reference/doctl/)
- [DigitalOcean Container Registry](https://docs.digitalocean.com/products/container-registry/)
- [DigitalOcean Managed Databases](https://docs.digitalocean.com/products/databases/)