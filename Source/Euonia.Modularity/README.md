# Euonia.Modularity

Euonia 的模块化与依赖注入基础设施：把应用拆分为可组合模块，按 `[DependsOn]` 拓扑加载，统一管理服务注册、自动注册、初始化与关闭生命周期，并提供请求上下文、懒加载服务提供者与命名服务解析。

Modularity and dependency injection infrastructure for Euonia: compose an application from modules loaded in `[DependsOn]` topological order, with unified service registration, conventional auto-registration, initialization/shutdown lifecycle, plus request context, lazy service provider and named service resolution.

- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Modularity`
- **NuGet 包**：`Euonia.Modularity`

## 安装

```bash
dotnet add package Euonia.Modularity
```

## 主要功能

- **模块系统**：`ModuleContextBase` + `[DependsOn(typeof(...))]`，自动按依赖顺序加载。
- **应用工厂**：`ApplicationFactory.Create<TStartupModule>(services, configuration)`。
- **生命周期**：`AheadConfigureServices`、`ConfigureServices`、`AfterConfigureServices`、`OnApplicationInitialization`、`OnApplicationShutdown`。
- **自动注册**：按约定扫描并注册服务；`[ExposeServices]`、`[ExportService]`、`[DisableAutomaticRegistration]` 控制行为。
- **访问器**：`IServiceAccessor`、`IRequestContextAccessor`、`IObjectAccessor<T>`。
- **懒加载**：`ILazyServiceProvider` 按需解析并缓存服务。
- **命名服务**：`GetNamedService<T>(name)` 与键控注册配合。

## 快速开始

定义模块：

```csharp
using Nerosoft.Euonia.Modularity;

[DependsOn(typeof(RepositoryModule))]
public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddScoped<IOrderService, OrderService>();
        Configure<SalesOptions>(context.Configuration.GetSection("Sales"));
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        // 应用启动后的初始化
    }
}
```

创建并初始化应用：

```csharp
using Nerosoft.Euonia.Modularity;

var services = new ServiceCollection();
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

using var application = ApplicationFactory.Create<SalesModule>(services, configuration);
using var serviceProvider = services.BuildServiceProvider();
application.Initialize(serviceProvider);

// ...
application.Shutdown();
```

依赖注入中使用访问器：

```csharp
public sealed class OrderService(ILazyServiceProvider lazyProvider, IServiceAccessor accessor)
{
    public void DoWork()
    {
        var bus = lazyProvider.GetService<IBus>();
        var provider = accessor.ServiceProvider;
    }
}
```

## 核心类型

| 类型 | 作用 |
|------|------|
| `ApplicationFactory` | `Create<TStartupModule>` / `Create(Type, ...)` 创建应用 |
| `IApplicationWithServiceProvider` / `IModularityApplication` | 应用实例：服务提供程序、初始化与关闭 |
| `ModuleContextBase` / `IModuleContext` | 模块基类与生命周期钩子 |
| `DependsOnAttribute` | 声明模块依赖 |
| `ServiceConfigurationContext` | 服务注册上下文（包含 `Services` 与 `Configuration`） |
| `ApplicationInitializationContext` / `ApplicationShutdownContext` | 初始化与关闭上下文 |
| `ExposeServicesAttribute` / `ExportServiceAttribute` | 显式暴露/注册服务 |
| `DisableAutomaticRegistrationAttribute` | 跳过约定式自动注册 |
| `ILazyServiceProvider` | 懒加载服务解析 |
| `IServiceAccessor` / `IRequestContextAccessor` / `IObjectAccessor<T>` | 全局与请求级访问器 |

## 相关包

- [`Euonia.Core`](../Euonia.Core/README.md) —— 基础工具
- [`Euonia.Hosting`](../Euonia.Hosting/README.md) —— ASP.NET Core 宿主集成
- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 仓储模块
- [`Euonia.Application`](../Euonia.Application/README.md) —— 应用服务自动注册

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
