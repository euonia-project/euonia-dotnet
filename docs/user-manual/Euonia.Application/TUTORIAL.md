# Euonia.Application 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

`Euonia.Application` 提供用例、应用服务基类与 Castle 拦截器，把日志、校验、授权、缓存、锁、重试、熔断等横切逻辑从业务代码中剥离。

- **包名**：`Euonia.Application`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Application`（`Register<TService>()` 位于 `Microsoft.Extensions.DependencyInjection`）
- **完整选项/边界**：[Source/Euonia.Application/README.md](../../../Source/Euonia.Application/README.md)

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

## 相关文档

- [API 参考](API.md)
- [Source README：完整拦截器、用例与代理边界](../../../Source/Euonia.Application/README.md)
- [Euonia.Modularity 用户手册](../Euonia.Modularity/TUTORIAL.md) —— 模块与自动注册
- [Euonia.Pipeline 用户手册](../Euonia.Pipeline/TUTORIAL.md) —— 管道行为
- [Euonia.Validation 用户手册](../Euonia.Validation/TUTORIAL.md) —— 校验拦截器与异常
- [Euonia.Hosting 用户手册](../Euonia.Hosting/TUTORIAL.md) —— Web 宿主中的代理
- [仓库根 README](../../../README.md)
