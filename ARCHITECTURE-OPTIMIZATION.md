# MIBO Architecture Optimization Plan

## Current State Analysis
- **20+ microservices** with many barely implemented
- **Multiple data stores** (PostgreSQL, MongoDB, Redis) without clear separation
- **Complex messaging** (NATS + SignalR + HTTP)
- **Over-engineered** for current scale

## Proposed Optimized Architecture

### Phase 1: Consolidation (Immediate)

#### Core Services (Keep These)
1. **API Gateway** - Single entry point with Ocelot
2. **Identity Service** - Authentication & authorization
3. **Conversation Service** - AI chat orchestration
4. **Finance Service** - Financial data management
5. **External Data Service** - Weather, calendar, third-party integrations

#### Infrastructure Services (Consolidate)
1. **Storage Service** - Unified data access layer
   - PostgreSQL provider (primary)
   - MongoDB provider (when needed)
   - Redis provider (caching)
2. **Observability Service** - Monitoring, metrics, logging
3. **Event Bus** - Single messaging solution (remove NATS, keep SignalR)

#### Remove/Merge These Services
- ❌ CalendarAgent → Merge into External Data Service
- ❌ CalendarDataService → Merge into External Data Service
- ❌ WeatherDataService → Merge into External Data Service
- ❌ MIBO.Metrics → Merge into Observability Service
- ❌ MIBO.Storage.Mongo → Merge into Storage Service
- ❌ MIBO.Storage.PostgreSQL → Merge into Storage Service
- ❌ MIBO.Storage.Blob → Merge into Storage Service
- ❌ MIBO.EventBus → Use SignalR for real-time, HTTP for async

### Phase 2: Architectural Patterns

#### 1. Implement BFF (Backend for Frontend)
```csharp
// New endpoint in API Gateway or ConversationService
[HttpGet("api/user/{userId}/context")]
public async Task<IActionResult> GetUserContext(int userId)
{
    // Parallel fetch from all services
    var financialTask = _financeService.GetSummary(userId);
    var conversationTask = _conversationService.GetRecent(userId);
    var calendarTask = _externalDataService.GetEvents(userId);

    await Task.WhenAll(financialTask, conversationTask, calendarTask);

    return Ok(new {
        financial = financialTask.Result,
        conversations = conversationTask.Result,
        calendar = calendarTask.Result
    });
}
```

#### 2. Implement CQRS for Conversation Service
```csharp
// Commands
public class SendMessageCommand : IRequest<MessageResponse>
{
    public int UserId { get; set; }
    public string Message { get; set; }
}

// Queries
public class GetConversationQuery : IRequest<ConversationResponse>
{
    public int ConversationId { get; set; }
}

// Handlers
public class SendMessageHandler : IRequestHandler<SendMessageCommand, MessageResponse>
{
    public async Task<MessageResponse> Handle(SendMessageCommand request)
    {
        // Process message, call AI, save to DB
    }
}
```

#### 3. Implement Distributed Caching
```csharp
public interface ICacheService
{
    Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
}

// Usage in services
public async Task<FinancialSummary> GetFinancialSummary(int userId)
{
    return await _cache.GetOrSetAsync(
        $"finance:summary:{userId}",
        async () => await CalculateFinancialSummary(userId),
        TimeSpan.FromMinutes(5)
    );
}
```

### Phase 3: Data Strategy

#### Primary Database (PostgreSQL)
- User accounts & identity
- Transaction records
- Budget data
- Calendar events
- System configuration

#### Document Store (MongoDB) - Add Later If Needed
- Conversation history (high volume, unstructured)
- AI interaction logs
- User preferences

#### Cache Layer (Redis)
- Session management
- Frequently accessed data
- Real-time state
- API response caching

### Phase 4: Communication Patterns

#### Synchronous (Request/Response)
- **HTTP/REST** - Standard API calls
- **gRPC** - Internal service communication (optional)

#### Asynchronous (Events/Messages)
- **SignalR** - Real-time updates to clients
- **Background Jobs** - Hangfire or native BackgroundService
- **Message Queue** - Simple in-memory queue initially, upgrade to RabbitMQ/Azure Service Bus later

### Phase 5: Deployment Optimization

#### Development Environment
```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: postgres:15
    ports: ["5432:5432"]

  redis:
    image: redis:7
    ports: ["6379:6379"]

  api-gateway:
    build: ./src/MIBO.ApiGateway
    ports: ["5000:80"]
    depends_on: [postgres, redis]

  identity-service:
    build: ./src/MIBO.IdentityService
    depends_on: [postgres]

  conversation-service:
    build: ./src/MIBO.ConversationService
    depends_on: [postgres, redis]

  finance-service:
    build: ./src/MIBO.FinanceDataService
    depends_on: [postgres]
```

#### Production Environment
- Use **Kubernetes** with namespace per environment
- Implement **Horizontal Pod Autoscaling**
- Use **Istio/Linkerd** for service mesh (later)

### Implementation Priority

#### Week 1-2: Consolidation
1. Merge redundant services
2. Remove unused dependencies
3. Standardize on PostgreSQL

#### Week 3-4: Core Patterns
1. Implement BFF pattern
2. Add distributed caching
3. Simplify messaging to SignalR only

#### Week 5-6: Optimization
1. Add response aggregation
2. Implement CQRS for conversations
3. Performance testing & tuning

#### Week 7-8: Production Ready
1. Complete CI/CD pipelines
2. Add comprehensive monitoring
3. Load testing & optimization

## Expected Benefits

### Performance
- **50% reduction** in inter-service calls via BFF
- **80% faster** response times with caching
- **30% less** infrastructure complexity

### Development
- **Faster development** with fewer services to maintain
- **Easier debugging** with simplified communication
- **Better testability** with clear boundaries

### Operations
- **Lower costs** with fewer services to deploy
- **Easier monitoring** with consolidated observability
- **Simpler scaling** with clear bottleneck identification

## Migration Checklist

- [ ] Backup all data and configurations
- [ ] Create migration scripts for data consolidation
- [ ] Update CI/CD pipelines for new structure
- [ ] Update API Gateway routes
- [ ] Test all integrations thoroughly
- [ ] Update documentation
- [ ] Train team on new architecture
- [ ] Plan rollback strategy

## Monitoring Success

### Key Metrics to Track
- Response time (P50, P95, P99)
- Service availability (99.9% target)
- Error rate (<1%)
- Infrastructure costs
- Development velocity

### Success Criteria
- ✅ Reduced service count from 20+ to 8-10
- ✅ Single database for initial deployment
- ✅ Unified messaging strategy
- ✅ Sub-200ms response time for common operations
- ✅ Simplified deployment process