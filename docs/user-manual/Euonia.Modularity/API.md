# Euonia.Modularity API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。

`Euonia.Modularity` 把应用拆成可组合模块，按 `[DependsOn]` 拓扑加载，并统一管理服务注册、约定式自动注册、初始化与关闭生命周期。

- **包名**：`Euonia.Modularity`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Modularity`（部分扩展位于 `Microsoft.Extensions.DependencyInjection`、`System`）
- **完整选项/边界**：[Source/Euonia.Modularity/README.md](../../../Source/Euonia.Modularity/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|-----------|------|-----------------|
| `ModuleContextBase` | 模块基类，提供 `ConfigureServices`、`OnApplicationInitialization`、`OnApplicationShutdown` | `AutomaticRegisterService` 默认 `true` |
| `[DependsOn(typeof(T))]` | 声明模块依赖，加载器按拓扑排序 | 可重复标注多个依赖 |
| `ApplicationFactory.Create<TStartupModule>(services, configuration, optionsAction?)` | 创建应用并完成模块加载/服务注册 | `configuration` 可为 `null` |
| `ApplicationFactory.CreateAsync<TStartupModule>(...)` | 异步服务配置版本 | 支持 `CancellationToken` |
| `AddModularityApplication<TStartupModule>(configuration, optionsAction?)` | ASP.NET Core/DI 入口，返回 `IApplicationWithServiceProvider` | 仍需 `Initialize` 或 `InitializeApplication` |
| `IApplicationWithServiceProvider.Initialize(provider)` | 设置服务提供程序并执行模块初始化 | 同一 provider 重复调用幂等 |
| `ServiceConfigurationContext` | `ConfigureServices` 的上下文 | `Services`、`Configuration` |
| `ApplicationInitializationContext` | 初始化上下文，可解析 `IApplicationBuilder` 等 | 依赖 Hosting 扩展 |
| `[ExportService(lifetime, params Type[])]` | 约定式注册实现类型及其服务类型 | `TryRegister`、`ReplaceServices`、`IncludeSelf` |
| `[ExposeServices(params Type[])]` | 显式声明要公开的服务类型 | `IncludeDefaults`、`IncludeSelf` |
| `[DisableAutomaticRegistration]` | 跳过该类型的约定式自动注册 | 不影响显式 `services.AddXxx` |
| `ILazyServiceProvider` | 按需解析并缓存服务 | `GetService<T>`、`GetRequiredService<T>`、键控重载 |
| `IServiceAccessor` | 访问当前服务提供程序 | `ServiceProvider` |
| `IRequestContextAccessor` | 访问当前 `RequestContext` | 请求流外返回 `null` |
| `ConfigurationHelper.BuildConfiguration(options?, builderAction?)` | 构建 JSON/用户机密/命令行/环境变量配置 | 环境变量源位于末尾 |
| `ConfigurationBuilderOptions` | 配置构建选项 | `FileName` 默认 `appsettings`；`ReloadOnChange` 默认 `true` |
| `IModuleContainer.IsModuleLoaded<T>()` / `FindModule<T>()` | 查询已加载模块 | 返回 `bool` / `IModuleDescriptor?` |
| `ModuleLifecycleOptions` | 注册额外模块生命周期实现 | `Lifecycle` 类型列表 |

### 配置示例：ConnectionStrings

`ConfigurationHelper` 支持标准 `ConnectionStrings` 节，连接串解析按名称回退：

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

两种写法都映射到 `ConnectionStrings:Default`；第二种是模块化配置的别名规则，仅在经过 `ConfigurationHelper`/模块化包装时生效。前缀与优先级的完整规则见 [Source README](../../../Source/Euonia.Modularity/README.md#配置与环境变量)。

## 相关文档

- [教程](TUTORIAL.md)
- [Source README：完整安装、选项与边界](../../../Source/Euonia.Modularity/README.md)
- [Euonia.Hosting 用户手册](../Euonia.Hosting/API.md) —— Web 宿主中的模块初始化
- [Euonia.Application 用户手册](../Euonia.Application/API.md) —— 应用服务自动注册与拦截器
- [Euonia.Pipeline 用户手册](../Euonia.Pipeline/API.md) —— 模块内管道
- [仓库根 README](../../../README.md)
