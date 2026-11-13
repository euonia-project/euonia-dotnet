# Euonia.Tenancy 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Tenancy |
| 公共类型 | 50 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Tenancy

### Nerosoft.Euonia.Tenancy.CachedTenantStore

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantStore

- new Nerosoft.Euonia.Tenancy.CachedTenantStore(Nerosoft.Euonia.Tenancy.ITenantStore inner, IOptionsMonitor<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> options)
- void Clear()
- ValueTask<Nerosoft.Euonia.Tenancy.ITenantInfo> FindByIdAsync(System.String tenantId, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Tenancy.ITenantInfo> FindByIdentifierAsync(System.String identifier, System.Threading.CancellationToken cancellationToken = default)
- void Invalidate(System.String tenantId)
- ValueTask<System.Boolean> IsActiveAsync(System.String tenantId, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<IReadOnlyList<Nerosoft.Euonia.Tenancy.ITenantInfo>> ListAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Tenancy.ConfigurationTenantStore

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantStore

- new Nerosoft.Euonia.Tenancy.ConfigurationTenantStore(IOptionsMonitor<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> options)
- ValueTask<Nerosoft.Euonia.Tenancy.ITenantInfo> FindByIdAsync(System.String tenantId, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Tenancy.ITenantInfo> FindByIdentifierAsync(System.String identifier, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<System.Boolean> IsActiveAsync(System.String tenantId, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<IReadOnlyList<Nerosoft.Euonia.Tenancy.ITenantInfo>> ListAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Tenancy.CrossTenantOperationException

**类型**：class
基类 Nerosoft.Euonia.Tenancy.TenantException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Tenancy.CrossTenantOperationException(System.String message)

### Nerosoft.Euonia.Tenancy.DefaultTenantAccessValidator

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantAccessValidator

- new Nerosoft.Euonia.Tenancy.DefaultTenantAccessValidator(IOptionsMonitor<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> options)
- ValueTask<System.Boolean> CanAccessAsync(System.Security.Claims.ClaimsPrincipal user, Nerosoft.Euonia.Tenancy.ITenantInfo tenant, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Tenancy.DefaultTenantMessageTrustPolicy

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantMessageTrustPolicy

- new Nerosoft.Euonia.Tenancy.DefaultTenantMessageTrustPolicy(IOptionsMonitor<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> options)
- System.Boolean IsTrusted(System.String transport, System.String tenantId)

### Nerosoft.Euonia.Tenancy.InMemoryTenantStore

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantStore

- new Nerosoft.Euonia.Tenancy.InMemoryTenantStore(IEnumerable<Nerosoft.Euonia.Tenancy.ITenantInfo> tenants = null)
- void Add(Nerosoft.Euonia.Tenancy.ITenantInfo tenant)
- ValueTask<Nerosoft.Euonia.Tenancy.ITenantInfo> FindByIdAsync(System.String tenantId, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Tenancy.ITenantInfo> FindByIdentifierAsync(System.String identifier, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<System.Boolean> IsActiveAsync(System.String tenantId, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<IReadOnlyList<Nerosoft.Euonia.Tenancy.ITenantInfo>> ListAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Boolean Remove(System.String tenantId)

### Nerosoft.Euonia.Tenancy.ITenantAccessor

**类型**：interface

- Nerosoft.Euonia.Tenancy.TenantContext Current { get; }
- System.String TenantId { get; }

### Nerosoft.Euonia.Tenancy.ITenantAccessValidator

**类型**：interface

- ValueTask<System.Boolean> CanAccessAsync(System.Security.Claims.ClaimsPrincipal user, Nerosoft.Euonia.Tenancy.ITenantInfo tenant, System.Threading.CancellationToken cancellationToken = default)

### ITenantConnectionStringResolver<TContext>

**类型**：interface

- System.String Resolve(Nerosoft.Euonia.Tenancy.ITenantInfo tenant)

### Nerosoft.Euonia.Tenancy.ITenantFilterState

**类型**：interface

- System.String CurrentTenantId { get; }
- System.Boolean IgnoreTenantFilter { get; }

### Nerosoft.Euonia.Tenancy.ITenantInfo

**类型**：interface

- System.String ConnectionString { get; }
- System.String Id { get; }
- System.String Identifier { get; }
- Nerosoft.Euonia.Tenancy.TenantIsolation Isolation { get; }
- System.String Name { get; }
- System.String Schema { get; }
- Nerosoft.Euonia.Tenancy.TenantStatus Status { get; }

### Nerosoft.Euonia.Tenancy.ITenantMessageTrustPolicy

**类型**：interface

- System.Boolean IsTrusted(System.String transport, System.String tenantId)

### Nerosoft.Euonia.Tenancy.ITenantResolveContributor

**类型**：interface

- System.Int32 Order { get; }
- ValueTask<System.String> ResolveAsync(Nerosoft.Euonia.Tenancy.TenantResolveContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Tenancy.ITenantResolver

**类型**：interface

- ValueTask<Nerosoft.Euonia.Tenancy.TenantResolutionResult> ResolveAsync(Nerosoft.Euonia.Tenancy.TenantResolveContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Tenancy.ITenantScope

**类型**：interface
接口 System.IDisposable、System.IAsyncDisposable

- Nerosoft.Euonia.Tenancy.TenantContext Context { get; }

### Nerosoft.Euonia.Tenancy.ITenantScopeFactory

**类型**：interface

- Nerosoft.Euonia.Tenancy.ITenantScope Begin(Nerosoft.Euonia.Tenancy.ITenantInfo tenant)
- Nerosoft.Euonia.Tenancy.ITenantScope Begin(Nerosoft.Euonia.Tenancy.ITenantInfo tenant, System.String actorTenantId, System.String targetTenantId, System.String crossTenantReason)
- Nerosoft.Euonia.Tenancy.ITenantScope BeginCrossTenant(System.String reason, System.String targetTenantId = null)
- Nerosoft.Euonia.Tenancy.ITenantScope BeginIsolated(Nerosoft.Euonia.Tenancy.ITenantInfo tenant)

### Nerosoft.Euonia.Tenancy.ITenantStore

**类型**：interface

- ValueTask<Nerosoft.Euonia.Tenancy.ITenantInfo> FindByIdAsync(System.String tenantId, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<Nerosoft.Euonia.Tenancy.ITenantInfo> FindByIdentifierAsync(System.String identifier, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<System.Boolean> IsActiveAsync(System.String tenantId, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<IReadOnlyList<Nerosoft.Euonia.Tenancy.ITenantInfo>> ListAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Tenancy.MessageTenantResolveContributor

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantResolveContributor

- new Nerosoft.Euonia.Tenancy.MessageTenantResolveContributor()
- System.Int32 Order { get; }
- ValueTask<System.String> ResolveAsync(Nerosoft.Euonia.Tenancy.TenantResolveContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Tenancy.MultiTenancyOptions

**类型**：class

- new Nerosoft.Euonia.Tenancy.MultiTenancyOptions()
- System.Boolean AllowInactiveTenantWrites { get; set; }
- System.Boolean AllowSystemCrossTenant { get; set; }
- System.String ClaimType { get; set; }
- System.String ConnectionStringTemplate { get; set; }
- System.String[] CrossTenantRoles { get; set; }
- Func<System.String, System.Boolean> CrossTenantScopeAuthorizer { get; set; }
- Nerosoft.Euonia.Tenancy.TenantIsolation DefaultIsolation { get; set; }
- System.String DefaultTenantId { get; set; }
- System.Boolean EmitResponseHeader { get; set; }
- System.Boolean Enabled { get; set; }
- System.String[] ExcludedPaths { get; set; }
- System.Boolean ExposeTenantExistence { get; set; }
- System.String HeaderName { get; set; }
- System.String HostFormat { get; set; }
- Nerosoft.Euonia.Tenancy.TenantInactiveMessagePolicy InactiveTenantMessagePolicy { get; set; }
- System.Boolean RequireTenant { get; set; }
- System.String RouteKey { get; set; }
- System.TimeSpan StoreCacheDuration { get; set; }
- List<Nerosoft.Euonia.Tenancy.TenantInfo> Tenants { get; set; }
- System.Boolean TrustBrokerTenant { get; set; }
- System.String[] TrustedTransports { get; set; }
- System.Boolean TrustHeaderForAnonymous { get; set; }
- System.Boolean ValidateModelCacheKeyFactory { get; set; }
- static const System.String ConfigurationSection = Euonia:MultiTenancy

### Nerosoft.Euonia.Tenancy.TenancyServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddConfigurationTenantStore(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddInMemoryTenantStore(Microsoft.Extensions.DependencyInjection.IServiceCollection services, IEnumerable<Nerosoft.Euonia.Tenancy.ITenantInfo> tenants = null)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTenancy(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> configure = null)

### Nerosoft.Euonia.Tenancy.TenantAccessDeniedException

**类型**：class
基类 Nerosoft.Euonia.Tenancy.TenantException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Tenancy.TenantAccessDeniedException(System.String tenantId)

### Nerosoft.Euonia.Tenancy.TenantAccessor

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantAccessor

- new Nerosoft.Euonia.Tenancy.TenantAccessor()
- Nerosoft.Euonia.Tenancy.TenantContext Current { get; }
- System.String TenantId { get; }

### Nerosoft.Euonia.Tenancy.TenantCacheKey

**类型**：static class

- static System.String Build(System.String tenantId, System.String key)

### Nerosoft.Euonia.Tenancy.TenantContext

**类型**：class

- new Nerosoft.Euonia.Tenancy.TenantContext()
- System.String ActorTenantId { get; set; }
- System.String CrossTenantReason { get; set; }
- System.Boolean IsCrossTenant { get; set; }
- System.String TargetTenantId { get; set; }
- Nerosoft.Euonia.Tenancy.ITenantInfo Tenant { get; set; }
- System.String TenantId { get; set; }

### Nerosoft.Euonia.Tenancy.TenantContextKeys

**类型**：static class

- static const System.String Candidates = tenant.candidates
- static const System.String Failure = tenant.failure
- static const System.String Info = tenant.info

### Nerosoft.Euonia.Tenancy.TenantEntryContext

**类型**：struct

- new Nerosoft.Euonia.Tenancy.TenantEntryContext(Nerosoft.Euonia.Tenancy.TenantEntryOperation operation, System.String entityTenantId, System.String originalTenantId, System.String scopeTenantId, System.String targetTenantId, Nerosoft.Euonia.Tenancy.TenantStatus scopeTenantStatus, System.Boolean isCrossTenant, System.Boolean allowInactiveWrites)
- System.Boolean AllowInactiveWrites { get; }
- System.String EntityTenantId { get; }
- System.Boolean IsCrossTenant { get; }
- Nerosoft.Euonia.Tenancy.TenantEntryOperation Operation { get; }
- System.String OriginalTenantId { get; }
- System.String ScopeTenantId { get; }
- Nerosoft.Euonia.Tenancy.TenantStatus ScopeTenantStatus { get; }
- System.String TargetTenantId { get; }

### Nerosoft.Euonia.Tenancy.TenantEntryGuard

**类型**：static class

- static System.String Validate(in Nerosoft.Euonia.Tenancy.TenantEntryContext context)

### Nerosoft.Euonia.Tenancy.TenantEntryOperation

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| None |
| Added |
| Modified |
| Deleted |


### Nerosoft.Euonia.Tenancy.TenantException

**类型**：class
基类 System.BusinessException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Tenancy.TenantException(System.String code, System.String message)
- new Nerosoft.Euonia.Tenancy.TenantException(System.String code, System.String message, System.Exception innerException)

### Nerosoft.Euonia.Tenancy.TenantFailureCodes

**类型**：static class

- static const System.String Archived = TenantArchived
- static const System.String Conflict = TenantConflict
- static const System.String CrossTenant = CrossTenantOperation
- static const System.String Forbidden = TenantForbidden
- static const System.String Invalid = TenantInvalid
- static const System.String NotFound = TenantNotFound
- static const System.String Required = TenantRequired
- static const System.String StoreUnavailable = TenantStoreUnavailable
- static const System.String Suspended = TenantSuspended

### Nerosoft.Euonia.Tenancy.TenantFilterExpression

**类型**：static class

- static System.Linq.Expressions.LambdaExpression Combine(System.Linq.Expressions.LambdaExpression existing, System.Linq.Expressions.LambdaExpression tenant)

### Nerosoft.Euonia.Tenancy.TenantFilterNames

**类型**：static class

- static const System.String Tenant = Tenant
- static const System.String Tombstone = Tombstone

### Nerosoft.Euonia.Tenancy.TenantIdValidator

**类型**：static class

- static const System.Int32 MaxLength = 64
- static System.Boolean IsValid(System.String tenantId)
- static System.Boolean IsValidIdentifier(System.String identifier)
- static void Validate(System.String tenantId)

### Nerosoft.Euonia.Tenancy.TenantInactiveMessagePolicy

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| DeadLetter |
| Skip |
| Process |


### Nerosoft.Euonia.Tenancy.TenantInfo

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantInfo

- new Nerosoft.Euonia.Tenancy.TenantInfo()
- System.String ConnectionString { get; set; }
- System.String Id { get; set; }
- System.String Identifier { get; set; }
- Nerosoft.Euonia.Tenancy.TenantIsolation Isolation { get; set; }
- System.String Name { get; set; }
- System.String Schema { get; set; }
- Nerosoft.Euonia.Tenancy.TenantStatus Status { get; set; }
- System.String ToString()

### Nerosoft.Euonia.Tenancy.TenantInvalidException

**类型**：class
基类 Nerosoft.Euonia.Tenancy.TenantException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Tenancy.TenantInvalidException(System.String tenantId)

### Nerosoft.Euonia.Tenancy.TenantIsolation

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| SharedTable |
| SharedSchema |
| DatabasePerTenant |


### Nerosoft.Euonia.Tenancy.TenantKeyTransformer

**类型**：class
### Nerosoft.Euonia.Tenancy.TenantMessageTrustPolicy

**类型**：static class

- static System.Boolean IsTrusted(Nerosoft.Euonia.Tenancy.MultiTenancyOptions options, System.String transport, System.String tenantId)

### Nerosoft.Euonia.Tenancy.TenantNotFoundException

**类型**：class
基类 Nerosoft.Euonia.Tenancy.TenantException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Tenancy.TenantNotFoundException(System.String tenantId)

### Nerosoft.Euonia.Tenancy.TenantRequiredException

**类型**：class
基类 Nerosoft.Euonia.Tenancy.TenantException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Tenancy.TenantRequiredException()

### Nerosoft.Euonia.Tenancy.TenantResolutionResult

**类型**：class

- new Nerosoft.Euonia.Tenancy.TenantResolutionResult()
- IReadOnlyList<System.String> Candidates { get; set; }
- System.String FailureCode { get; set; }
- System.String Source { get; set; }
- System.Boolean Succeeded { get; }
- Nerosoft.Euonia.Tenancy.ITenantInfo Tenant { get; set; }

### Nerosoft.Euonia.Tenancy.TenantResolveContext

**类型**：class

- new Nerosoft.Euonia.Tenancy.TenantResolveContext()
- IReadOnlyDictionary<System.String, System.String> Headers { get; set; }
- System.String Host { get; set; }
- System.Boolean IsTrustedTransport { get; set; }
- IDictionary<System.String, System.Object> Items { get; }
- System.String MessageId { get; set; }
- System.String MessageTenantId { get; set; }
- System.String Path { get; set; }
- System.String RemoteIp { get; set; }
- System.String RouteTenant { get; set; }
- System.IServiceProvider Services { get; set; }
- System.String Transport { get; set; }
- System.Security.Claims.ClaimsPrincipal User { get; set; }

### Nerosoft.Euonia.Tenancy.TenantResolver

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantResolver

- new Nerosoft.Euonia.Tenancy.TenantResolver(IEnumerable<Nerosoft.Euonia.Tenancy.ITenantResolveContributor> contributors, Nerosoft.Euonia.Tenancy.ITenantStore store, IOptionsMonitor<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> options)
- ValueTask<Nerosoft.Euonia.Tenancy.TenantResolutionResult> ResolveAsync(Nerosoft.Euonia.Tenancy.TenantResolveContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Tenancy.TenantScopeConflictException

**类型**：class
基类 System.InvalidOperationException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Tenancy.TenantScopeConflictException(System.String currentTenantId, System.String targetTenantId)
- System.String CurrentTenantId { get; }
- System.String TargetTenantId { get; }

### Nerosoft.Euonia.Tenancy.TenantScopeFactory

**类型**：class
接口 Nerosoft.Euonia.Tenancy.ITenantScopeFactory

- new Nerosoft.Euonia.Tenancy.TenantScopeFactory()
- new Nerosoft.Euonia.Tenancy.TenantScopeFactory(IOptionsMonitor<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> options)
- new Nerosoft.Euonia.Tenancy.TenantScopeFactory(Nerosoft.Euonia.Tenancy.MultiTenancyOptions options)
- Nerosoft.Euonia.Tenancy.ITenantScope Begin(Nerosoft.Euonia.Tenancy.ITenantInfo tenant)
- Nerosoft.Euonia.Tenancy.ITenantScope Begin(Nerosoft.Euonia.Tenancy.ITenantInfo tenant, System.String actorTenantId, System.String targetTenantId, System.String crossTenantReason)
- Nerosoft.Euonia.Tenancy.ITenantScope BeginCrossTenant(System.String reason, System.String targetTenantId = null)
- Nerosoft.Euonia.Tenancy.ITenantScope BeginIsolated(Nerosoft.Euonia.Tenancy.ITenantInfo tenant)

### Nerosoft.Euonia.Tenancy.TenantStartupValidator

**类型**：class
接口 Microsoft.Extensions.Hosting.IHostedService

- new Nerosoft.Euonia.Tenancy.TenantStartupValidator(System.IServiceProvider services, IOptions<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> options)
- System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Tenancy.TenantStatus

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Active |
| Suspended |
| Archived |


### Nerosoft.Euonia.Tenancy.TenantStoreUnavailableException

**类型**：class
基类 Nerosoft.Euonia.Tenancy.TenantException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Tenancy.TenantStoreUnavailableException(System.Exception innerException)

### Nerosoft.Euonia.Tenancy.TenantSuspendedException

**类型**：class
基类 Nerosoft.Euonia.Tenancy.TenantException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Tenancy.TenantSuspendedException(System.String tenantId, Nerosoft.Euonia.Tenancy.TenantStatus status)
