# Euonia.Endpoint.Http 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Endpoint.Http |
| 公共类型 | 8 |
| 命名空间 | 3 |

## 命名空间 Microsoft.AspNetCore.Builder

### Microsoft.AspNetCore.Builder.EndpointRouteBuilderExtensions

**类型**：static class

- static Microsoft.AspNetCore.Routing.IEndpointRouteBuilder MapDynamicHttpEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDynamicHttpEndpoint(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.Configuration.IConfiguration configuration, System.Reflection.Assembly[] assemblies)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDynamicHttpEndpoint(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Endpoint.Http.HttpExposureOptions> configure, System.Reflection.Assembly[] assemblies)

## 命名空间 Nerosoft.Euonia.Endpoint.Http

### HttpBodyBindingResult<T>

**类型**：struct
接口 IEquatable<HttpBodyBindingResult<T>>

- new HttpBodyBindingResult<T>(System.Boolean Succeeded, T Value)
- System.Boolean Succeeded { get; set; }
- T Value { get; set; }

### Nerosoft.Euonia.Endpoint.Http.HttpExposureOptions

**类型**：class

- new Nerosoft.Euonia.Endpoint.Http.HttpExposureOptions()
- System.String DefaultRoutePrefix { get; set; }
- System.Boolean EnableExposeApplicationServiceAsWebApi { get; set; }
- System.Boolean RequireAuthorization { get; set; }
- System.String[] ScanAssemblies { get; set; }
- System.Boolean ScanEntryAssembly { get; set; }
- System.String[] ScanNamespaces { get; set; }
- System.String[] ScanTypes { get; set; }
- System.Boolean ThrowOnRouteConflict { get; set; }
- System.Boolean ThrowOnUnregisteredService { get; set; }
- static const System.String SectionName = Euonia:Endpoint:Http

### Nerosoft.Euonia.Endpoint.Http.HttpGeneratedMapper

**类型**：static class

- static Microsoft.AspNetCore.Builder.IEndpointConventionBuilder Map(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints, System.Type serviceType, System.String methodName, System.String prefix, System.String route, System.String[] methods, System.Type[] parameterTypes, System.Delegate handler)
- static Task<HttpBodyBindingResult<T>> ReadBodyAsync<T>(Microsoft.AspNetCore.Http.HttpContext context, System.Boolean required)

### Nerosoft.Euonia.Endpoint.Http.HttpGeneratedMappingAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Endpoint.Http.HttpGeneratedMappingAttribute(System.Type mappingType)
- System.Type MappingType { get; }

### Nerosoft.Euonia.Endpoint.Http.HttpRegistry

**类型**：class

- new Nerosoft.Euonia.Endpoint.Http.HttpRegistry()
- Nerosoft.Euonia.Endpoint.Http.HttpReservation TryReserve(System.String routePattern, IEnumerable<System.String> methods, System.Boolean generated)
- Nerosoft.Euonia.Endpoint.Http.HttpReservation TryReserve(System.String routePattern, IEnumerable<System.String> methods, System.Boolean generated, out IReadOnlyList<System.String> availableMethods)

### Nerosoft.Euonia.Endpoint.Http.HttpReservation

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Reserved |
| AlreadyGenerated |
| Conflict |
