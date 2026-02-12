# CI/CD Pipeline Documentation for MIBO Microservices

## Overview

This repository uses GitHub Actions for continuous integration and deployment of the MIBO microservices platform.

## Architecture Decisions

### Why GitHub Actions?
- Native GitHub integration
- Free for public repositories
- Matrix builds for parallel processing
- Excellent marketplace ecosystem
- Built-in secrets management

### Pipeline Strategy
- **Monorepo with path filters**: Only rebuild affected services
- **Reusable workflows**: DRY principle for common tasks
- **Multi-stage Docker builds**: Optimized container images
- **GitOps ready**: Kustomize + Helm for K8s deployments

## Workflows

### 1. Main CI/CD Pipeline (`ci-cd-pipeline.yml`)
**Triggers**: Push to main/develop, Pull requests, Manual dispatch
**Purpose**: Primary build, test, and deploy pipeline

**Stages**:
1. **Detect Changes**: Uses path filters to identify modified services
2. **Build Shared Libraries**: Compiles common dependencies first
3. **Build & Test Services**: Parallel builds with matrix strategy
4. **Security Scan**: Vulnerability scanning with Trivy
5. **Build Docker Images**: Multi-platform container builds
6. **Deploy**: Environment-based Kubernetes deployment

### 2. Pull Request Validation (`pr-validation.yml`)
**Triggers**: Pull request events
**Purpose**: Quality gates before merge

**Checks**:
- PR title validation (semantic commits)
- Code formatting (dotnet-format)
- Code analysis (Roslyn analyzers)
- Dependency review
- Test coverage reporting
- License compliance
- Dockerfile linting
- Kubernetes manifest validation

### 3. Security Scanning (`security-scan.yml`)
**Triggers**: Daily schedule, dependency updates
**Purpose**: Continuous security monitoring

**Scans**:
- CodeQL analysis (SAST)
- OWASP dependency check
- Container vulnerability scanning
- Secret detection (TruffleHog, Gitleaks)
- Infrastructure as Code scanning (Checkov, Terrascan)

### 4. Deployment Workflow (`deploy.yml`)
**Triggers**: Manual dispatch
**Purpose**: Controlled environment deployments

**Features**:
- Environment selection (dev/staging/production)
- Service selection (individual or all)
- Dry-run capability
- Smoke testing
- Deployment recording
- Slack notifications

### 5. Release Management (`release.yml`)
**Triggers**: Version tags (v*), Manual dispatch
**Purpose**: Automated release process

**Steps**:
1. Create GitHub release with changelog
2. Build release artifacts
3. Push versioned Docker images
4. Package Helm charts
5. Auto-deploy to staging

## Setup Instructions

### 1. GitHub Secrets Configuration

Add these secrets to your GitHub repository:

```bash
# Kubernetes configs (base64 encoded)
KUBECONFIG_DEV
KUBECONFIG_STAGING
KUBECONFIG_PROD

# Optional: External services
SONAR_TOKEN          # SonarCloud integration
SNYK_TOKEN          # Snyk security scanning
FOSSA_API_KEY       # License compliance
SLACK_WEBHOOK       # Notifications
CODECOV_TOKEN       # Code coverage
```

### 2. Branch Protection Rules

Configure for `main` branch:
- Require PR reviews (1+)
- Require status checks:
  - `code-quality`
  - `test-coverage`
  - `security-scan`
- Dismiss stale reviews
- Include administrators

### 3. Environment Configuration

Create GitHub environments:
- `dev` - Auto-deploy from develop
- `staging` - Auto-deploy from tags
- `production` - Manual approval required

### 4. Container Registry

GitHub Container Registry (ghcr.io) is configured by default:
- Public packages for open-source
- Automatic cleanup policies
- Multi-architecture support

## Usage Guide

### Building a Single Service

```bash
# Trigger via GitHub CLI
gh workflow run ci-cd-pipeline.yml \
  -f services="api-gateway" \
  -f environment="dev"
```

### Creating a Release

```bash
# Tag-based release
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0

# Manual release
gh workflow run release.yml -f version="1.0.0"
```

### Manual Deployment

```bash
# Deploy all services to staging
gh workflow run deploy.yml \
  -f environment="staging" \
  -f services="all" \
  -f image-tag="v1.0.0"

# Deploy specific service with dry-run
gh workflow run deploy.yml \
  -f environment="production" \
  -f services="api-gateway" \
  -f image-tag="v1.0.0" \
  -f dry-run=true
```

## Best Practices

### 1. Commit Messages
Use conventional commits for automatic changelog:
```
feat: Add user authentication
fix: Resolve memory leak in cache service
docs: Update API documentation
chore: Upgrade dependencies
```

### 2. Docker Image Tagging
- `latest` - Latest from main branch
- `develop` - Latest from develop branch
- `v1.0.0` - Specific version release
- `main-abc123` - Branch + commit SHA
- `pr-42` - Pull request builds

### 3. Secret Management
- Use GitHub Secrets for sensitive data
- Rotate secrets quarterly
- Use separate secrets per environment
- Enable secret scanning

### 4. Performance Optimization
- Cache Docker layers with BuildKit
- Cache NuGet packages
- Use matrix builds for parallelism
- Implement incremental builds

## Monitoring & Troubleshooting

### Viewing Pipeline Status

```bash
# List recent workflow runs
gh run list

# View specific run details
gh run view <run-id>

# Watch run in real-time
gh run watch <run-id>

# Download logs
gh run download <run-id>
```

### Common Issues

**Issue**: Docker build fails with "no space left"
**Solution**: Add cleanup step or increase runner disk space

**Issue**: Deployment timeout
**Solution**: Increase `--timeout` in Helm command or check K8s cluster resources

**Issue**: Path filter not detecting changes
**Solution**: Check filter patterns match your file structure

**Issue**: Security scan failures
**Solution**: Review vulnerability reports, update dependencies, or add suppressions

## Metrics & KPIs

Track these metrics for pipeline health:

- **Build Success Rate**: Target >95%
- **Average Build Time**: Target <10 minutes
- **Deployment Frequency**: Daily for dev, weekly for production
- **Mean Time to Recovery**: Target <1 hour
- **Test Coverage**: Target >80%
- **Security Vulnerabilities**: Zero critical, <5 high

## Migration from Other CI/CD Systems

### From Azure DevOps
1. Export pipeline YAML
2. Convert syntax to GitHub Actions
3. Migrate service connections to secrets
4. Update badge URLs

### From Jenkins
1. Convert Jenkinsfile to workflow YAML
2. Replace Jenkins plugins with Actions
3. Migrate credentials to secrets
4. Update webhook configurations

### From GitLab CI
1. Convert `.gitlab-ci.yml` to workflows
2. Map GitLab variables to secrets
3. Update container registry references
4. Migrate deployment environments

## Future Enhancements

- [ ] Implement blue-green deployments
- [ ] Add performance testing stage
- [ ] Integrate with APM tools
- [ ] Implement automated rollbacks
- [ ] Add cost optimization reporting
- [ ] Implement progressive delivery
- [ ] Add chaos engineering tests

## Support

For issues or questions:
1. Check workflow run logs
2. Review this documentation
3. Open an issue with `ci/cd` label
4. Contact DevOps team on Slack #mibo-devops