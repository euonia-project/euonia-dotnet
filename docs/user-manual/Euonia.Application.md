# Euonia.Application 用户手册

`Euonia.Application` 提供用例、应用服务基类与 Castle 拦截器，把日志、校验、授权、缓存、锁、重试、熔断等横切逻辑从业务代码中剥离。

- **包名**：`Euonia.Application`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Application`（`Register<TService>()` 位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项/边界**：[Source/Euonia.Application/README.md](../../Source/Euonia.Application/README.md)

## 教程：让应用服务自动重试瞬时失败

这个场景创建一个控制台应用，通过 `ApplicationModule` 注册拦截器，用 `Register<SalesServiceContext>()` 扫描应用服务，再验证 `[Retry]` 在类代理上生效。

### 前置条件

- .NET 10 或 .NET 11 SDK。
- 不要求数据库或缓存；示例只使用 `[Retry]`，它不依赖外部基础设施。

### 1. 创建项目并安装

```bash
dotnet new console -n SalesApp
cd SalesApp
dotnet add package Euonia.Application
dotnet add package Microsoft.Extensions.Logging
dotnet add package Microsoft.Extensions.Logging.Console
```

### 2. 定义应用服务与上下文

```csharp
using System.Threading;
using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Modularity;

public interface IOrderService
{
    Task<string> GetCustomerAsync(long orderId);
}

public sealed class OrderService : BaseApplicationService, IOrderService
{
    private int _attempts;

    [Retry(MaxRetries = 2, DelayMs = 10)]
    public virtual Task<string> GetCustomerAsync(long orderId)
    {
        var attempt = Interlocked.Increment(ref _attempts);
        if (attempt < 3)
        {
            throw new InvalidOperationException($"transient failure {attempt}");
        }

        return Task.FromResult($"customer-{orderId}");
    }
}

public sealed class SalesServiceContext : ServiceContextBase
{
}

[DependsOn(typeof(ApplicationModule))]
public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.Register<SalesServiceContext>();
    }
}
```

### 3. 创建应用并解析代理

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Modularity;

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());

var configuration = ConfigurationHelper.BuildConfiguration(new ConfigurationBuilderOptions
{
    FileName = "appsettings",
    EnvironmentName = "Development"
});

using var application = ApplicationFactory.Create<SalesModule>(services, configuration);
using var provider = services.BuildServiceProvider();

application.Initialize(provider);

var orderService = provider.GetRequiredService<IOrderService>();
Console.WriteLine(await orderService.GetCustomerAsync(42));

application.Shutdown();
```

### 4. 运行与验证

```bash
dotnet run
```

预期输出：

```text
customer-42
```

目标方法前两次抛异常、第三次成功，说明 `[Retry(MaxRetries = 2)]` 生效。把方法改成非 `virtual`，或直接 `new OrderService()`，输出会变成未处理的异常或不再重试，可用来验证代理边界。

### 常见失败点

- **拦截器完全不生效**：应用服务不是从 DI 解析、方法不是 `virtual`，或实现类没有公共无参构造函数；类代理需要无参构造，接口代理可绕过该限制。
- **应用服务没被注册**：`Register<TContext>()` 只扫描 `IApplicationService` 实现与管道行为；确认 `OrderService` 继承 `BaseApplicationService`（实现 `IApplicationService`），且上下文 `Assembly` 指向实现所在程序集。
- **`AddLogging` 缺失导致解析代理失败**：`ApplicationModule` 注册的 `LoggingInterceptor` 依赖 `ILoggerFactory`；控制台宿主需 `services.AddLogging(...)`。
- **`[Cache]` 不缓存**：没有 `ICacheService`、方法返回 `void`/非泛型 `Task`，或结果为 `null`；缓存拦截器会静默放行或跳过写回。

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `ApplicationModule` | 注册全部拦截器、管道行为、presenter 与用例执行器 | 通过 `[DependsOn]` 引入 |
| `ServiceContextBase` | 应用服务上下文基类，默认开启自动注册 | `Assembly` 默认上下文类型程序集 |
| `Register<TContext>()` | 注册上下文并扫描应用服务/管道行为 | 不注册用例 |
| `IApplicationService` | 应用服务标记接口 | 继承 `IHasLazyServiceProvider` |
| `BaseApplicationService` | 应用服务基类，懒加载总线、用户、日志、执行器 | `Bus`、`User`、`RequestContext`、`Logger`、`Executor` |
| `IUseCase<TInput, TOutput>` | 类型化用例契约 | 另有 `INonInputUseCase<T>`、`INonOutputUseCase<T>`、`IParameterlessUseCase` |
| `IUseCaseExecutor` | 执行用例并分发到 presenter | 容器重载每次创建新作用域 |
| `DefaultUseCasePresenter<TOutput>` | 默认结果呈现器 | `OnSucceed`、`OnFailed`、`OnCanceled`、`Output` |
| `[Cache(Key, TimeoutSeconds, Groups)]` | 缓存有返回值的方法结果 | 无 `ICacheService` 时放行 |
| `[CacheEvict(params string[] groups)]` | 按组失效缓存 | `Mode` 默认 `After` |
| `[Retry(MaxRetries, DelayMs, Backoff)]` | 失败重试 | `MaxRetries` 默认 3；`Backoff` 默认 `Fixed` |
| `[CircuitBreaker(MaxFailures, ResetTimeoutSeconds, SuccessThreshold)]` | 连续失败后短路 | 默认 5 次 / 30 秒 / 1 次成功 |
| `[DistributedLock(token)]` / `[SemaphoreLock(token, maximumCount)]` | 分布式互斥 / 进程内信号量 | 令牌支持 `{parameter}` 占位符 |
| `[Idempotent(Key, TimeoutSeconds)]` | 幂等窗口内去重 | `TimeoutSeconds` 默认 60 |
| `[Timing(ThresholdMs, WarningThresholdMs)]` | 记录慢调用 | 默认 1000 / 5000 毫秒 |
| `IOrderedInterceptor` | 控制拦截器顺序 | `Order` 数字小的在外层 |
| `ServiceContextBase.AutoRegisterApplicationService` | 是否扫描应用服务 | 默认 `true` |
| `ServiceContextBase.ApplicationServiceLifetime` | 应用服务代理生命周期 | 默认 `Scoped` |
| `ServiceContextBase.ApplicationServiceTypeFilter` | 自动注册类型筛选器 | 默认 `null` |

### 配置说明

`Euonia.Application` 没有配置节或 `*Options`；拦截器参数由特性属性配置，自动注册由 `ServiceContextBase` 的虚拟属性控制，因此本页不提供 appsettings 示例。

## 相关文档

- [Source README：完整拦截器、用例与代理边界](../../Source/Euonia.Application/README.md)
- [Euonia.Modularity 用户手册](./Euonia.Modularity.md) —— 模块与自动注册
- [Euonia.Pipeline 用户手册](./Euonia.Pipeline.md) —— 管道行为
- [Euonia.Validation 用户手册](./Euonia.Validation.md) —— 校验拦截器与异常
- [Euonia.Hosting 用户手册](./Euonia.Hosting.md) —— Web 宿主中的代理
- [仓库根 README](../../README.md)
