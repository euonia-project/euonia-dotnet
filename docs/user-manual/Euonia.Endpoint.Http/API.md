# Euonia.Endpoint.Http API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

把 `Euonia.Endpoint` 标记的接口方法映射为 ASP.NET Core Minimal API 端点：生成优先 + 运行时兜底、参数绑定、认证元数据与扫描范围控制。本页是 API 速查；完整规则见 [项目 README](../../../Source/Euonia.Endpoint.Http/README.md)。

- **NuGet 包**：`Euonia.Endpoint.Http`
- **目标框架**：`net10.0`、`net11.0`（`FrameworkReference Microsoft.AspNetCore.App`）
- **根命名空间**：`Nerosoft.Euonia.Endpoint.Http`（`AddDynamicHttpEndpoint` 在 `Microsoft.Extensions.DependencyInjection`，`MapDynamicHttpEndpoint` 在 `Microsoft.AspNetCore.Builder`）
- **依赖**：`Euonia.Endpoint`、`Microsoft.Extensions.Logging.Abstractions`、`Microsoft.Extensions.Options.ConfigurationExtensions`
- **完整参考**：[Source/Euonia.Endpoint.Http/README.md](../../../Source/Euonia.Endpoint.Http/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddDynamicHttpEndpoint(IConfiguration, params Assembly[])` | 绑定 `Euonia:Endpoint:Http` 并注册运行时 | 程序集为空时用配置扫描范围 |
| `AddDynamicHttpEndpoint(Action<HttpExposureOptions>, params Assembly[])` | 回调注册 | 显式程序集后不再回退配置 |
| `MapDynamicHttpEndpoint()` | 先跑生成映射，再跑运行时兜底 | 开关关闭时不映射 |
| `HttpExposureOptions.EnableExposeApplicationServiceAsWebApi` | 总开关 | `false` |
| `HttpExposureOptions.DefaultRoutePrefix` | `Prefix` 为空时的默认前缀 | `"api"` |
| `HttpExposureOptions.RequireAuthorization` | 每个端点追加默认授权要求 | `true` |
| `HttpExposureOptions.ThrowOnUnregisteredService` | 服务未注册时抛异常 | `true` |
| `HttpExposureOptions.ThrowOnRouteConflict` | 路由冲突时抛异常 | `true` |
| `HttpExposureOptions.ScanAssemblies` / `ScanNamespaces` / `ScanTypes` | 扫描范围 | 均为空数组；命名空间按 `.` 段边界匹配 |
| `HttpExposureOptions.ScanEntryAssembly` | 未配置程序集时是否扫入口程序集 | `true` |
| `HttpRegistry.TryReserve(routePattern, methods, generated)` | 「路由 + 动词」占用与冲突检测 | 返回 `HttpReservation` |
| `HttpReservation` | 占用结果 | `Reserved` / `AlreadyGenerated` / `Conflict` |
| `HttpGeneratedMapper.Map(...)` / `ReadBodyAsync<T>(...)` | 生成代码调用的映射入口 | 由 SourceGenerator 生成 |
| `HttpGeneratedMappingAttribute` | 程序集级标记，指向 `HttpMappings` | 生成器写入 |

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Endpoint.Http/README.md) —— 参数绑定、路由推导、认证元数据与限制
- [Euonia.Endpoint 用户手册](../Euonia.Endpoint/API.md) —— 暴露特性与共享契约
- [Euonia.Endpoint.SourceGenerator 用户手册](../Euonia.Endpoint.SourceGenerator/API.md) —— 生成强类型映射
- [Euonia.Endpoint.Grpc 用户手册](../Euonia.Endpoint.Grpc/API.md) —— gRPC 侧运行时
- [仓库根 README](../../../README.md)
