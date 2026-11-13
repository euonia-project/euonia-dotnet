# Euonia.Bus.Grpc 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Bus.Grpc |
| 公共类型 | 21 |
| 命名空间 | 6 |

## 命名空间 Google.Protobuf

### Google.Protobuf.DecimalReflection

**类型**：static class

- static Google.Protobuf.Reflection.FileDescriptor Descriptor { get; }

### Google.Protobuf.DecimalValue

**类型**：class
接口 IMessage<Google.Protobuf.DecimalValue>、Google.Protobuf.IMessage、IEquatable<Google.Protobuf.DecimalValue>、IDeepCloneable<Google.Protobuf.DecimalValue>、Google.Protobuf.IBufferMessage

- new Google.Protobuf.DecimalValue(System.Int64 units, System.Int32 nanos)
- new Google.Protobuf.DecimalValue()
- new Google.Protobuf.DecimalValue(Google.Protobuf.DecimalValue other)
- static Google.Protobuf.Reflection.MessageDescriptor Descriptor { get; }
- System.Int32 Nanos { get; set; }
- static MessageParser<Google.Protobuf.DecimalValue> Parser { get; }
- System.Int64 Units { get; set; }
- static const System.Int32 NanosFieldNumber = 2
- static const System.Int32 UnitsFieldNumber = 1
- System.Int32 CalculateSize()
- Google.Protobuf.DecimalValue Clone()
- System.Boolean Equals(System.Object other)
- System.Boolean Equals(Google.Protobuf.DecimalValue other)
- System.Int32 GetHashCode()
- void MergeFrom(Google.Protobuf.DecimalValue other)
- void MergeFrom(Google.Protobuf.CodedInputStream input)
- static System.Decimal operator Implicit(Google.Protobuf.DecimalValue decimalValue)
- static Nullable<System.Decimal> operator Implicit(Google.Protobuf.DecimalValue decimalValue)
- static Google.Protobuf.DecimalValue operator Implicit(System.Decimal value)
- static Google.Protobuf.DecimalValue operator Implicit(Nullable<System.Decimal> value)
- System.String ToString()
- void WriteTo(Google.Protobuf.CodedOutputStream output)

### Google.Protobuf.GrpcExtensions

**类型**：static class

- static TValue GetData<TValue>(Google.Protobuf.GrpcRequest request)
- static TValue GetProperty<TValue>(Google.Protobuf.GrpcRequest request, System.String key)
- static TValue GetResult<TValue>(Google.Protobuf.GrpcResponse response)
- static Google.Protobuf.GrpcRequest SetData<TValue>(Google.Protobuf.GrpcRequest request, TValue value)
- static Google.Protobuf.GrpcRequest SetProperty<TValue>(Google.Protobuf.GrpcRequest request, System.String key, TValue value)
- static Google.Protobuf.GrpcResponse SetResult<TValue>(Google.Protobuf.GrpcResponse response, TValue value)

### Google.Protobuf.GrpcRequest

**类型**：class
接口 IMessage<Google.Protobuf.GrpcRequest>、Google.Protobuf.IMessage、IEquatable<Google.Protobuf.GrpcRequest>、IDeepCloneable<Google.Protobuf.GrpcRequest>、Google.Protobuf.IBufferMessage

- new Google.Protobuf.GrpcRequest(System.Object data)
- new Google.Protobuf.GrpcRequest()
- new Google.Protobuf.GrpcRequest(Google.Protobuf.GrpcRequest other)
- System.String Data { get; set; }
- static Google.Protobuf.Reflection.MessageDescriptor Descriptor { get; }
- System.Int32 Page { get; set; }
- static MessageParser<Google.Protobuf.GrpcRequest> Parser { get; }
- MapField<System.String, System.String> Property { get; }
- System.String RequestId { get; set; }
- System.Int32 Size { get; set; }
- static const System.Int32 DataFieldNumber = 2
- static const System.Int32 PageFieldNumber = 4
- static const System.Int32 PropertyFieldNumber = 3
- static const System.Int32 RequestIdFieldNumber = 1
- static const System.Int32 SizeFieldNumber = 5
- System.Int32 CalculateSize()
- Google.Protobuf.GrpcRequest Clone()
- System.Boolean Equals(System.Object other)
- System.Boolean Equals(Google.Protobuf.GrpcRequest other)
- System.Int32 GetHashCode()
- void MergeFrom(Google.Protobuf.GrpcRequest other)
- void MergeFrom(Google.Protobuf.CodedInputStream input)
- System.String ToString()
- void WriteTo(Google.Protobuf.CodedOutputStream output)

### Google.Protobuf.GrpcResponse

**类型**：class
接口 IMessage<Google.Protobuf.GrpcResponse>、Google.Protobuf.IMessage、IEquatable<Google.Protobuf.GrpcResponse>、IDeepCloneable<Google.Protobuf.GrpcResponse>、Google.Protobuf.IBufferMessage

- new Google.Protobuf.GrpcResponse(System.Object value)
- new Google.Protobuf.GrpcResponse()
- new Google.Protobuf.GrpcResponse(Google.Protobuf.GrpcResponse other)
- System.String Data { get; set; }
- static Google.Protobuf.Reflection.MessageDescriptor Descriptor { get; }
- static MessageParser<Google.Protobuf.GrpcResponse> Parser { get; }
- MapField<System.String, System.String> Property { get; }
- System.String RequestId { get; set; }
- static const System.Int32 DataFieldNumber = 2
- static const System.Int32 PropertyFieldNumber = 3
- static const System.Int32 RequestIdFieldNumber = 1
- System.Int32 CalculateSize()
- Google.Protobuf.GrpcResponse Clone()
- System.Boolean Equals(System.Object other)
- System.Boolean Equals(Google.Protobuf.GrpcResponse other)
- System.Int32 GetHashCode()
- void MergeFrom(Google.Protobuf.GrpcResponse other)
- void MergeFrom(Google.Protobuf.CodedInputStream input)
- System.String ToString()
- void WriteTo(Google.Protobuf.CodedOutputStream output)

### Google.Protobuf.JsonReflection

**类型**：static class

- static Google.Protobuf.Reflection.FileDescriptor Descriptor { get; }

### Google.Protobuf.JsonValue

**类型**：class
接口 IMessage<Google.Protobuf.JsonValue>、Google.Protobuf.IMessage、IEquatable<Google.Protobuf.JsonValue>、IDeepCloneable<Google.Protobuf.JsonValue>、Google.Protobuf.IBufferMessage

- new Google.Protobuf.JsonValue()
- new Google.Protobuf.JsonValue(Google.Protobuf.JsonValue other)
- static Google.Protobuf.Reflection.MessageDescriptor Descriptor { get; }
- static MessageParser<Google.Protobuf.JsonValue> Parser { get; }
- System.String Value { get; set; }
- static const System.Int32 ValueFieldNumber = 1
- System.Int32 CalculateSize()
- Google.Protobuf.JsonValue Clone()
- System.Boolean Equals(System.Object other)
- System.Boolean Equals(Google.Protobuf.JsonValue other)
- System.Int32 GetHashCode()
- void MergeFrom(Google.Protobuf.JsonValue other)
- void MergeFrom(Google.Protobuf.CodedInputStream input)
- System.String ToString()
- void WriteTo(Google.Protobuf.CodedOutputStream output)

### Google.Protobuf.RequestReflection

**类型**：static class

- static Google.Protobuf.Reflection.FileDescriptor Descriptor { get; }

### Google.Protobuf.ResponseReflection

**类型**：static class

- static Google.Protobuf.Reflection.FileDescriptor Descriptor { get; }

## 命名空间 Microsoft.AspNetCore.Builder

### Microsoft.AspNetCore.Builder.ApplicationBuilderExtensions

**类型**：static class

- static void MapGrpcServices(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder, System.Boolean useHealthCheck = true)
- static Microsoft.AspNetCore.Builder.IApplicationBuilder UseGrpcEndpoints(Microsoft.AspNetCore.Builder.IApplicationBuilder builder, Action<Microsoft.AspNetCore.Routing.IEndpointRouteBuilder> configure = null)
- static void UseGrpcHealthCheck(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder builder)

### Microsoft.AspNetCore.Builder.GrpcEndpointExtensions

**类型**：static class

- static Microsoft.AspNetCore.Routing.IEndpointRouteBuilder MapGrpcBusService(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.GrpcServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGrpcService(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Grpc.AspNetCore.Server.GrpcServiceOptions> configureOptions = null)

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGrpcBus(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String name = "grpc", Action<Nerosoft.Euonia.Bus.Grpc.GrpcBusOptions> configureOptions = null)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddGrpcBusServer(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

## 命名空间 Nerorsoft.Bus

### Nerorsoft.Bus.MessageReflection

**类型**：static class

- static Google.Protobuf.Reflection.FileDescriptor Descriptor { get; }

### Nerorsoft.Bus.ReplierService

**类型**：static class

- static Google.Protobuf.Reflection.ServiceDescriptor Descriptor { get; }
- static Grpc.Core.ServerServiceDefinition BindService(Nerorsoft.Bus.ReplierService.ReplierServiceBase serviceImpl)
- static void BindService(Grpc.Core.ServiceBinderBase serviceBinder, Nerorsoft.Bus.ReplierService.ReplierServiceBase serviceImpl)

### Nerorsoft.Bus.ReplierService.ReplierServiceBase

**类型**：class

- Task<Google.Protobuf.GrpcResponse> Call(Google.Protobuf.GrpcRequest request, Grpc.Core.ServerCallContext context)

## 命名空间 Nerosoft.Euonia.Bus.Grpc

### Nerosoft.Euonia.Bus.Grpc.GrpcBusOptions

**类型**：class

- new Nerosoft.Euonia.Bus.Grpc.GrpcBusOptions()
- System.Boolean Enabled { get; set; }
- System.String Endpoint { get; set; }
- IDictionary<System.String, System.String> Endpoints { get; }
- System.String MethodName { get; set; }
- System.String Name { get; set; }
- System.Boolean RequireAuthorization { get; set; }
- System.String SerializerProvider { get; set; }
- System.String ServiceName { get; set; }
- static const System.String SectionName = Euonia:Bus:Grpc

### Nerosoft.Euonia.Bus.Grpc.RemoteMessageService

**类型**：class
基类 Nerorsoft.Bus.ReplierService.ReplierServiceBase

- new Nerosoft.Euonia.Bus.Grpc.RemoteMessageService(Nerosoft.Euonia.Bus.IHandlerContext handler, System.IServiceProvider provider, IOptions<Nerosoft.Euonia.Bus.Grpc.GrpcBusOptions> options, ILogger<Nerosoft.Euonia.Bus.Grpc.RemoteMessageService> logger)
- Task<Google.Protobuf.GrpcResponse> Call(Google.Protobuf.GrpcRequest request, Grpc.Core.ServerCallContext context)

## 命名空间 Nerosoft.Euonia.Grpc

### Nerosoft.Euonia.Grpc.HealthService

**类型**：class
基类 Grpc.HealthCheck.HealthServiceImpl

- new Nerosoft.Euonia.Grpc.HealthService()

### Nerosoft.Euonia.Grpc.IExceptionHandler

**类型**：interface

- Grpc.Core.RpcException Handle(System.Exception exception)

### Nerosoft.Euonia.Grpc.RequestTraceInterceptor

**类型**：class
基类 Grpc.Core.Interceptors.Interceptor

- new Nerosoft.Euonia.Grpc.RequestTraceInterceptor(Microsoft.Extensions.Logging.ILoggerFactory logger)
- Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, Grpc.Core.ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
