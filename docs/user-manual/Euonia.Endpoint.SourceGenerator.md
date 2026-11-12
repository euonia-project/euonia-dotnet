# Euonia.Endpoint.SourceGenerator 用户手册

`Euonia.Endpoint` 的构建期增量生成器（Roslyn analyzer）：为带 `[ExposeWebApi]` / `[ExposeGrpc]` 的接口方法生成强类型 HTTP lambda、gRPC 契约与适配器、注册代码与程序集标记。本页是端到端教程与 API 速查；打包与调试细节见 [项目 README](../../Source/Euonia.Endpoint.SourceGenerator/README.md)。

- **NuGet 包**：`Euonia.Endpoint.SourceGenerator`（`DevelopmentDependency`，只含 analyzer）
- **目标框架**：`netstandard2.0`（发布用 analyzer）、`net10.0`（仅 C# Dev Kit / 本地 IDE）
- **根命名空间**：`Nerosoft.Euonia.Endpoint.SourceGenerator`；生成代码在 `Nerosoft.Euonia.Endpoint.Generated`
- **依赖**：Roslyn `Microsoft.CodeAnalysis.CSharp` 5.9.0（`PrivateAssets="all"`）
- **完整参考**：[Source/Euonia.Endpoint.SourceGenerator/README.md](../../Source/Euonia.Endpoint.SourceGenerator/README.md)

## 教程

### 场景：让端点走「生成路径」而不是运行时反射

一个 ASP.NET Core 项目已有 `IOrderService`，希望 HTTP 端点由编译期生成的强类型 lambda 映射（无反射、无重载歧义），并能在构建产物里看到生成的代码。

### 前置条件

.NET 10 SDK；声明接口的项目（analyzer 只在引用它的编译里生效）；宿主仍需要 `Euonia.Endpoint.Http` 或 `Euonia.Endpoint.Grpc`。

### 1. 安装（analyzer 形式）

```bash
dotnet add package Euonia.Endpoint.SourceGenerator
```

把普通引用改成 analyzer（`dotnet add package` 不会自动加这些元数据）：

```xml
<PackageReference Include="Euonia.Endpoint.SourceGenerator" Version="2026.5.0"
                  PrivateAssets="all"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

同仓开发时用项目引用：

```xml
<ProjectReference Include="..\Euonia.Endpoint.SourceGenerator\Euonia.Endpoint.SourceGenerator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

### 2. 标注接口

```csharp
using Nerosoft.Euonia.Endpoint;

public interface IOrderService
{
    [ExposeWebApi(Prefix = "api/orders", Route = "{id}", Method = new[] { "GET" })]
    [ExposeGrpc(Prefix = "sample.OrderService", Route = "Get")]
    Task<OrderDto> GetAsync(string id, CancellationToken cancellationToken = default);
}

public sealed class OrderDto
{
    public string Id { get; set; } = string.Empty;
}
```

生成器只处理**当前编译中的接口方法**：实例方法、非泛型、无 `ref/out/in/params`；HTTP 侧非 `CancellationToken` 的默认参数会跳过生成。

### 3. 查看生成代码

在消费项目里临时开启：

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
<CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
```

```bash
dotnet build
find obj/Generated -name '*.g.cs' | head
# 期望能看到 HttpMappings / GrpcMappings（命名空间 Nerosoft.Euonia.Endpoint.Generated）
```

生成内容：HTTP 的 `HttpMappings.MapGenerated(...)`、gRPC 的 `GrpcMappings.AddGenerated/MapGenerated`、请求包装类型与适配器、程序集级 `HttpGeneratedMapping` / `GrpcGeneratedMapping` 标记。

### 4. 运行时仍然要注册与映射

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddDynamicHttpEndpoint(builder.Configuration);
builder.Services.AddDynamicGrpcEndpoint(builder.Configuration);

var app = builder.Build();
app.MapDynamicHttpEndpoint();    // 内部先跑生成的 MapGenerated，再跑运行时兜底
app.MapDynamicGrpcEndpoint();
app.Run();
```

```bash
dotnet run
curl -s http://localhost:5000/api/orders/A-1     # 走生成路径
```

### 常见失败点

- **构建通过但没有生成代码**：引用缺 `OutputItemType="Analyzer"`，或引用加在了不声明接口的项目上。
- **某个方法没生成**：带非 `CancellationToken` 默认参数；gRPC 侧带 `CallContext` 参数的方法同样会跳过。
- **`EUONIA001`**：`GrpcMethod` 与流式签名不匹配，该方法被跳过（警告不中断构建）。
- **`EUONIA002`**：多个契约推导出同一 gRPC 服务名，重复契约被跳过；显式写 `Prefix` 消歧。
- **多目标项目报 `RS1041`**：仓库内已在 `net10.0` 目标条件抑制，消费方无需处理。

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

- [项目 README](../../Source/Euonia.Endpoint.SourceGenerator/README.md) —— 生成内容、诊断、多目标打包与调试
- [Euonia.Endpoint 用户手册](Euonia.Endpoint.md) —— 暴露特性与共享契约
- [Euonia.Endpoint.Http 用户手册](Euonia.Endpoint.Http.md) —— 消费生成映射的 HTTP 运行时
- [Euonia.Endpoint.Grpc 用户手册](Euonia.Endpoint.Grpc.md) —— 消费生成契约的 gRPC 运行时
- [仓库根 README](../../README.md)
