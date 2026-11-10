# Euonia.Endpoint.SourceGenerator

`Euonia.Endpoint` 的构建期增量生成器。

## 目标框架

- `netstandard2.0`：发布用 analyzer
- `net10.0`：C# Dev Kit / 本地 IDE

基于 Roslyn 5.9.0。

## 生成内容

### HTTP

扫描当前编译中带 `[ExposeWebApi]` 的接口方法，生成：

- 强类型 Minimal API lambda；
- 参数类型数组，用于运行时精确匹配重载；
- 程序集级 `[assembly: HttpGeneratedMapping(...)]` 标记；
- `Nerosoft.Euonia.Endpoint.Generated.HttpMappings.MapGenerated(...)`。

生成代码直接调用应用服务接口，不经反射：

```csharp
public static void MapGenerated(IEndpointRouteBuilder endpoints)
{
    HttpGeneratedMapper.Map(
        endpoints,
        typeof(global::MyApp.IOrderService),
        "GetAsync",
        "api/orders",
        "{id}",
        new[] { "GET" },
        new[] { typeof(string), typeof(CancellationToken) },
        async (string @id, [FromServices] IOrderService __service) =>
        {
            return Results.Ok(await __service.GetAsync(@id));
        });
}
```

### gRPC

扫描当前编译中带 `[ExposeGrpc]` 的接口方法，生成：

- `[Service]` 契约接口；
- 请求/响应包装类型；
- 调用应用服务的适配器；
- `AddGenerated` / `MapGenerated` 注册代码；
- 程序集级 `[assembly: GrpcGeneratedMapping(...)]` 标记；
- `Nerosoft.Euonia.Endpoint.Generated.GrpcMappings`。

## 引用方式

### 作为包引用

```xml
<PackageReference Include="Euonia.Endpoint.SourceGenerator"
                  Version="..."
                  PrivateAssets="all"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

### 作为项目引用

```xml
<ProjectReference Include="..\Euonia.Endpoint.SourceGenerator\Euonia.Endpoint.SourceGenerator.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

`ReferenceOutputAssembly="false"` 避免把生成器程序集作为运行时依赖复制到输出目录。

## 使用前提

- 特性必须标记在**接口方法**上；实现类方法不会被生成器扫描。
- 接口方法必须是实例方法、非泛型方法。
- HTTP：
  - 返回 `Task` / `Task<T>` / `ValueTask<T>`，或 `IResult`；
  - 参数不能是 `ref/out/in` / `params`；
  - 非 `CancellationToken` 的默认参数会跳过生成，由运行时兜底处理；
  - 不支持文件上传、`IAsyncEnumerable` 返回、多个 body 参数。
- gRPC：
  - 请求/响应 DTO 必须标 `[DataContract]`，可传输成员标 `[DataMember(Order = n)]`；`Order` 必须唯一且发布后稳定；
  - 返回 `Task` / `Task<T>` / `IAsyncEnumerable<T>`；
  - 流式参数使用 `IAsyncEnumerable<T>`；
  - 带 `CallContext` 参数或非 `CancellationToken` 默认参数的方法会跳过；
  - `GrpcMethod` 必须与签名匹配。

## 生成命名

- 命名空间：`Nerosoft.Euonia.Endpoint.Generated`
- HTTP 映射类：`HttpMappings`
- gRPC 映射类：`GrpcMappings`
- gRPC 契约：`I{Service}GrpcContract{index}`
- gRPC 适配器：`{Service}GrpcAdapter{index}`
- gRPC 请求包装：`I{Service}GrpcContract{index}_{Method}Request`

## 诊断

| ID | 级别 | 含义 |
|---|---|---|
| `EUONIA001` | Warning | `GrpcMethod` 与实际流式签名不匹配，方法被跳过 |
| `EUONIA002` | Warning | 多个契约推导出相同 gRPC 服务名，重复契约被跳过 |

诊断不会中断构建，但对应方法/契约不会生成。

## 多目标与打包

项目目标框架：

```xml
<TargetFrameworks>netstandard2.0;net10.0</TargetFrameworks>
```

- `netstandard2.0`：真正打包进 NuGet 的 analyzer，兼容 Roslyn 编译器宿主；
- `net10.0`：仅用于 C# Dev Kit / 本地 IDE 体验；
- 打包时只把 `netstandard2.0` 的 DLL 放进 `analyzers/dotnet/cs`；
- `net10.0` 目标按条件抑制 `RS1041`，不会作为 analyzer 发布。

`dotnet pack` 后包内应包含：

```text
analyzers/dotnet/cs/Euonia.Endpoint.SourceGenerator.dll
```

## 调试生成代码

在消费项目中开启：

```xml
<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
```

文件会输出到：

```text
obj/Debug/net10.0/generated/Euonia.Endpoint.SourceGenerator/
```

建议把该目录加入 `.gitignore`，不要手工修改生成文件。

## 与运行时兜底的关系

生成器只处理当前编译中的接口。以下场景由 `Euonia.Endpoint.Http` / `Euonia.Endpoint.Grpc` 的运行时扫描兜底：

- 插件程序集或运行时加载的程序集；
- 生成器跳过的签名；
- 没有引用生成器的项目。

运行时兜底受 `ScanAssemblies` / `ScanNamespaces` / `ScanTypes` 限制，默认只扫描入口程序集。

## 限制

- 不扫描实现类方法；
- 不处理方法级 gRPC 授权；
- 共享契约类型尚未生成 wire-DTO 映射；
- 生成代码只覆盖当前编译，跨程序集接口需要在对应程序集也启用生成器。

## 相关文档

- [Euonia.Endpoint](../Euonia.Endpoint/README.md)
- [Euonia.Endpoint.Http](../Euonia.Endpoint.Http/README.md)
- [Euonia.Endpoint.Grpc](../Euonia.Endpoint.Grpc/README.md)
