# Euonia.Bus.Http 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Bus.Http |
| 公共类型 | 3 |
| 命名空间 | 3 |

## 命名空间 Microsoft.AspNetCore.Builder

### Microsoft.AspNetCore.Builder.BusEndpointExtensions

**类型**：static class

- static Microsoft.AspNetCore.Routing.IEndpointRouteBuilder MapBusEndpoint(Microsoft.AspNetCore.Routing.IEndpointRouteBuilder endpoints)

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddHttpBus(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String name = "http", Action<Nerosoft.Euonia.Bus.Http.HttpBusOptions> configureOptions = null)

## 命名空间 Nerosoft.Euonia.Bus.Http

### Nerosoft.Euonia.Bus.Http.HttpBusOptions

**类型**：class

- new Nerosoft.Euonia.Bus.Http.HttpBusOptions()
- System.Boolean Enabled { get; set; }
- System.String Endpoint { get; set; }
- IDictionary<System.String, System.String> Endpoints { get; }
- Func<System.Net.Http.HttpMessageHandler> MessageHandlerFactory { get; set; }
- System.String Name { get; set; }
- Nullable<System.TimeSpan> RequestTimeout { get; set; }
- System.Boolean RequireAuthorization { get; set; }
- System.String Route { get; set; }
- System.String SerializerProvider { get; set; }
- static const System.String LegacySectionName = HttpBus
- static const System.String SectionName = Euonia:Bus:Http
