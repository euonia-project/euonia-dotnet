# Euonia.Application

Euonia 应用层构建块：用例（Use Case）、应用服务基类、基于 Castle DynamicProxy 的拦截器（日志、授权、校验、追踪、缓存、锁、计时、幂等、重试、熔断），以及日志、校验、用户上下文等消息管道行为，用于把横切关注点从业务代码中剥离。

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Application`（`Register<TService>()` 扩展位于 `Microsoft.Extensions.DependencyInjection`）
- **NuGet 包**：`Euonia.Application`

## 安装

```bash
dotnet add package Euonia.Application
```

## 快速开始

应用入口是 `ApplicationModule` 与 `Register<TService>()`；没有独立的注册扩展方法。

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(ApplicationModule))]
public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Register<SalesServiceContext>();
    }
}

public sealed class SalesServiceContext : ServiceContextBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, OrderRepository>();
    }
}
```

编写应用服务并使用拦截器特性：

```csharp
using Nerosoft.Euonia.Application;

public sealed record OrderDto(long Id);

public sealed class OrderApplicationService : BaseApplicationService, IApplicationService
{
    [Cache(Key = "order:{0}", TimeoutSeconds = 60, Groups = new[] { "orders" })]
    public virtual Task<OrderDto?> GetAsync(long id, CancellationToken cancellationToken)
        => Task.FromResult<OrderDto?>(null);

    [Retry(MaxRetries = 3, DelayMs = 200, Backoff = RetryBackoffMode.Exponential)]
    [DistributedLock("order:{id}")]
    public virtual Task SubmitAsync(long id, CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

实现一个类型化用例：

```csharp
public sealed record CancelOrderInput(long OrderId);

public sealed record CancelOrderOutput(long OrderId, bool Canceled);

public sealed class CancelOrderUseCase : IUseCase<CancelOrderInput, CancelOrderOutput>
{
    public Task<CancelOrderOutput> ExecuteAsync(CancelOrderInput input, CancellationToken cancellationToken = default)
        => Task.FromResult(new CancelOrderOutput(input.OrderId, true));
}
```

## 主要功能

- **用例模型**：`IUseCase<TInput, TOutput>`、无输入/无输出变体与 `IUseCaseExecutor`，统一执行并分发到 presenter。
- **应用服务**：`BaseApplicationService` 通过 `ILazyServiceProvider` 懒加载总线、当前用户、请求上下文、日志与用例执行器。
- **自动注册**：`ServiceContextBase` + `services.Register<TContext>()` 按程序集扫描应用服务与管道行为，并为实现类型与接口生成共享目标实例的代理。
- **拦截器**：日志、授权、校验、追踪、锁、计时、缓存、缓存失效、幂等、重试与熔断。
- **管道行为**：`ValidationBehavior`、`UserContextBehavior`、`CorrelationIdBehavior`，可挂到消息总线的处理管道上。

## 核心类型

| 类型 | 作用 |
|------|------|
| `IServiceContext` / `ServiceContextBase` | 描述应用服务所在程序集、生命周期与自动注册开关 |
| `Register<TContext>()` | 注册服务上下文并自动扫描应用服务与管道行为 |
| `IApplicationService` / `BaseApplicationService` | 应用服务契约与基类（懒加载 `IBus`、`UserPrincipal`、日志等） |
| `IUseCase<TInput, TOutput>` | 类型化用例契约，另有 `INonInputUseCase<T>`、`INonOutputUseCase<T>`、`IParameterlessUseCase` |
| `IUseCaseExecutor` / `UseCaseExecutor` | 执行用例并把成功/失败结果分发给 presenter；容器重载每次创建新作用域 |
| `IUseCasePresenter<TOutput>` / `DefaultUseCasePresenter<TOutput>` | 结果呈现器及其默认实现 |
| `ApplicationModule` | 注册全部拦截器、管道行为、presenter 与用例执行器 |

**常用拦截器特性**

| 特性 | 作用 |
|------|------|
| `[Cache]` / `[CacheEvict]` | 缓存方法结果 / 按组失效缓存 |
| `[Retry]` | 按固定、线性或指数退避重试 |
| `[CircuitBreaker]` | 连续失败达到阈值后短路调用（进程本地状态） |
| `[DistributedLock]` / `[SemaphoreLock]` | 跨进程互斥 / 进程内信号量 |
| `[Idempotent]` | 幂等窗口内对相同指纹的重复调用只执行一次 |
| `[Timing]` / `[SensitiveData]` | 计时告警与参数级敏感数据脱敏（`TracingInterceptor` 全局记录调用链） |

## 使用示例

`Register<TContext>()` 只扫描 `IApplicationService` 实现与管道行为，**不会**自动注册用例；需要执行器从容器解析用例时显式注册：

```csharp
public override void ConfigureServices(ServiceConfigurationContext context)
{
    context.Services.AddScoped<CancelOrderUseCase>();
}
```

执行用例并把结果交给 presenter：

```csharp
using Nerosoft.Euonia.Application;

public sealed class CancelOrderEndpoint(IUseCaseExecutor executor)
{
    public async Task<CancelOrderOutput?> CancelAsync(long orderId)
    {
        var presenter = new DefaultUseCasePresenter<CancelOrderOutput>();

        await executor.ExecuteAsync<CancelOrderUseCase, CancelOrderInput, CancelOrderOutput>(
            new CancelOrderInput(orderId), presenter);

        return presenter.Output;
    }
}
```

`ExecuteAsync<TUseCase, TInput, TOutput>` 会为一次执行创建独立的 DI 作用域，用例依赖的 `DbContext`/仓储在用例完成后才释放；用例抛出的异常不会向外抛出，而是写入 presenter 的 `OnFailed`（取消写入 `OnCanceled`）。

## 选项（代码配置）

`Euonia.Application` 没有配置节或 `*Options` 类型；自动注册行为由 `ServiceContextBase` 的虚拟属性控制：

| 配置点 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `Assembly` | `Assembly` | 上下文类型所在程序集 | 自动扫描的目标程序集 |
| `AutoRegisterApplicationService` | `bool` | `true` | 是否扫描 `IApplicationService` 实现 |
| `AutoRegisterPipelineBehaviors` | `bool` | `true` | 是否扫描 `IPipelineBehavior<,>` 实现 |
| `ApplicationServiceLifetime` | `ServiceLifetime` | `Scoped` | 应用服务代理的生命周期 |
| `ApplicationServiceTypeFilter` | `Func<Type, bool>?` | `null` | 自动注册的类型筛选器 |
| `ConfigureServices(IServiceCollection)` | 方法 | 空实现 | 注册应用额外服务 |

拦截器行为由特性参数配置（如 `[Retry(MaxRetries = 3, Backoff = RetryBackoffMode.Exponential)]`），同样不读取 appsettings。

## 注意事项 / 限制

- 应用入口是 `ApplicationModule` + `Register<TService>()`；不存在单独的 `Add*` 入口。`Register<T>` 只扫描 `IApplicationService` 与管道行为，用例、仓储等需显式注册。
- 类代理要求目标类型有无参构造函数；缺少时按实现类型解析会回退为裸实例。被拦截方法还必须是 `virtual`，否则类代理不会拦截该方法；按接口解析仍会创建接口代理并执行拦截器。
- `[Cache]` 在容器中没有 `ICacheService` 时静默退化为直接执行；`[DistributedLock]`/`[SemaphoreLock]` 需要相应的锁实现，`[Idempotent]` 依赖缓存与请求上下文。缺少基础设施时的行为由各拦截器决定，部署前应确认依赖已注册。
- `[Cache]` 只处理有返回值的同步方法和 `Task<T>`/`ValueTask<T>`；`void`、非泛型 `Task`/`ValueTask` 不缓存，`null` 结果也不写入缓存。
- `[Retry]` 默认对所有异常重试；需要只重试特定异常时设置 `RetryableExceptions`。`[CircuitBreaker]` 的状态保存在进程本地静态表中，按 `{service}.{method}` 隔离，不跨进程共享。
- `ApplicationModule` 用 `TryAddSingleton<IAuditingRecorder>(NullAuditingRecorder.Instance)` 兜底；需要真实审计时应在模块之外调用 `AddAuditing()` 并注册审计存储。
- 拦截器按 `IOrderedInterceptor.Order` 稳定排序（数字小的在外层），其余按注册顺序；不要在拦截器内部再次解析同一应用服务，以免代理递归。

## 常见问题与故障排查

### 拦截器完全不生效
**原因**：拦截器只对容器解析出的 Castle 代理生效；直接 `new` 应用服务、方法不是 `virtual`，或实现类没有无参构造函数时，类代理路径不会拦截该方法（无参构造缺失时直接回退为裸实例）。
**解决**：从 DI 解析应用服务；把被拦截方法声明为 `virtual`；保留公共无参构造函数。按接口解析始终会创建接口代理，可作为类代理不可用时的替代路径。

### 应用服务没有被自动注册
**原因**：`Register<TContext>()` 只扫描 `IApplicationService` 实现与 `IPipelineBehavior<,>` 实现；类型未实现 `IApplicationService`、不在 `ServiceContextBase.Assembly` 指向的程序集，或被 `ApplicationServiceTypeFilter` 排除时不会注册。
**解决**：让应用服务实现 `IApplicationService`（通常继承 `BaseApplicationService`）；检查上下文 `Assembly` 是否指向实现所在程序集；临时移除 `ApplicationServiceTypeFilter` 排查。

### `[Cache]` 没有缓存结果
**原因**：容器中没有 `ICacheService` 时缓存拦截器会直接放行；方法返回 `void`、非泛型 `Task`/`ValueTask`，或结果为 `null` 时也不会写缓存。
**解决**：注册缓存实现（如 `AddDefaultCacheManager<TComponent>` 或具体缓存模块）；把方法改为返回 `Task<T>`/`ValueTask<T>`；确认缓存键模板与参数匹配，且结果非空。

### 用例执行器报无法解析 `TUseCase`
**原因**：`ExecuteAsync<TUseCase, TInput, TOutput>` 会创建新作用域并调用 `GetRequiredService<TUseCase>()`；`Register<TContext>()` 不会注册用例，未显式注册时解析失败，异常会写入 presenter 的 `OnFailed` 而不是向外抛出。
**解决**：在模块中 `context.Services.AddScoped<CancelOrderUseCase>()`（或按实际生命周期注册）；若使用 presenter，订阅 `OnFailed` 记录异常，避免静默失败。

## 最佳实践

- 用例显式注册，应用服务靠 `Register<TContext>()` 扫描；不要把用例塞进 `IApplicationService` 自动注册路径。
- 应用服务保持无状态、方法 `virtual`、公共无参构造；依赖通过构造函数或 `LazyServiceProvider` 获取。
- 缓存/锁/幂等等拦截器依赖的基础设施在宿主统一注册；缓存只标注在有返回值且结果非空的方法上。
- 用 DI 容器写代理测试：分别按接口与实现类型解析，断言拦截器执行且同一作用域共享目标实例。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`；包版本由仓库根 `project.props` 统一管理（当前 `2026.5.0`）。
- 关键依赖：`Euonia.Modularity`（Castle.Core 5.2.1）、`Euonia.Caching`、`Euonia.Concurrency`、`Euonia.Pipeline`、`Euonia.Validation`、`Euonia.Uow`。
- 已知边界：类代理要求无参构造与 `virtual` 方法；`Register<T>` 不注册用例；缓存拦截器在缺少 `ICacheService` 时静默退化。
- 升级提示：代理注册与拦截器排序以当前源码为准；升级 Modularity/Castle 后重跑应用服务注册与拦截器测试。

## 相关包

- [`Euonia.Domain`](../Euonia.Domain/README.md) —— DDD 构建块
- [`Euonia.Pipeline`](../Euonia.Pipeline/README.md) —— 中间件管道
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与生命周期
- [`Euonia.Validation`](../Euonia.Validation/README.md) —— 校验框架
- [`Euonia.Caching`](../Euonia.Caching/README.md) —— `ICacheService` 缓存实现
- [`Euonia.Concurrency`](../Euonia.Concurrency/README.md) —— 锁与信号量实现

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
