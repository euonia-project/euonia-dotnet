# Euonia.Modularity 教程

> 本页是教程部分；API 参考见 [API.md](API.md)。

`Euonia.Modularity` 把应用拆成可组合模块，按 `[DependsOn]` 拓扑加载，并统一管理服务注册、约定式自动注册、初始化与关闭生命周期。

- **包名**：`Euonia.Modularity`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Modularity`（部分扩展位于 `Microsoft.Extensions.DependencyInjection`、`System`）
- **完整选项/边界**：[Source/Euonia.Modularity/README.md](../../../Source/Euonia.Modularity/README.md)

## 教程：组装并启动一个模块化控制台应用

这个场景从空项目开始，定义“基础设施模块 → 销售模块”的依赖链，用 `[ExportService]` 自动注册服务，再通过 `ApplicationFactory` 初始化并解析服务。

### 前置条件

- 已安装 .NET 10 或 .NET 11 SDK。
- 能访问 NuGet；示例使用控制台宿主，不依赖 ASP.NET Core。

### 1. 创建项目并安装

```bash
dotnet new console -n SalesHost
cd SalesHost
dotnet add package Euonia.Modularity
```

### 2. 定义模块与导出服务

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

public interface IOrderRepository
{
    string FindCustomer(long orderId);
}

public sealed class InMemoryOrderRepository : IOrderRepository
{
    public string FindCustomer(long orderId) => $"customer-{orderId}";
}

public interface IOrderService
{
    string GetCustomer(long orderId);
}

// AutomaticRegisterService 默认 true；[ExportService] 让加载器把实现注册为 IOrderService。
[ExportService(ServiceLifetime.Scoped, typeof(IOrderService))]
public sealed class OrderService(IOrderRepository repository) : IOrderService
{
    public string GetCustomer(long orderId) => repository.FindCustomer(orderId);
}

public sealed class InfrastructureModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
    }
}

[DependsOn(typeof(InfrastructureModule))]
public sealed class SalesModule : ModuleContextBase
{
}
```

### 3. 创建、初始化并解析

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

var services = new ServiceCollection();
var configuration = ConfigurationHelper.BuildConfiguration(new ConfigurationBuilderOptions
{
    FileName = "appsettings",
    EnvironmentName = "Development"
});

using var application = ApplicationFactory.Create<SalesModule>(services, configuration);
using var provider = services.BuildServiceProvider();

// 只 Create 不会执行 OnApplicationInitialization，必须显式 Initialize。
application.Initialize(provider);

var orderService = provider.GetRequiredService<IOrderService>();
Console.WriteLine(orderService.GetCustomer(42)); // customer-42

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

再验证模块图：在 `Initialize` 前打印 `application.Modules`，应看到 `InfrastructureModule` 排在 `SalesModule` 之前；`application.IsModuleLoaded<InfrastructureModule>()` 应为 `true`。

### 常见失败点

- **`IOrderService` 无法解析**：`OrderService` 没标 `[ExportService]`，或所在程序集不在模块扫描范围内；也可显式 `context.Services.AddScoped<IOrderService, OrderService>()`。
- **初始化逻辑没执行**：只调用了 `ApplicationFactory.Create`，漏掉 `application.Initialize(provider)`；Web 宿主对应 `app.InitializeApplication()`。
- **循环依赖**：`[DependsOn]` 形成环时 `ModuleLoader.SortByDependency` 抛 `ArgumentException`；依赖不存在时抛 `InvalidOperationException`。
- **环境变量键不对**：设置 `EnvironmentVariablesPrefix = "EUONIA_"` 会把 `EUONIA_BUS_ENABLED` 映射成 `BUS:ENABLED`，而不是 `Euonia:Bus:Enabled`。

## 相关文档

- [API 参考](API.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Modularity/README.md)
- [Euonia.Hosting 用户手册](../Euonia.Hosting/TUTORIAL.md) —— Web 宿主中的模块初始化
- [Euonia.Application 用户手册](../Euonia.Application/TUTORIAL.md) —— 应用服务自动注册与拦截器
- [Euonia.Pipeline 用户手册](../Euonia.Pipeline/TUTORIAL.md) —— 模块内管道
- [仓库根 README](../../../README.md)
