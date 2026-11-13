# Euonia.Uow 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Uow |
| 公共类型 | 18 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddUnitOfWork(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

## 命名空间 Nerosoft.Euonia.Uow

### Nerosoft.Euonia.Uow.ITransactionalContext

**类型**：interface

_无公共成员。_

### Nerosoft.Euonia.Uow.IUnitOfWork

**类型**：interface
接口 System.IDisposable

- IReadOnlyDictionary<System.String, Nerosoft.Euonia.Uow.IUnitOfWorkContext> Contexts { get; }
- System.Guid Id { get; }
- System.Boolean IsCompleted { get; }
- System.Boolean IsDisposed { get; }
- System.Boolean IsReserved { get; }
- Dictionary<System.String, System.Object> Items { get; }
- Nerosoft.Euonia.Uow.IUnitOfWorkOptions Options { get; }
- Nerosoft.Euonia.Uow.IUnitOfWork Outer { get; }
- System.String ReservationName { get; }
- System.IServiceProvider ServiceProvider { get; }
- event EventHandler<Nerosoft.Euonia.Uow.UnitOfWorkEventArgs> Completed
- event EventHandler<System.DisposedEventArgs> Disposed
- event EventHandler<Nerosoft.Euonia.Uow.UnitOfWorkFailedEventArgs> Failed
- void AddContext(System.String key, Nerosoft.Euonia.Uow.IUnitOfWorkContext context)
- System.Threading.Tasks.Task CompleteAsync(System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Uow.IUnitOfWorkContext FindContext(System.String key)
- Nerosoft.Euonia.Uow.IUnitOfWorkContext GetOrAddContext(System.String key, Func<Nerosoft.Euonia.Uow.IUnitOfWorkContext> factory)
- TService GetService<TService>()
- System.Object GetService(System.Type serviceType)
- IEnumerable<TService> GetServices<TService>()
- IEnumerable<System.Object> GetServices(System.Type serviceType)
- void Initialize(Nerosoft.Euonia.Uow.UnitOfWorkOptions options)
- void OnCompleted(Func<System.Threading.Tasks.Task> handler)
- void Reserve(System.String reservationName)
- System.Threading.Tasks.Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
- void SetOuter(Nerosoft.Euonia.Uow.IUnitOfWork outer)

### Nerosoft.Euonia.Uow.IUnitOfWorkAccessor

**类型**：interface

- Nerosoft.Euonia.Uow.IUnitOfWork UnitOfWork { get; }
- Nerosoft.Euonia.Uow.IUnitOfWork GetCurrentUnitOfWork()
- void SetUnitOfWork(Nerosoft.Euonia.Uow.IUnitOfWork unitOfWork)

### Nerosoft.Euonia.Uow.IUnitOfWorkContext

**类型**：interface
接口 System.IDisposable

- System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Uow.IUnitOfWorkEnabled

**类型**：interface

_无公共成员。_

### Nerosoft.Euonia.Uow.IUnitOfWorkManager

**类型**：interface

- Nerosoft.Euonia.Uow.IUnitOfWork Current { get; }
- Nerosoft.Euonia.Uow.IUnitOfWork Begin(Nerosoft.Euonia.Uow.UnitOfWorkOptions options, System.Boolean requiresNew = false)
- Nerosoft.Euonia.Uow.IUnitOfWork Begin(System.Boolean isTransactional = false, System.Boolean requiresNew = false)

### Nerosoft.Euonia.Uow.IUnitOfWorkOptions

**类型**：interface

- Nullable<System.Data.IsolationLevel> IsolationLevel { get; }
- System.Boolean IsTransactional { get; }
- Nullable<System.TimeSpan> Timeout { get; }

### Nerosoft.Euonia.Uow.UnitOfWork

**类型**：class
基类 Nerosoft.Euonia.Uow.UnitOfWorkBase；接口 System.IDisposable、Nerosoft.Euonia.Uow.IUnitOfWork

- new Nerosoft.Euonia.Uow.UnitOfWork(System.IServiceProvider provider, IOptionsMonitor<Nerosoft.Euonia.Uow.UnitOfWorkOptions> options)
- IReadOnlyDictionary<System.String, Nerosoft.Euonia.Uow.IUnitOfWorkContext> Contexts { get; }
- System.Guid Id { get; }
- System.Boolean IsCompleted { get; set; }
- System.Boolean IsDisposed { get; set; }
- System.Boolean IsReserved { get; set; }
- Dictionary<System.String, System.Object> Items { get; }
- Nerosoft.Euonia.Uow.IUnitOfWorkOptions Options { get; set; }
- Nerosoft.Euonia.Uow.IUnitOfWork Outer { get; set; }
- System.String ReservationName { get; set; }
- System.IServiceProvider ServiceProvider { get; }
- event EventHandler<Nerosoft.Euonia.Uow.UnitOfWorkEventArgs> Completed
- event EventHandler<Nerosoft.Euonia.Uow.UnitOfWorkFailedEventArgs> Failed
- void AddContext(System.String key, Nerosoft.Euonia.Uow.IUnitOfWorkContext context)
- System.Threading.Tasks.Task CompleteAsync(System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Uow.IUnitOfWorkContext FindContext(System.String key)
- Nerosoft.Euonia.Uow.IUnitOfWorkContext GetOrAddContext(System.String key, Func<Nerosoft.Euonia.Uow.IUnitOfWorkContext> factory)
- void Initialize(Nerosoft.Euonia.Uow.UnitOfWorkOptions options)
- void OnCompleted(Func<System.Threading.Tasks.Task> handler)
- void Reserve(System.String reservationName)
- System.Threading.Tasks.Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
- void SetOuter(Nerosoft.Euonia.Uow.IUnitOfWork outer)

### Nerosoft.Euonia.Uow.UnitOfWorkAccessor

**类型**：class
接口 Nerosoft.Euonia.Uow.IUnitOfWorkAccessor、System.ISingletonDependency

- new Nerosoft.Euonia.Uow.UnitOfWorkAccessor()
- Nerosoft.Euonia.Uow.IUnitOfWork UnitOfWork { get; }
- Nerosoft.Euonia.Uow.IUnitOfWork GetCurrentUnitOfWork()
- void SetUnitOfWork(Nerosoft.Euonia.Uow.IUnitOfWork unitOfWork)

### Nerosoft.Euonia.Uow.UnitOfWorkAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Uow.UnitOfWorkAttribute()
- new Nerosoft.Euonia.Uow.UnitOfWorkAttribute(Nullable<System.Boolean> isTransactional)
- new Nerosoft.Euonia.Uow.UnitOfWorkAttribute(Nullable<System.Boolean> isTransactional, Nullable<System.Data.IsolationLevel> isolationLevel)
- new Nerosoft.Euonia.Uow.UnitOfWorkAttribute(Nullable<System.Boolean> isTransactional, Nullable<System.Data.IsolationLevel> isolationLevel, Nullable<System.TimeSpan> timeout)
- System.Boolean IsDisabled { get; set; }
- Nullable<System.Data.IsolationLevel> IsolationLevel { get; set; }
- Nullable<System.Boolean> IsTransactional { get; set; }
- Nullable<System.TimeSpan> Timeout { get; set; }

### Nerosoft.Euonia.Uow.UnitOfWorkBase

**类型**：class
基类 System.DisposableObject；接口 System.IDisposable

- System.IServiceProvider ServiceProvider { get; }
- TService GetService<TService>()
- System.Object GetService(System.Type serviceType)
- IEnumerable<TService> GetServices<TService>()
- IEnumerable<System.Object> GetServices(System.Type serviceType)

### Nerosoft.Euonia.Uow.UnitOfWorkEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Uow.UnitOfWorkEventArgs(Nerosoft.Euonia.Uow.IUnitOfWork unitOfWork)
- Nerosoft.Euonia.Uow.IUnitOfWork UnitOfWork { get; }

### Nerosoft.Euonia.Uow.UnitOfWorkFailedEventArgs

**类型**：class
基类 Nerosoft.Euonia.Uow.UnitOfWorkEventArgs

- new Nerosoft.Euonia.Uow.UnitOfWorkFailedEventArgs(Nerosoft.Euonia.Uow.IUnitOfWork unitOfWork, System.Exception exception, System.Boolean isRollback)
- System.Exception Exception { get; }
- System.Boolean IsRollback { get; }

### Nerosoft.Euonia.Uow.UnitOfWorkInterceptor

**类型**：class
接口 Castle.DynamicProxy.IInterceptor

- new Nerosoft.Euonia.Uow.UnitOfWorkInterceptor(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory factory)
- void Intercept(Castle.DynamicProxy.IInvocation invocation)

### Nerosoft.Euonia.Uow.UnitOfWorkManager

**类型**：class
接口 Nerosoft.Euonia.Uow.IUnitOfWorkManager

- new Nerosoft.Euonia.Uow.UnitOfWorkManager(Microsoft.Extensions.DependencyInjection.IServiceScopeFactory factory, Nerosoft.Euonia.Uow.IUnitOfWorkAccessor accessor)
- Nerosoft.Euonia.Uow.IUnitOfWork Current { get; }
- Nerosoft.Euonia.Uow.IUnitOfWork Begin(Nerosoft.Euonia.Uow.UnitOfWorkOptions options, System.Boolean requiresNew = false)
- Nerosoft.Euonia.Uow.IUnitOfWork Begin(System.Boolean isTransactional = false, System.Boolean requiresNew = false)

### Nerosoft.Euonia.Uow.UnitOfWorkModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Uow.UnitOfWorkModule()
- void AheadConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Uow.UnitOfWorkOptions

**类型**：class
接口 Nerosoft.Euonia.Uow.IUnitOfWorkOptions

- new Nerosoft.Euonia.Uow.UnitOfWorkOptions()
- new Nerosoft.Euonia.Uow.UnitOfWorkOptions(System.Boolean isTransactional = false, Nullable<System.Data.IsolationLevel> isolationLevel = default, Nullable<System.TimeSpan> timeout = default)
- Nullable<System.Data.IsolationLevel> IsolationLevel { get; set; }
- System.Boolean IsTransactional { get; set; }
- Nullable<System.TimeSpan> Timeout { get; set; }
- Nerosoft.Euonia.Uow.UnitOfWorkOptions Normalize(Nerosoft.Euonia.Uow.UnitOfWorkOptions options)
