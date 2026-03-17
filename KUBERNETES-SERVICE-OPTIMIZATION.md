# Kubernetes Service Architecture Optimization

## Current vs Proposed Architecture

### Current Architecture (5 Services)
```
Client → API Gateway → [IdentityService, ConversationService ← → LangChainService]
                                            ↓
                                    [NATS, Redis, MongoDB]
```

### Proposed Architecture (7-8 Services)
```
Client → API Gateway → [IdentityService]
                    → [ConversationOrchestrator] → [MessageService]
                                               → [ToolExecutionService]
                                               → [UICompositionService]
                                               → [LangChainService]
                                               → [DataAggregationService]
```

## Service Separation Recommendations

### 1. Split ConversationService into 3 Services

#### A. **ConversationOrchestrator** (Lightweight Coordinator)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: conversation-orchestrator
  namespace: test
spec:
  replicas: 2
  selector:
    matchLabels:
      app: conversation-orchestrator
  template:
    metadata:
      labels:
        app: conversation-orchestrator
    spec:
      containers:
      - name: conversation-orchestrator
        image: registry.digitalocean.com/mibo/conversation-orchestrator:test-latest
        ports:
        - containerPort: 8080
        resources:
          requests:
            memory: "128Mi"
            cpu: "50m"
          limits:
            memory: "256Mi"
            cpu: "200m"
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

**Responsibilities:**
- Route messages to appropriate services
- Maintain conversation flow state
- Coordinate between services
- Handle SignalR connections

#### B. **MessageService** (Persistence & History)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: message-service
  namespace: test
spec:
  replicas: 2
  selector:
    matchLabels:
      app: message-service
  template:
    metadata:
      labels:
        app: message-service
    spec:
      containers:
      - name: message-service
        image: registry.digitalocean.com/mibo/message-service:test-latest
        ports:
        - containerPort: 8081
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "300m"
```

**Responsibilities:**
- Store/retrieve conversation history
- Manage MongoDB persistence
- Handle message search and filtering
- Implement message retention policies

#### C. **ToolExecutionService** (Action Processing)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: tool-execution-service
  namespace: test
spec:
  replicas: 3  # More replicas for parallel tool execution
  selector:
    matchLabels:
      app: tool-execution-service
  template:
    metadata:
      labels:
        app: tool-execution-service
    spec:
      containers:
      - name: tool-execution-service
        image: registry.digitalocean.com/mibo/tool-execution-service:test-latest
        ports:
        - containerPort: 8082
        resources:
          requests:
            memory: "256Mi"
            cpu: "200m"
          limits:
            memory: "1Gi"
            cpu: "1000m"  # Higher CPU for tool execution
```

**Responsibilities:**
- Execute tools requested by LangChain
- Manage external API calls
- Handle timeouts and retries
- Tool result formatting

### 2. Add New Support Services

#### D. **UICompositionService** (Dynamic UI Generation)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: ui-composition-service
  namespace: test
spec:
  replicas: 2
  selector:
    matchLabels:
      app: ui-composition-service
  template:
    metadata:
      labels:
        app: ui-composition-service
    spec:
      containers:
      - name: ui-composition-service
        image: registry.digitalocean.com/mibo/ui-composition-service:test-latest
        ports:
        - containerPort: 8083
        resources:
          requests:
            memory: "128Mi"
            cpu: "50m"
          limits:
            memory: "256Mi"
            cpu: "200m"
```

**Responsibilities:**
- Generate dynamic UI components
- Manage UI templates
- Handle chart/visualization rendering
- Provide UI state management

#### E. **DataAggregationService** (BFF Pattern)
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: data-aggregation-service
  namespace: test
spec:
  replicas: 2
  selector:
    matchLabels:
      app: data-aggregation-service
  template:
    metadata:
      labels:
        app: data-aggregation-service
    spec:
      containers:
      - name: data-aggregation-service
        image: registry.digitalocean.com/mibo/data-aggregation-service:test-latest
        ports:
        - containerPort: 8084
        resources:
          requests:
            memory: "256Mi"
            cpu: "100m"
          limits:
            memory: "512Mi"
            cpu: "500m"
```

**Responsibilities:**
- Aggregate data from multiple sources
- Implement GraphQL endpoint for flexible queries
- Cache aggregated results
- Provide unified API for frontend

### 3. Optimize Existing Services

#### Enhanced LangChainService Configuration
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: langchain-service
  namespace: test
spec:
  replicas: 2  # Increase from 1 for redundancy
  selector:
    matchLabels:
      app: langchain-service
  template:
    metadata:
      labels:
        app: langchain-service
    spec:
      containers:
      - name: langchain-service
        image: registry.digitalocean.com/mibo/langchain-service:test-latest
        ports:
        - containerPort: 8088
        resources:
          requests:
            memory: "512Mi"  # Increase for AI operations
            cpu: "500m"
          limits:
            memory: "2Gi"
            cpu: "2000m"
```

### 4. Infrastructure Optimizations

#### A. Add HorizontalPodAutoscaler
```yaml
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: conversation-orchestrator-hpa
  namespace: test
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: conversation-orchestrator
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: tool-execution-hpa
  namespace: test
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: tool-execution-service
  minReplicas: 3
  maxReplicas: 20  # Higher max for parallel tool execution
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 60
```

#### B. Add Service Mesh (Istio)
```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: test
  labels:
    istio-injection: enabled  # Enable automatic sidecar injection
---
apiVersion: networking.istio.io/v1beta1
kind: VirtualService
metadata:
  name: conversation-routing
  namespace: test
spec:
  hosts:
  - conversation-orchestrator
  http:
  - match:
    - headers:
        priority:
          exact: high
    route:
    - destination:
        host: conversation-orchestrator
        subset: v2
      weight: 100
  - route:
    - destination:
        host: conversation-orchestrator
        subset: v1
      weight: 90
    - destination:
        host: conversation-orchestrator
        subset: v2
      weight: 10  # Canary deployment
```

#### C. Add Circuit Breaker
```yaml
apiVersion: networking.istio.io/v1beta1
kind: DestinationRule
metadata:
  name: tool-execution-circuit-breaker
  namespace: test
spec:
  host: tool-execution-service
  trafficPolicy:
    connectionPool:
      tcp:
        maxConnections: 100
      http:
        http1MaxPendingRequests: 100
        http2MaxRequests: 100
        maxRequestsPerConnection: 2
    outlierDetection:
      consecutiveErrors: 5
      interval: 30s
      baseEjectionTime: 30s
      maxEjectionPercent: 50
      minHealthPercent: 30
```

### 5. Communication Patterns

#### A. Synchronous Communication (gRPC)
```yaml
apiVersion: v1
kind: Service
metadata:
  name: message-service-grpc
  namespace: test
spec:
  ports:
  - port: 9090
    targetPort: 9090
    protocol: TCP
    name: grpc
  selector:
    app: message-service
```

#### B. Asynchronous Communication (NATS Topics)
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: nats-topics
  namespace: test
data:
  topics.yaml: |
    topics:
      - conversation.created
      - message.sent
      - message.received
      - tool.execution.requested
      - tool.execution.completed
      - ui.update.requested
      - data.aggregation.requested
```

### 6. Database Separation

#### A. MongoDB for Conversations
```yaml
apiVersion: v1
kind: Service
metadata:
  name: mongodb-conversations
  namespace: test
spec:
  ports:
  - port: 27017
  selector:
    app: mongodb-conversations
---
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: mongodb-conversations
  namespace: test
spec:
  serviceName: mongodb-conversations
  replicas: 3  # MongoDB replica set
  selector:
    matchLabels:
      app: mongodb-conversations
  template:
    spec:
      containers:
      - name: mongodb
        image: mongo:7
        volumeMounts:
        - name: data
          mountPath: /data/db
  volumeClaimTemplates:
  - metadata:
      name: data
    spec:
      accessModes: ["ReadWriteOnce"]
      resources:
        requests:
          storage: 20Gi
```

#### B. PostgreSQL for Structured Data
```yaml
apiVersion: v1
kind: Service
metadata:
  name: postgresql-primary
  namespace: test
spec:
  ports:
  - port: 5432
  selector:
    app: postgresql
    role: primary
```

### 7. API Gateway Enhancement

#### Updated API Gateway Routes
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: api-gateway-config
  namespace: test
data:
  ocelot.json: |
    {
      "Routes": [
        {
          "UpstreamPathTemplate": "/api/conversations/{everything}",
          "DownstreamPathTemplate": "/api/{everything}",
          "DownstreamScheme": "http",
          "DownstreamHostAndPorts": [
            {
              "Host": "conversation-orchestrator",
              "Port": 8080
            }
          ],
          "LoadBalancerOptions": {
            "Type": "RoundRobin"
          }
        },
        {
          "UpstreamPathTemplate": "/api/messages/{everything}",
          "DownstreamPathTemplate": "/api/{everything}",
          "DownstreamScheme": "http",
          "DownstreamHostAndPorts": [
            {
              "Host": "message-service",
              "Port": 8081
            }
          ]
        },
        {
          "UpstreamPathTemplate": "/api/tools/{everything}",
          "DownstreamPathTemplate": "/api/{everything}",
          "DownstreamScheme": "http",
          "DownstreamHostAndPorts": [
            {
              "Host": "tool-execution-service",
              "Port": 8082
            }
          ],
          "RateLimitOptions": {
            "EnableRateLimiting": true,
            "Period": "1m",
            "Limit": 100,
            "PeriodTimespan": 60
          }
        },
        {
          "UpstreamPathTemplate": "/api/data/{everything}",
          "DownstreamPathTemplate": "/graphql",
          "DownstreamScheme": "http",
          "DownstreamHostAndPorts": [
            {
              "Host": "data-aggregation-service",
              "Port": 8084
            }
          ]
        }
      ],
      "GlobalConfiguration": {
        "ServiceDiscoveryProvider": {
          "Type": "Kubernetes",
          "Namespace": "test"
        }
      }
    }
```

### 8. Performance Optimizations

#### A. Redis Caching Strategy
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: redis-cache-config
  namespace: test
data:
  cache-config.yaml: |
    caches:
      - name: conversation-cache
        ttl: 300  # 5 minutes
        pattern: "conversation:*"
      - name: message-cache
        ttl: 600  # 10 minutes
        pattern: "messages:user:*"
      - name: tool-result-cache
        ttl: 3600  # 1 hour
        pattern: "tool:result:*"
      - name: ui-component-cache
        ttl: 86400  # 24 hours
        pattern: "ui:component:*"
```

#### B. Connection Pooling
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: database-config
  namespace: test
data:
  connection.yaml: |
    mongodb:
      minPoolSize: 10
      maxPoolSize: 100
      maxIdleTimeMS: 60000
    postgresql:
      minPoolSize: 5
      maxPoolSize: 50
      connectionTimeout: 30
    redis:
      minPoolSize: 5
      maxPoolSize: 30
      connectTimeout: 5000
```

### 9. Monitoring & Observability

#### A. Service-Specific Metrics
```yaml
apiVersion: v1
kind: Service
metadata:
  name: prometheus-metrics
  namespace: test
  labels:
    app: prometheus
spec:
  ports:
  - port: 9090
    name: metrics
  selector:
    app: prometheus
---
apiVersion: v1
kind: ConfigMap
metadata:
  name: prometheus-config
  namespace: test
data:
  prometheus.yml: |
    scrape_configs:
    - job_name: 'conversation-orchestrator'
      kubernetes_sd_configs:
      - role: pod
        namespaces:
          names:
          - test
      relabel_configs:
      - source_labels: [__meta_kubernetes_pod_label_app]
        regex: conversation-orchestrator
        action: keep
      metrics_path: /metrics
      scrape_interval: 15s
```

#### B. Distributed Tracing
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: jaeger
  namespace: test
spec:
  replicas: 1
  selector:
    matchLabels:
      app: jaeger
  template:
    spec:
      containers:
      - name: jaeger
        image: jaegertracing/all-in-one:latest
        ports:
        - containerPort: 16686  # UI
        - containerPort: 14268  # Collector
        env:
        - name: COLLECTOR_ZIPKIN_HOST_PORT
          value: ":9411"
```

### 10. Security Enhancements

#### A. Network Policies
```yaml
apiVersion: networking.k8s.io/v1
kind: NetworkPolicy
metadata:
  name: conversation-network-policy
  namespace: test
spec:
  podSelector:
    matchLabels:
      app: conversation-orchestrator
  policyTypes:
  - Ingress
  - Egress
  ingress:
  - from:
    - podSelector:
        matchLabels:
          app: api-gateway
    ports:
    - protocol: TCP
      port: 8080
  egress:
  - to:
    - podSelector:
        matchLabels:
          app: message-service
    - podSelector:
        matchLabels:
          app: tool-execution-service
    - podSelector:
        matchLabels:
          app: langchain-service
  - to:
    - podSelector:
        matchLabels:
          app: redis
    - podSelector:
        matchLabels:
          app: nats
```

#### B. Pod Security Policies
```yaml
apiVersion: policy/v1beta1
kind: PodSecurityPolicy
metadata:
  name: restricted
  namespace: test
spec:
  privileged: false
  allowPrivilegeEscalation: false
  requiredDropCapabilities:
    - ALL
  volumes:
    - 'configMap'
    - 'emptyDir'
    - 'projected'
    - 'secret'
    - 'persistentVolumeClaim'
  hostNetwork: false
  hostIPC: false
  hostPID: false
  runAsUser:
    rule: 'MustRunAsNonRoot'
  seLinux:
    rule: 'RunAsAny'
  supplementalGroups:
    rule: 'RunAsAny'
  fsGroup:
    rule: 'RunAsAny'
  readOnlyRootFilesystem: true
```

## Implementation Priority

### Phase 1 (Week 1-2): Core Service Separation
1. Split ConversationService into ConversationOrchestrator and MessageService
2. Deploy ToolExecutionService
3. Update API Gateway routes
4. Test service communication

### Phase 2 (Week 3-4): Performance & Scaling
1. Implement HorizontalPodAutoscalers
2. Configure Redis caching layers
3. Add connection pooling
4. Deploy DataAggregationService

### Phase 3 (Week 5-6): Advanced Features
1. Deploy UICompositionService
2. Implement service mesh (Istio)
3. Add circuit breakers
4. Configure distributed tracing

### Phase 4 (Week 7-8): Production Hardening
1. Implement network policies
2. Configure pod security policies
3. Add comprehensive monitoring
4. Performance testing and tuning

## Expected Benefits

### Performance Improvements
- **50% reduction** in response latency through service specialization
- **3x increase** in concurrent request handling
- **70% better** resource utilization through targeted scaling

### Reliability Improvements
- **99.9% availability** through redundancy and circuit breakers
- **80% reduction** in cascade failures
- **60% faster** recovery from failures

### Development Benefits
- **Clearer service boundaries** improve team velocity
- **Independent deployments** reduce risk
- **Better testability** through focused services

### Operational Benefits
- **Granular scaling** reduces costs
- **Better observability** simplifies debugging
- **Service-level monitoring** improves incident response

## Migration Strategy

### Step 1: Parallel Deployment
Deploy new services alongside existing ConversationService

### Step 2: Gradual Traffic Shift
Use Istio/Traefik to gradually route traffic to new services

### Step 3: Monitor & Adjust
Watch metrics, adjust resources, fix issues

### Step 4: Decommission Old Service
Remove original ConversationService once stable

## Success Metrics

- Response time P95 < 200ms
- Service availability > 99.9%
- Pod CPU utilization 40-70%
- Memory utilization 50-80%
- Zero message loss
- < 1% error rate
