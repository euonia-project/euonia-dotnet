# Euonia.Endpoint.SourceGenerator API 参考

> 本页是 API 参考；教程见 [TUTORIAL.md](TUTORIAL.md)。
> 完整公开类型与成员签名见 [API-FULL.md](API-FULL.md)。

`Euonia.Endpoint` 的构建期增量生成器（Roslyn analyzer）：为带 `[ExposeWebApi]` / `[ExposeGrpc]` 的接口方法生成强类型 HTTP lambda、gRPC 契约与适配器、注册代码与程序集标记。本页是 API 速查；打包与调试细节见 [项目 README](../../../Source/Euonia.Endpoint.SourceGenerator/README.md)。

- **NuGet 包**：`Euonia.Endpoint.SourceGenerator`（`DevelopmentDependency`，只含 analyzer）
- **目标框架**：`netstandard2.0`（发布用 analyzer）、`net10.0`（仅 C# Dev Kit / 本地 IDE）
- **根命名空间**：`Nerosoft.Euonia.Endpoint.SourceGenerator`；生成代码在 `Nerosoft.Euonia.Endpoint.Generated`
- **依赖**：Roslyn `Microsoft.CodeAnalysis.CSharp` 5.9.0（`PrivateAssets="all"`）
- **完整参考**：[Source/Euonia.Endpoint.SourceGenerator/README.md](../../../Source/Euonia.Endpoint.SourceGenerator/README.md)

## API 参考

### MSBuild 配置

| 属性/元素 | 说明 | 关键值 |
|---|---|---|
| `PackageReference` + `PrivateAssets` | 避免 analyzer 成为运行时传递依赖 | `all` |
| `OutputItemType` | 声明为分析器 | `Analyzer` |
| `ReferenceOutputAssembly` | 不把生成器程序集复制到输出 | `false`（项目引用建议） |
| `EmitCompilerGeneratedFiles` | 把生成文件落盘，仅排障用 | `true` / `false` |
| `CompilerGeneratedFilesOutputPath` | 生成文件目录 | 建议放 `obj/` 下 |
| `WarningsAsErrors` | 把诊断升级为错误 | `EUONIA001;EUONIA002` |
| 包内容路径 | analyzer 在包内的位置 | `analyzers/dotnet/cs/Euonia.Endpoint.SourceGenerator.dll` |

### 生成成员

| 类型/成员 | 说明 | 关键参数/默认值 |
|---|---|---|
| `Nerosoft.Euonia.Endpoint.Generated.HttpMappings.MapGenerated(IEndpointRouteBuilder)` | 生成路径的 HTTP 映射入口 | 由 `MapDynamicHttpEndpoint` 调用 |
| `HttpGeneratedMapper.Map(...)` | 生成代码使用的映射与去重入口 | 传入路由、动词、参数类型、lambda |
| `HttpGeneratedMappingAttribute` | 程序集级标记 | 指向 `HttpMappings` 类型 |
| `Nerosoft.Euonia.Endpoint.Generated.GrpcMappings.AddGenerated(IServiceCollection)` | 注册生成的 gRPC 适配器 | 由 `AddDynamicGrpcEndpoint` 调用 |
| `GrpcMappings.MapGenerated(IEndpointRouteBuilder)` | 映射生成的 gRPC 服务 | 由 `MapDynamicGrpcEndpoint` 调用 |
| `GrpcGeneratedMappingAttribute` | 程序集级标记 | 指向 `GrpcMappings` 类型 |
| `I{Service}GrpcContract{index}` / `{Service}GrpcAdapter{index}` | 生成的契约接口与适配器 | 命名空间 `Nerosoft.Euonia.Endpoint.Generated` |
| `EUONIA001` | `GrpcMethod` 与签名不匹配（Warning） | 方法被跳过 |
| `EUONIA002` | 多个契约推导出相同 gRPC 服务名（Warning） | 重复契约被跳过 |

本页不涉及配置节：生成器没有 `appsettings` 选项，行为由接口标注与 MSBuild 属性决定。

## 相关文档

- [教程](TUTORIAL.md)
- [项目 README](../../../Source/Euonia.Endpoint.SourceGenerator/README.md) —— 生成内容、诊断、多目标打包与调试
- [Euonia.Endpoint 用户手册](../Euonia.Endpoint/API.md) —— 暴露特性与共享契约
- [Euonia.Endpoint.Http 用户手册](../Euonia.Endpoint.Http/API.md) —— 消费生成映射的 HTTP 运行时
- [Euonia.Endpoint.Grpc 用户手册](../Euonia.Endpoint.Grpc/API.md) —— 消费生成契约的 gRPC 运行时
- [仓库根 README](../../../README.md)
