# Euonia.Endpoint.Grpc API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

基于 `protobuf-net.Grpc` 的 code-first gRPC 运行时：把 `Euonia.Endpoint` 标记的接口方法暴露为 gRPC 服务，支持四种方法类型、方法级授权与 proto 导出。本页是 API 速查；完整规则见 [项目 README](../../../Source/Euonia.Endpoint.Grpc/README.md)。

- **NuGet 包**：`Euonia.Endpoint.Grpc`
- **目标框架**：`net10.0`、`net11.0`（`FrameworkReference Microsoft.AspNetCore.App`）
- **根命名空间**：`Nerosoft.Euonia.Endpoint.Grpc`（`AddDynamicGrpcEndpoint` 在 `Microsoft.Extensions.DependencyInjection`，`MapDynamicGrpcEndpoint` 在 `Microsoft.AspNetCore.Builder`）
- **关键依赖**：`protobuf-net` 3.4.40、`protobuf-net.Grpc` / `.AspNetCore` / `.Reflection` 1.3.14、`Grpc.AspNetCore.Server.Reflection`
- **完整参考**：[Source/Euonia.Endpoint.Grpc/README.md](../../../Source/Euonia.Endpoint.Grpc/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `AddDynamicGrpcEndpoint(IConfiguration, params Assembly[])` | 绑定 `Euonia:Endpoint:Grpc` 并注册运行时与拦截器 | 开关打开时执行生成的 `AddGenerated` |
| `AddDynamicGrpcEndpoint(Action<GrpcExposureOptions>, params Assembly[])` | 回调注册 | 显式程序集优先 |
| `MapDynamicGrpcEndpoint()` | 映射生成的 gRPC 服务 | `EnableReflection` 时映射 reflection |
| `GrpcExposureOptions.EnableExposeApplicationServiceAsGrpc` | 总开关 | `false` |
| `GrpcExposureOptions.RequireAuthorization` | 方法级授权要求 | `true` |
| `GrpcExposureOptions.EnableReflection` | 是否映射 gRPC reflection | `false` |
| `GrpcExposureOptions.ThrowOnUnregisteredService` | 未注册服务时抛异常 | `true`（生成代码读取） |
| `GrpcExposureOptions.ScanAssemblies` / `ScanEntryAssembly` | 扫描范围 | 空数组 / `true` |
| `GrpcMetadataRegistry` | 方法级授权元数据：`Build` / `GetServiceMetadata` / `GetMethodMetadata` | 由注册流程构建 |
| `GrpcMethodAuthorizationInterceptor` | 内置拦截器，执行方法级授权 | 失败返回 `PermissionDenied` |
| `GrpcMethodAuthorizationDescriptor` | 描述符：`MethodPath` / `ServiceType` / `MethodName` | 生成器写入 |
| `EndpointProtoExporter.Export(Assembly, string)` | 导出 `.proto` | 返回文件路径列表 |
| `GrpcGeneratedMappingAttribute` | 程序集级标记，指向 `GrpcMappings` | 生成器写入 |

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Endpoint.Grpc/README.md) —— 四种方法类型、DTO 规则、认证与限制
- [Euonia.Endpoint 用户手册](../Euonia.Endpoint/API.md) —— 暴露特性与共享契约
- [Euonia.Endpoint.SourceGenerator 用户手册](../Euonia.Endpoint.SourceGenerator/API.md) —— 生成契约与适配器
- [Euonia.Endpoint.Http 用户手册](../Euonia.Endpoint.Http/API.md) —— HTTP 侧运行时
- [仓库根 README](../../../README.md)
