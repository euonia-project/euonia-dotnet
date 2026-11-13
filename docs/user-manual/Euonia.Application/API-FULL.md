# Euonia.Application 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Application |
| 公共类型 | 53 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static void AddApplicationService(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Reflection.Assembly assembly, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime)
- static void AddApplicationService(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Reflection.Assembly assembly, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime, Func<System.Type, System.Boolean> filter)
- static void AddPipelineBehaviors(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.Reflection.Assembly assembly)
- static void Register<TService>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

## 命名空间 Nerosoft.Euonia.Application

### Nerosoft.Euonia.Application.ApplicationModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Application.ApplicationModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Application.AuditingInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor、Nerosoft.Euonia.Application.IOrderedInterceptor

- new Nerosoft.Euonia.Application.AuditingInterceptor(Nerosoft.Euonia.Auditing.IAuditingRecorder recorder, Nerosoft.Euonia.Auditing.AuditingOptions options = null)
- System.Int32 Order { get; }
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.AuthorizationInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.AuthorizationInterceptor(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.BaseApplicationService

**类型**：class
接口 Nerosoft.Euonia.Application.IApplicationService、System.IHasLazyServiceProvider

- System.ILazyServiceProvider LazyServiceProvider { get; set; }

### Nerosoft.Euonia.Application.CacheAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Application.CacheAttribute()
- System.Double AbsoluteExpirationSeconds { get; set; }
- System.String[] Groups { get; set; }
- System.Boolean IsUtc { get; set; }
- System.String Key { get; set; }
- System.Int32 TimeoutSeconds { get; set; }

### Nerosoft.Euonia.Application.CacheEvictAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Application.CacheEvictAttribute(System.String[] groups)
- System.String[] Groups { get; }
- Nerosoft.Euonia.Application.CacheEvictionMode Mode { get; set; }

### Nerosoft.Euonia.Application.CacheEvictionInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.CacheEvictionInterceptor(System.IServiceProvider serviceProvider)
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.CacheEvictionMode

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| After |
| Before |


### Nerosoft.Euonia.Application.CacheGroupManager

**类型**：class
接口 Nerosoft.Euonia.Application.ICacheGroupManager

- new Nerosoft.Euonia.Application.CacheGroupManager(System.IServiceProvider serviceProvider)
- System.Int32 Evict(IEnumerable<System.String> groups)
- IReadOnlyCollection<System.String> GetKeys(System.String group)
- void Register(System.String key, IEnumerable<System.String> groups)
- void Remove(System.String key)

### Nerosoft.Euonia.Application.CacheInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.CacheInterceptor(System.IServiceProvider serviceProvider)
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.CircuitBreakerAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Application.CircuitBreakerAttribute()
- System.Int32 MaxFailures { get; set; }
- System.Double ResetTimeoutSeconds { get; set; }
- System.Int32 SuccessThreshold { get; set; }

### Nerosoft.Euonia.Application.CircuitBreakerInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.CircuitBreakerInterceptor()
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.CircuitBreakerOpenException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Application.CircuitBreakerOpenException()
- new Nerosoft.Euonia.Application.CircuitBreakerOpenException(System.String message)
- new Nerosoft.Euonia.Application.CircuitBreakerOpenException(System.String message, System.Exception innerException)

### CorrelationIdBehavior<TMessage, TResponse>

**类型**：class
接口 IPipelineBehavior<TMessage, TResponse>

- new CorrelationIdBehavior<TMessage, TResponse>(Nerosoft.Euonia.Modularity.IRequestContextAccessor contextAccessor)
- Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)

### DefaultUseCasePresenter<TOutput>

**类型**：class
基类 System.DisposableObject；接口 System.IDisposable、IUseCasePresenter<TOutput>、Nerosoft.Euonia.Application.IUseCaseOutputFailure、IUseCaseOutputSuccess<TOutput>

- new DefaultUseCasePresenter<TOutput>()
- TOutput Output { get; set; }
- event System.EventHandler OnCanceled
- event EventHandler<System.Exception> OnFailed
- event EventHandler<TOutput> OnSucceed
- void Error(System.Exception exception)
- void Ok(TOutput output)

### Nerosoft.Euonia.Application.DistributedLockAttribute

**类型**：class
基类 Nerosoft.Euonia.Application.LockAttribute

- new Nerosoft.Euonia.Application.DistributedLockAttribute(System.String token)

### Nerosoft.Euonia.Application.EmptyUseCaseInput

**类型**：class
接口 Nerosoft.Euonia.Application.IUseCaseInput、IEquatable<Nerosoft.Euonia.Application.EmptyUseCaseInput>

- new Nerosoft.Euonia.Application.EmptyUseCaseInput()

### Nerosoft.Euonia.Application.EmptyUseCaseOutput

**类型**：class
接口 Nerosoft.Euonia.Application.IUseCaseOutput、IEquatable<Nerosoft.Euonia.Application.EmptyUseCaseOutput>

- new Nerosoft.Euonia.Application.EmptyUseCaseOutput()
- static Nerosoft.Euonia.Application.EmptyUseCaseOutput Instance { get; }

### Nerosoft.Euonia.Application.IApplicationService

**类型**：interface
接口 System.IHasLazyServiceProvider

_无公共成员。_

### Nerosoft.Euonia.Application.ICacheGroupManager

**类型**：interface

- System.Int32 Evict(IEnumerable<System.String> groups)
- IReadOnlyCollection<System.String> GetKeys(System.String group)
- void Register(System.String key, IEnumerable<System.String> groups)
- void Remove(System.String key)

### Nerosoft.Euonia.Application.IdempotentAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Application.IdempotentAttribute()
- System.String Key { get; set; }
- System.Double TimeoutSeconds { get; set; }
- System.Boolean UseRequestKey { get; set; }

### Nerosoft.Euonia.Application.IdempotentInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.IdempotentInterceptor(System.IServiceProvider serviceProvider, Nerosoft.Euonia.Modularity.IRequestContextAccessor requestContextAccessor = null)
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### INonInputUseCase<TOutput>

**类型**：interface
接口 IUseCase<Nerosoft.Euonia.Application.EmptyUseCaseInput, TOutput>、Nerosoft.Euonia.Application.IUseCase

- Task<TOutput> ExecuteAsync(System.Threading.CancellationToken cancellationToken = default)

### INonOutputUseCase<TInput>

**类型**：interface
接口 IUseCase<TInput, Nerosoft.Euonia.Application.EmptyUseCaseOutput>、Nerosoft.Euonia.Application.IUseCase

- System.Threading.Tasks.Task ExecuteAsync(TInput input, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Application.IOrderedInterceptor

**类型**：interface

- System.Int32 Order { get; }

### Nerosoft.Euonia.Application.IParameterlessUseCase

**类型**：interface
接口 IUseCase<Nerosoft.Euonia.Application.EmptyUseCaseInput, Nerosoft.Euonia.Application.EmptyUseCaseOutput>、Nerosoft.Euonia.Application.IUseCase

- System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Application.IServiceContext

**类型**：interface

- Microsoft.Extensions.DependencyInjection.ServiceLifetime ApplicationServiceLifetime { get; }
- Func<System.Type, System.Boolean> ApplicationServiceTypeFilter { get; }
- System.Reflection.Assembly Assembly { get; }
- System.Boolean AutoRegisterApplicationService { get; }
- System.Boolean AutoRegisterPipelineBehaviors { get; }
- void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

### Nerosoft.Euonia.Application.IUseCase

**类型**：interface

- Task<System.Object> ExecuteAsync(System.Object input, System.Threading.CancellationToken cancellationToken = default)

### IUseCase<TInput, TOutput>

**类型**：interface
接口 Nerosoft.Euonia.Application.IUseCase

- Task<TOutput> ExecuteAsync(TInput input, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Application.IUseCaseExecutor

**类型**：interface

- System.Threading.Tasks.Task ExecuteAsync<TInput, TOutput>(IUseCase<TInput, TOutput> useCase, TInput input, IUseCasePresenter<TOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TInput>(INonOutputUseCase<TInput> useCase, TInput input, IUseCasePresenter<Nerosoft.Euonia.Application.EmptyUseCaseOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TOutput>(INonInputUseCase<TOutput> useCase, IUseCasePresenter<TOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync(Nerosoft.Euonia.Application.IParameterlessUseCase useCase, IUseCasePresenter<Nerosoft.Euonia.Application.EmptyUseCaseOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TUseCase, TInput, TOutput>(TInput input, IUseCasePresenter<TOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TUseCase, TInput>(TInput input, IUseCasePresenter<Nerosoft.Euonia.Application.EmptyUseCaseOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TUseCase, TOutput>(IUseCasePresenter<TOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TUseCase>(IUseCasePresenter<Nerosoft.Euonia.Application.EmptyUseCaseOutput> presenter, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Application.IUseCaseInput

**类型**：interface

_无公共成员。_

### Nerosoft.Euonia.Application.IUseCaseOutput

**类型**：interface

_无公共成员。_

### Nerosoft.Euonia.Application.IUseCaseOutputFailure

**类型**：interface

- void Error(System.Exception exception)

### IUseCaseOutputSuccess<TOutput>

**类型**：interface

- void Ok(TOutput output)

### IUseCasePresenter<TOutput>

**类型**：interface
接口 Nerosoft.Euonia.Application.IUseCaseOutputFailure、IUseCaseOutputSuccess<TOutput>、System.IDisposable

- event System.EventHandler OnCanceled
- event EventHandler<System.Exception> OnFailed
- event EventHandler<TOutput> OnSucceed

### Nerosoft.Euonia.Application.LockAttribute

**类型**：class
基类 System.Attribute

- System.Int32 MaximumCount { get; }
- System.Int32 Timeout { get; set; }
- System.String Token { get; }

### Nerosoft.Euonia.Application.LockInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.LockInterceptor(System.IServiceProvider serviceProvider)
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.LoggingInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.LoggingInterceptor(Microsoft.Extensions.Logging.ILoggerFactory logger)
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.RetryAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Application.RetryAttribute()
- Nerosoft.Euonia.Application.RetryBackoffMode Backoff { get; set; }
- System.Int32 DelayMs { get; set; }
- System.Boolean Jitter { get; set; }
- System.Int32 MaxRetries { get; set; }
- System.Type[] RetryableExceptions { get; set; }

### Nerosoft.Euonia.Application.RetryBackoffMode

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Fixed |
| Linear |
| Exponential |


### Nerosoft.Euonia.Application.RetryInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.RetryInterceptor()
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.SemaphoreLockAttribute

**类型**：class
基类 Nerosoft.Euonia.Application.LockAttribute

- new Nerosoft.Euonia.Application.SemaphoreLockAttribute(System.String token, System.Int32 maximumCount = 1)

### Nerosoft.Euonia.Application.ServiceContextBase

**类型**：class
接口 Nerosoft.Euonia.Application.IServiceContext

- Microsoft.Extensions.DependencyInjection.ServiceLifetime ApplicationServiceLifetime { get; }
- Func<System.Type, System.Boolean> ApplicationServiceTypeFilter { get; }
- System.Reflection.Assembly Assembly { get; }
- System.Boolean AutoRegisterApplicationService { get; }
- System.Boolean AutoRegisterPipelineBehaviors { get; }
- void ConfigureServices(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

### Nerosoft.Euonia.Application.TimingAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Application.TimingAttribute()
- System.Double ThresholdMs { get; set; }
- System.Double WarningThresholdMs { get; set; }

### Nerosoft.Euonia.Application.TimingInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.TimingInterceptor(Microsoft.Extensions.Logging.ILoggerFactory loggerFactory)
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.TracingInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.TracingInterceptor(Microsoft.Extensions.Logging.ILoggerFactory logger)
- new Nerosoft.Euonia.Application.TracingInterceptor(Microsoft.Extensions.Logging.ILoggerFactory logger, Nerosoft.Euonia.Modularity.IRequestContextAccessor contextAccessor)
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Application.UseCaseExecutor

**类型**：class
接口 Nerosoft.Euonia.Application.IUseCaseExecutor

- new Nerosoft.Euonia.Application.UseCaseExecutor(System.IServiceProvider serviceProvider)
- System.Threading.Tasks.Task ExecuteAsync<TInput, TOutput>(IUseCase<TInput, TOutput> useCase, TInput input, IUseCasePresenter<TOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TInput>(INonOutputUseCase<TInput> useCase, TInput input, IUseCasePresenter<Nerosoft.Euonia.Application.EmptyUseCaseOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TOutput>(INonInputUseCase<TOutput> useCase, IUseCasePresenter<TOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync(Nerosoft.Euonia.Application.IParameterlessUseCase useCase, IUseCasePresenter<Nerosoft.Euonia.Application.EmptyUseCaseOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TUseCase, TInput, TOutput>(TInput input, IUseCasePresenter<TOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TUseCase, TInput>(TInput input, IUseCasePresenter<Nerosoft.Euonia.Application.EmptyUseCaseOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TUseCase, TOutput>(IUseCasePresenter<TOutput> presenter, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task ExecuteAsync<TUseCase>(IUseCasePresenter<Nerosoft.Euonia.Application.EmptyUseCaseOutput> presenter, System.Threading.CancellationToken cancellationToken = default)

### UserContextBehavior<TMessage, TResponse>

**类型**：class
接口 IPipelineBehavior<TMessage, TResponse>

- new UserContextBehavior<TMessage, TResponse>(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory scopeFactory)
- Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)

### Nerosoft.Euonia.Application.UserContextExtensions

**类型**：static class

- static System.String GetAuthorizationToken(Nerosoft.Euonia.Bus.MessageMetadata metadata)
### Nerosoft.Euonia.Application.UserContextMetadataKeys

**类型**：static class

- static const System.String Authorization = Authorization
- static const System.String UserCode = $nerosoft:user.code
- static const System.String UserId = $nerosoft:user.id
- static const System.String UserName = $nerosoft:user.name
- static const System.String UserTenant = $nerosoft:user.tenant

### ValidationBehavior<TMessage, TResponse>

**类型**：class
接口 IPipelineBehavior<TMessage, TResponse>

- new ValidationBehavior<TMessage, TResponse>(System.IServiceProvider provider)
- Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)

### Nerosoft.Euonia.Application.ValidationInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Application.ValidationInterceptor()
- void Intercept(Castle.DynamicProxy.IInvocation invocation)
