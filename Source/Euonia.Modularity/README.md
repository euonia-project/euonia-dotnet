# Euonia.Modularity

Euonia 的模块化与依赖注入基础设施：把应用拆分为可组合模块，按 `[DependsOn]` 拓扑加载，统一管理服务注册、自动注册、初始化与关闭生命周期，并提供请求上下文、懒加载服务提供者与命名服务解析。

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

## 配置项

`ConfigurationBuilderOptions` 是 `ConfigurationHelper.BuildConfiguration` 的选项对象（代码配置；模块化框架没有 `Euonia:Modularity` 之类的配置节）：

| 属性 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `FileName` | `string` | `appsettings` | JSON 文件名（不含扩展名） |
| `EnvironmentName` | `string?` | `null` | 环境名；未设置时读取 `DOTNET_ENVIRONMENT` / `ASPNETCORE_ENVIRONMENT` |
| `BasePath` | `string?` | `null` | 配置文件基础路径；未设置时使用当前工作目录 |
| `ReloadOnChange` | `bool` | `true` | 配置文件变更时是否重新加载 |
| `UserSecretsId` / `UserSecretsAssembly` | `string?` / `Assembly?` | `null` | 仅 Development 环境加载用户机密；`UserSecretsId` 优先 |
| `CommandLineArgs` | `string[]?` | `null` | 追加到配置的命令行参数 |
| `EnvironmentVariablesPrefix` | `string?` | `null` | 环境变量前缀；同时作用于原生与自定义两种格式 |

`ConnectionStrings` 是模块化配置中可用的标准节（连接串解析支持名称回退）：

```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=euonia;User Id=sa;Password=change-me"
  }
}
```

```bash
ConnectionStrings__Default="Server=localhost;Database=euonia;User Id=sa;Password=change-me"
CONNECTION_STRING_DEFAULT="Server=localhost;Database=euonia;User Id=sa;Password=change-me"
```

两种写法都映射到 `ConnectionStrings:Default`；第二种是模块化框架的别名规则。环境变量源的完整优先级、前缀陷阱与数组索引写法见下一节。

## 配置与环境变量

`ConfigurationHelper.BuildConfiguration` 依次加载 JSON 配置文件、环境 JSON 配置文件、用户机密、命令行参数、
`builderAction` 追加的配置源，然后追加自定义大写单下划线格式的环境变量源，**最后**追加原生 `__` 格式的环境变量源：

```text
原生 __ 环境变量 > 自定义 _ 环境变量 > appsettings/用户机密/命令行/builderAction 追加的源 > 代码默认值
```

两种格式并存：

- **原生格式**：配置键的层级分隔符 `:` 对应环境变量中的双下划线 `__`，单下划线 `_` 是普通字符，例如
  `Euonia:Bus:RabbitMq:Connection` 对应 `Euonia__Bus__RabbitMq__Connection`。
- **自定义格式**：全大写 + 单下划线，每个下划线都是层级分隔符，例如 `EUONIA_BUS_ENABLED` 对应
  `Euonia:Bus:Enabled`（详见下一小节）。
- 数组使用索引：`Euonia__Endpoint__Http__ScanAssemblies__0`，或自定义格式 `EUONIA_ENDPOINT_HTTP_SCANASSEMBLIES_0`。
- 设置为空字符串表示显式空值；要回退到配置文件或默认值，应**不设置**该变量。
- 可选用 `ConfigurationBuilderOptions.EnvironmentVariablesPrefix` 限定前缀（前缀比较忽略大小写，同时作用于两种格式）。

```csharp
var configuration = ConfigurationHelper.BuildConfiguration(new ConfigurationBuilderOptions
{
    EnvironmentName = "Production",
    // EnvironmentVariablesPrefix = "EUONIA_" // 仅加载 EUONIA_ 开头的变量
});

// 对宿主已有配置（如 ASP.NET Core 的 ConfigurationManager）追加环境变量源：
// 同一实例被直接追加；普通 IConfigurationRoot 返回分层视图，原实例不变。
var effective = ConfigurationHelper.EnsureEnvironmentVariables(configuration);
```

`ApplicationFactory.Create<T>(services, configuration)` 与 `AddModularityApplication<T>(configuration)`
都会先确保环境变量源位于最后再注册配置，因此 `application.Configuration` 与模块内的 `Configuration`
读到的值同样遵循“环境变量优先”。显式传入的 `ConfigurationRoot` 会被包装成分层视图，不保证与原实例引用相等；
`ConfigurationManager`（实现 `IConfigurationBuilder`）则保持同一实例。

### 自定义大写单下划线格式

除原生 `__` 格式外，`ConfigurationHelper` 还识别“全大写 + 单下划线”的部署变量：每个 `_` 是一个配置层级分隔符，
配置段内部不再拆分单词，读取时按配置键大小写不敏感匹配。

| 环境变量 | 映射到的配置键 | 说明 |
|---|---|---|
| `EUONIA_BUS_ENABLED` | `Euonia:Bus:Enabled` | 值原样写入，bool/string 均可 |
| `EUONIA_BUS_HEALTHCHECKS_MAXDEADLETTERS` | `Euonia:Bus:HealthChecks:MaxDeadLetters` | 四个层级；`HEALTHCHECKS` 是一个完整配置段 |
| `EUONIA_ENDPOINT_HTTP_SCANASSEMBLIES_0` | `Euonia:Endpoint:Http:ScanAssemblies:0` | 数组索引 |
| `CONNECTION_STRING_DEFAULT` | `ConnectionStrings:Default` | `configuration.GetConnectionString("Default")` 生效 |
| `CONNECTIONSTRINGS_DEFAULT` | `ConnectionStrings:Default` | 同上，别名拼写 |

规则与边界：

- **候选更严格**：只映射匹配 `^[A-Z][A-Z0-9]*(_[A-Z0-9]+)+$` 的变量名；`euonia_bus_enabled`、`EUONIA-BUS-ENABLED`、
  `EUONIABUSENABLED` 均不映射，含 `__` 的变量仍由原生格式处理。
- **原生格式优先**：同一配置键同时由两种格式提供时，原生 `__` 格式获胜（配置源按“自定义 → 原生”的顺序追加）。
- **连接串只在根级**：`CONNECTION_STRING_<NAME>` / `CONNECTIONSTRINGS_<NAME>` → `ConnectionStrings:<NAME>`，
  `<NAME>` 原样保留（含下划线，例如 `CONNECTION_STRING_MAIN_DB` 读取为 `GetConnectionString("Main_Db")`）；
  不写单数 `ConnectionString:`。两种拼写同时设置时 `CONNECTION_STRING_<NAME>` 获胜。
  **无前缀时** `EUONIA_CONNECTION_STRING_DEFAULT` 走通用规则（`EUONIA:CONNECTION:STRING:DEFAULT`），命名空间下的连接串请用
  `Euonia__ConnectionStrings__Default`；若前缀为 `EUONIA_`，该变量会先剥前缀得到 `CONNECTION_STRING_DEFAULT`，
  再命中特殊规则映射为 `ConnectionStrings:DEFAULT`（同样可由 `GetConnectionString("Default")` 读取）。
- **token 内不拆词**：`HEALTH_CHECKS` 会被当成两个层级而不是 `HealthChecks`；一个配置段内不要用下划线分词。
- **任意大写单下划线变量都会额外成为配置键**：没有黑名单，例如 `HTTP_PROXY` 还会产生 `HTTP:PROXY`、
  `DOTNET_ENVIRONMENT` 产生 `DOTNET:ENVIRONMENT`；若与某个配置节大小写不敏感同名冲突，请避免该节或使用前缀隔离
  （见本节末“模块化包装的前缀边界”）。
- **不改变原生语义**：原生格式仍把 `EUONIA_BUS_ENABLED` 暴露为字面键 `EUONIA_BUS_ENABLED`，自定义格式只是额外提供嵌套键。
- **无动态 reload**：环境变量在配置构建时快照，运行中修改需重新构建配置或重启进程（与原生的环境变量源一致）。

**前缀陷阱**：`EnvironmentVariablesPrefix` 对两种格式都生效，并且是“先剥前缀、再映射”。若把前缀设为 `EUONIA_`，
则 `EUONIA_BUS_ENABLED` 去掉前缀后得到 `BUS_ENABLED`，映射为 `BUS:ENABLED` 而不是 `Euonia:Bus:Enabled`；
要得到 `Euonia:Bus:Enabled` 需写成 `EUONIA_EUONIA_BUS_ENABLED`，推荐改用独立前缀（如 `APP_`）或不设置前缀：

```csharp
var aliasConfiguration = ConfigurationHelper.BuildConfiguration(new ConfigurationBuilderOptions
{
    EnvironmentVariablesPrefix = "APP_" // APP_EUONIA_BUS_ENABLED → Euonia:Bus:Enabled
});

// 已有宿主配置（如 ConfigurationManager）：
// 同一实例上按“自定义 → 原生”的顺序补齐两个源，重复调用幂等。
var effectiveAlias = ConfigurationHelper.EnsureEnvironmentVariables(hostConfiguration, "APP_");
```

宿主默认路径（`HostUtility.CreateHostBuilder`）同样先追加自定义格式的源、再追加原生 `__` 格式的源；
自定义 `createHostBuilder` 需自行按该顺序调用 `ConfigurationHelper.AddEnvironmentVariableAliases` 与
`AddEnvironmentVariables`。

**模块化包装的前缀边界**：`ApplicationFactory.Create` 与 `AddModularityApplication` 在包装宿主配置时会调用
`ConfigurationHelper.EnsureEnvironmentVariables`，并且**以无前缀方式**补齐 alias/native 源。若宿主先用
`ConfigurationBuilderOptions.EnvironmentVariablesPrefix` 构建了带前缀隔离的配置、再交给模块化包装，则无前缀的
`EUONIA_BUS_ENABLED` 等变量仍会在包装阶段被映射进来，前缀隔离**不是强保证**。需要严格隔离时，应避免把带前缀的配置
交给 `AddModularityApplication`/`ApplicationFactory.Create` 的自动包装，或确保进程中没有无前缀的可匹配变量；
如仍使用自动包装，应在包装后由应用侧显式校验/拒绝意外键（例如核对最终配置的键集合）。

### 自动绑定配置节的选项

以下选项类型暴露了配置节常量，并在注册扩展方法中自动绑定（无需宿主手工 `Configure`）：

| 选项类型 | 配置节 | 环境变量示例 |
|---|---|---|
| `HttpBusOptions.SectionName` | `Euonia:Bus:Http`（旧节 `HttpBus` 仍兼容） | `Euonia__Bus__Http__Endpoint` |
| `GrpcBusOptions.SectionName` | `Euonia:Bus:Grpc` | `Euonia__Bus__Grpc__Endpoint` |
| `BusHealthCheckOptions.SectionName` | `Euonia:Bus:HealthChecks` | `Euonia__Bus__HealthChecks__MaxDeadLetters` |
| `FileSynchronizationOptions.ConfigurationSection` | `FileSynchronization` | `FileSynchronization__Directory` |

自动绑定以**最低优先级**注册（配置节绑定作为 `IConfigureOptions<T>` 插入服务集合最前面），
因此 `services.Configure<T>(...)` 与 `AddX(..., configure)` 形参**无论写在 `AddX` 之前还是之后**都会覆盖
配置节与环境变量的值；显式编程入参始终优先。

### 环境变量优先的边界

- **后注册的配置源会覆盖环境变量**：`ConfigureAppConfiguration`（含 `HostUtility` 的
  `HostBuilderOptions.ConfigureHostBuilder`、ASP.NET Core `Startup.ConfigureAppConfiguration` 等）在环境变量源之后追加的
  JSON/内存/命令行源，仍会覆盖同名环境变量。需要绝对优先级时，请避免把这些键放进后续追加的源。
- **两种包装行为不同**：`ConfigurationManager`（ASP.NET Core `WebApplicationBuilder` 的 `Configuration`）路径会直接改写宿主配置，
  因此 Kestrel/Logging 等全局键同样变成“环境变量 > 命令行”；普通 `ConfigurationRoot` 路径只返回供模块化应用读取的分层视图，
  不会回写 `HostBuilderContext.Configuration`。
- **命令行不再覆盖环境变量**：`BuildConfiguration` 把环境变量源放在命令行参数与 `builderAction` 之后，与此前行为相反；
  如需命令行仍覆盖环境变量，应在构建完成后自行包装配置（例如 `.AddConfiguration(configuration).AddCommandLine(args)`）并明确承担优先级。
- **只读入口不影响全局**：`AddModularityApplication` 传入 `IConfiguration` 时只影响模块化应用与其注册的 `IConfiguration` 单例，
  非模块化的宿主组件仍读取宿主自己的配置。

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

## 使用示例

模块内注册命名服务，并在运行时按名称解析：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

public sealed class SalesModule : ModuleContextBase
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddScoped<IOrderService, OrderService>("primary");

        context.Services.AddNamedService<IPricingService>((name, provider) => name switch
        {
            "retail" => provider.GetRequiredService<RetailPricingService>(),
            _ => provider.GetRequiredService<WholesalePricingService>()
        });
    }
}
```

```csharp
var orderService = serviceProvider.GetNamedService<IOrderService>("primary");
var required = serviceProvider.GetNamedRequiredService<IOrderService>("primary");
```

也可以让约定式自动注册处理导出：

```csharp
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

[ExportService(ServiceLifetime.Scoped, typeof(IOrderService))]
public sealed class OrderService : IOrderService
{
}
```

模块的 `AutomaticRegisterService` 默认为 `true`；加载器会按 `[ExportService]`、`[ExposeServices]` 与约定注册该类型。需要跳过约定扫描时使用 `[DisableAutomaticRegistration]`，显式 `services.AddXxx` 不受影响。

## 注意事项 / 限制

- `[DependsOn]` 声明的是模块依赖图；同一模块类型只加载一次，循环依赖或无法构造的模块会在创建应用时失败。模块类型必须是实现 `IModuleContext` 的类。
- `AddModularityApplication<T>` / `ApplicationFactory.Create<T>` 只完成模块加载与服务注册；必须再调用返回实例的 `Initialize(serviceProvider)`（ASP.NET Core 中由 `app.InitializeApplication()` 完成）才会执行 `OnApplicationInitialization`。
- 约定式自动注册仅在模块的 `AutomaticRegisterService` 为 `true` 时执行；`[DisableAutomaticRegistration]` 只影响约定注册，不会移除已显式注册的服务。
- 命名服务的名称不能为空；同一服务类型下重复注册同名实现会抛出 `InvalidOperationException`，`GetNamedService<T>(name)` 在名称不存在时返回 `null`，需要失败语义时用 `GetNamedRequiredService<T>(name)`。
- 环境变量源位于配置末尾，原生 `__` 格式优先于自定义大写单下划线格式；前缀会在映射前剥离，把前缀设为 `EUONIA_` 会让 `EUONIA_BUS_ENABLED` 变成 `BUS:ENABLED`，需要 `Euonia:*` 键时应使用独立前缀或不设前缀。
- `ModuleContextBase.ConfigurationContext`（以及 `Services`、`Configure<T>`）只在模块执行服务配置期间可用；在 `ConfigureServices` 之外访问会抛出 `InvalidOperationException`。
- 对普通 `IConfigurationRoot` 调用 `ConfigurationHelper.EnsureEnvironmentVariables` 返回的是新的分层视图，不保证与宿主配置引用相等；`ConfigurationManager` 路径才会就地修改同一实例。

## 常见问题与故障排查

### `OnApplicationInitialization` 没有执行
**原因**：`ApplicationFactory.Create<T>` / `AddModularityApplication<T>` 只完成模块加载与服务注册，不会自动调用模块初始化；必须再调用 `Initialize(serviceProvider)`（ASP.NET Core 中由 `app.InitializeApplication()` 完成）。
**解决**：独立宿主在 `BuildServiceProvider()` 后调用 `application.Initialize(provider)`；Web 宿主在 `Startup.Configure` 最前面调用 `app.InitializeApplication()`，并确认返回值没有被提前丢弃。

### 模块没有加载，或启动时报循环依赖
**原因**：模块未出现在 `[DependsOn]` 依赖链中；自依赖/循环依赖会由 `ModuleLoader.SortByDependency` 抛 `ArgumentException`；依赖模块不存在时抛 `InvalidOperationException`。
**解决**：从启动模块开始补齐 `[DependsOn]`；用 `application.Modules`、`IsModuleLoaded<T>()`、`FindModule<T>()` 检查实际加载结果；模块类型必须实现 `IModuleContext` 且可被无参构造。

### 环境变量没有生效，或映射到了错误的键
**原因**：设置了 `EnvironmentVariablesPrefix` 后，前缀会先被剥离再映射；把前缀设为 `EUONIA_` 时，`EUONIA_BUS_ENABLED` 会变成 `BUS:ENABLED` 而不是 `Euonia:Bus:Enabled`。另外原生 `__` 源位于自定义 `_` 源之后，同键时原生格式优先。
**解决**：需要 `Euonia:*` 键时使用独立前缀（如 `APP_`）或不设前缀；用 `ConfigurationHelper.BuildConfiguration` 构建后打印最终键集合验证，不要混用多个前缀后依赖顺序。

### `GetNamedService<T>` 返回 `null`，或注册同名服务抛异常
**原因**：名称未注册、名称拼写不一致，或同一服务类型下重复注册同名实现。`GetNamedService` 找不到时返回 `null`；`GetNamedRequiredService` 才会抛异常。重复名称由 `RegisterNamedService` 抛 `InvalidOperationException`。
**解决**：集中管理名称常量；需要失败语义时用 `GetNamedRequiredService<T>(name)`；注册前检查名称是否已存在，避免在多个模块重复注册同一名称。

## 最佳实践

- 按能力拆分模块（领域、基础设施、应用层），`[DependsOn]` 只指向更底层模块，避免把启动模块作为其他模块的依赖。
- 在宿主启动路径只调用一次 `Initialize`，在停止路径调用 `Shutdown`；对同一服务提供程序重复调用是幂等的，测试中可安全清理。
- 配置统一走 `ConfigurationHelper.BuildConfiguration`，部署变量使用独立前缀或原生 `__`；对最终配置键集合做启动断言。
- 模块图测试用 `ApplicationFactory.Create` + `IsModuleLoaded`/`Modules`，配置用内存集合，避免依赖当前目录的 `appsettings.json`。

## 版本与兼容

- 目标框架：`net10.0`、`net11.0`；包版本由仓库根 `project.props` 统一管理（当前 `2026.5.0`）。
- 关键依赖：`Castle.Core` 5.2.1、`Microsoft.Extensions.*` 11.0.0（由 `Directory.Packages.props` 锁定）。
- 已知边界：环境变量源位于配置末尾；普通 `IConfigurationRoot` 包装后返回新视图；带前缀的配置交给自动包装时，前缀隔离不是强保证。
- 升级提示：环境变量优先于命令行、原生 `__` 优先于自定义 `_` 的规则已固定；升级后如仍依赖旧顺序，需要显式调整配置源。

## 相关包

- [`Euonia.Core`](../Euonia.Core/README.md) —— 基础工具
- [`Euonia.Hosting`](../Euonia.Hosting/README.md) —— ASP.NET Core 宿主集成
- [`Euonia.Repository`](../Euonia.Repository/README.md) —— 仓储模块
- [`Euonia.Application`](../Euonia.Application/README.md) —— 应用服务自动注册

## 许可证

本项目依据仓库根目录的 [GNU General Public License v3.0](../../LICENSE) 发布。
