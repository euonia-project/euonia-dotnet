# Euonia.Endpoint API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

把应用服务接口方法同时暴露为 HTTP Minimal API 与 gRPC 服务的**契约包**：暴露特性、共享 `DataContract` 类型与方法签名约定。本页是 API 速查；运行时细节见 [项目 README](../../../Source/Euonia.Endpoint/README.md)。

- **NuGet 包**：`Euonia.Endpoint`
- **目标框架**：`net10.0`、`net11.0`
- **根命名空间**：`Nerosoft.Euonia.Endpoint`
- **依赖**：无（不引用 ASP.NET Core / protobuf-net；运行时由 `Euonia.Endpoint.Http` / `Euonia.Endpoint.Grpc` 提供）
- **完整参考**：[Source/Euonia.Endpoint/README.md](../../../Source/Euonia.Endpoint/README.md)

## API 参考

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `ExposeWebApiAttribute` | 暴露为 HTTP 端点 | `Prefix`、`Route`、`Method`（默认 `["POST"]`） |
| `ExposeGrpcAttribute` | 暴露为 gRPC 方法 | `Prefix`、`Route`、`Method`（默认 `GrpcMethod.Unary`） |
| `GrpcMethod` | gRPC 方法类型 | `Unary` / `ServerStreaming` / `ClientStreaming` / `DuplexStreaming` |
| `EndpointEmpty` | 空请求/空返回的占位类型 | 无成员 |
| `DecimalValue` / `GuidValue` | `decimal` / `Guid` 的 proto 表示 | `DecimalValue` 9 位小数 |
| `TimestampValue` / `DurationValue` | `DateTime` / `TimeSpan` 表示 | 支持 1970 年之前时间 |
| `DateOnlyValue` / `TimeOnlyValue` | `DateOnly` / `TimeOnly` 表示 | 共享契约，暂未自动替换内建 BCL 映射 |
| `AddDynamicHttpEndpoint(IConfiguration, params Assembly[])` | 注册 HTTP 运行时并绑定 `Euonia:Endpoint:Http` | 程序集为空时用配置扫描范围 |
| `AddDynamicHttpEndpoint(Action<HttpExposureOptions>, params Assembly[])` | 回调方式注册 | 回调优先于配置节 |
| `MapDynamicHttpEndpoint()` | 映射生成端点 + 运行时兜底端点 | 开关关闭时不映射 |
| `AddDynamicGrpcEndpoint(IConfiguration, params Assembly[])` | 注册 gRPC 运行时并绑定 `Euonia:Endpoint:Grpc` | 同时注册拦截器 |
| `AddDynamicGrpcEndpoint(Action<GrpcExposureOptions>, params Assembly[])` | 回调方式注册 | 无运行时兜底 |
| `MapDynamicGrpcEndpoint()` | 映射生成的 gRPC 服务 | `EnableReflection` 时映射 reflection |
| `HttpExposureOptions` / `GrpcExposureOptions` | 两个运行时的选项类型 | 见各自手册 |
| `EndpointProtoExporter.Export(Assembly, string)` | 导出 `.proto` 到目录 | 返回生成的文件路径列表 |

配置节 `Euonia:Endpoint:Http` / `Euonia:Endpoint:Grpc` 由运行时包绑定；本包自身不读取配置。完整选项表见 [Euonia.Endpoint.Http](../Euonia.Endpoint.Http/API.md) 与 [Euonia.Endpoint.Grpc](../Euonia.Endpoint.Grpc/API.md)。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Endpoint/README.md) —— 签名约定、DTO 规则、行为说明
- [Euonia.Endpoint.Http 用户手册](../Euonia.Endpoint.Http/API.md) —— HTTP 运行时与 9 个选项
- [Euonia.Endpoint.Grpc 用户手册](../Euonia.Endpoint.Grpc/API.md) —— gRPC 运行时、流式与 proto 导出
- [Euonia.Endpoint.SourceGenerator 用户手册](../Euonia.Endpoint.SourceGenerator/API.md) —— 构建期生成器
- [仓库根 README](../../../README.md)
