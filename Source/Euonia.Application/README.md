# Euonia.Application

Euonia 应用层构建块：用例（Use Case）、应用服务基类、基于 Castle DynamicProxy 的拦截器，以及日志、校验、授权、追踪等消息管道行为，用于把横切关注点从业务代码中剥离。

Application layer building blocks for Euonia: use cases, application service base classes, Castle DynamicProxy interceptors and message pipeline behaviors for cross-cutting concerns such as logging, validation, authorization and tracing.

- **目标框架**：`net9.0`、`net10.0`
- **根命名空间**：`Nerosoft.Euonia.Application`
- **NuGet 包**：`Euonia.Application`

## 安装

```bash
dotnet add package Euonia.Application
```

## 主要功能

- **用例模型**：`IUseCase<TInput, TOutput>`、无输入/无输出变体与 `IUseCaseExecutor`，统一执行并分发到 presenter。
- **应用服务**：`BaseApplicationService` 通过 `ILazyServiceProvider` 懒加载总线、当前用户、请求上下文与日志。
- **自动注册**：`ServiceContextBase` + `services.Register<TContext>()` 按程序集扫描应用服务与管道行为，并为实现类型与接口生成共享目标实例的代理。
- **拦截器**：日志、授权、校验、追踪、锁、计时、缓存、缓存失效、幂等、重试与熔断。
- **管道行为**：`ValidationBehavior`、`UserContextBehavior`、`CorrelationIdBehavior`，可挂到消息总线的处理管道上。

## 快速开始

定义一个服务上下文，并交给 DI 自动扫描：

```csharp
using Nerosoft.Euonia.Application;

internal sealed class SalesServiceContext : ServiceContextBase
{
    public override void ConfigureServices(IServiceCollection services)
    {
        // 注册应用所需的额外服务
    }
}

// Program.cs
builder.Services.Register<SalesServiceContext>();
```

编写应用服务并使用拦截器特性：

```csharp
using Nerosoft.Euonia.Application;

public sealed class OrderApplicationService : BaseApplicationService, IApplicationService
{
    [Cache(Key = "order:{0}", TimeoutSeconds = 60, Groups = new[] { "orders" })]
    public Task<OrderDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        // 查询逻辑
    }

    [Retry(MaxRetries = 3, DelayMs = 200, Backoff = RetryBackoffMode.Exponential)]
    [DistributedLock("order:{id}")]
    public Task SubmitAsync(long id, CancellationToken cancellationToken)
    {
        // 提交逻辑
    }
}
```

实现一个类型化用例：

```csharp
public sealed class CancelOrderUseCase : IUseCase<CancelOrderInput, CancelOrderOutput>
{
    public Task<CancelOrderOutput> ExecuteAsync(CancelOrderInput input, CancellationToken cancellationToken = default)
    {
        // 业务逻辑
    }
}
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `IServiceContext` / `ServiceContextBase` | 描述应用服务所在程序集、生命周期与自动注册开关 |
| `Register<TContext>()` | 注册服务上下文并自动扫描应用服务与管道行为 |
| `IApplicationService` / `BaseApplicationService` | 应用服务契约与基类（懒加载 `IBus`、`UserPrincipal`、日志等） |
| `IUseCase<TInput, TOutput>` | 类型化用例契约 |
| `IUseCaseExecutor` | 执行用例并把成功/失败结果分发给 presenter |
| `IUseCasePresenter<TOutput>` / `DefaultUseCasePresenter<TOutput>` | 结果呈现器及其默认实现 |
| `ApplicationModule` | 注册全部拦截器、管道行为、presenter 与用例执行器 |

**常用拦截器特性**

| 特性 | 作用 |
|------|------|
| `[Cache]` / `[CacheEvict]` | 缓存方法结果 / 按组失效缓存 |
| `[Retry]` | 按固定、线性或指数退避重试 |
| `[CircuitBreaker]` | 失败率达到阈值后短路调用 |
| `[Lock]` / `[DistributedLock]` / `[SemaphoreLock]` | 进程内或分布式互斥、信号量 |
| `[Idempotent]` | 幂等窗口内对相同指纹的重复调用只执行一次 |
| `[Timing]` / `[SensitiveData]` | 计时告警与敏感数据脱敏（`TracingInterceptor` 全局记录调用链） |

## 相关包

- [`Euonia.Domain`](../Euonia.Domain/README.md) —— DDD 构建块
- [`Euonia.Pipeline`](../Euonia.Pipeline/README.md) —— 中间件管道
- [`Euonia.Modularity`](../Euonia.Modularity/README.md) —— 模块与生命周期
- [`Euonia.Validation`](../Euonia.Validation/README.md) —— 校验框架
- [`Euonia.Caching`](../Euonia.Caching/README.md) —— `ICacheService` 缓存实现

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
