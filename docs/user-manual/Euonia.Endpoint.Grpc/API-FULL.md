# Euonia.Endpoint.Grpc 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Endpoint.Grpc |
| 公共类型 | 9 |
| 命名空间 | 3 |

## 命名空间 Microsoft.AspNetCore.Builder

### Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions

**类型**：static class

- static Microsoft.AspNetCore.Routing.IEndpointRouteBuilder MapDynamicGrpcEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDynamicGrpcEndpoint(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration, System.Reflection.Assembly[] assemblies)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDynamicGrpcEndpoint(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Endpoint.Grpc.GrpcExposureOptions> configure, System.Reflection.Assembly[] assemblies)

## 命名空间 Nerosoft.Euonia.Endpoint.Grpc

### Nerosoft.Euonia.Endpoint.Grpc.EndpointProtoExporter

**类型**：static class

- static IReadOnlyList<System.String> Export(System.Reflection.Assembly assembly, System.String outputDirectory)

### Nerosoft.Euonia.Endpoint.Grpc.EndpointProtoModel

**类型**：static class

- static void Configure()

### Nerosoft.Euonia.Endpoint.Grpc.GrpcExposureOptions

**类型**：class

- new Nerosoft.Euonia.Endpoint.Grpc.GrpcExposureOptions()
- System.Boolean EnableExposeApplicationServiceAsGrpc { get; set; }
- System.Boolean EnableReflection { get; set; }
- System.Boolean RequireAuthorization { get; set; }
- System.String[] ScanAssemblies { get; set; }
- System.Boolean ScanEntryAssembly { get; set; }
- System.Boolean ThrowOnUnregisteredService { get; set; }
- static const System.String SectionName = Euonia:Endpoint:Grpc

### Nerosoft.Euonia.Endpoint.Grpc.GrpcGeneratedMappingAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Endpoint.Grpc.GrpcGeneratedMappingAttribute(System.Type mappingType)
- System.Type MappingType { get; }

### Nerosoft.Euonia.Endpoint.Grpc.GrpcMetadataRegistry

**类型**：class

- new Nerosoft.Euonia.Endpoint.Grpc.GrpcMetadataRegistry()
- void Build(IEnumerable<System.Reflection.Assembly> assemblies)
- IReadOnlyList<System.Object> GetMethodMetadata(System.Type serviceType, System.String methodName)
- IReadOnlyList<System.Object> GetServiceMetadata(System.Type serviceType)

### Nerosoft.Euonia.Endpoint.Grpc.GrpcMethodAuthorizationDescriptor

**类型**：class

- new Nerosoft.Euonia.Endpoint.Grpc.GrpcMethodAuthorizationDescriptor(System.String methodPath, System.Type serviceType, System.String methodName)
- System.String MethodName { get; }
- System.String MethodPath { get; }
- System.Type ServiceType { get; }

### Nerosoft.Euonia.Endpoint.Grpc.GrpcMethodAuthorizationInterceptor

**类型**：class
基类 Grpc.Core.Interceptors.Interceptor

- new Nerosoft.Euonia.Endpoint.Grpc.GrpcMethodAuthorizationInterceptor(IEnumerable<Nerosoft.Euonia.Endpoint.Grpc.GrpcMethodAuthorizationDescriptor> descriptors, Nerosoft.Euonia.Endpoint.Grpc.GrpcMetadataRegistry metadataRegistry, IOptions<Nerosoft.Euonia.Endpoint.Grpc.GrpcExposureOptions> options)
- Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, Grpc.Core.ServerCallContext context, ClientStreamingServerMethod<TRequest, TResponse> continuation)
- System.Threading.Tasks.Task DuplexStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, Grpc.Core.ServerCallContext context, DuplexStreamingServerMethod<TRequest, TResponse> continuation)
- System.Threading.Tasks.Task ServerStreamingServerHandler<TRequest, TResponse>(TRequest request, IServerStreamWriter<TResponse> responseStream, Grpc.Core.ServerCallContext context, ServerStreamingServerMethod<TRequest, TResponse> continuation)
- Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, Grpc.Core.ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
