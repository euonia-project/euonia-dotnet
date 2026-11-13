# Euonia.Bus.RabbitMq 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Bus.RabbitMq |
| 公共类型 | 4 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddRabbitMqBus(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String name, Action<Nerosoft.Euonia.Bus.RabbitMq.RabbitMqBusOptions> configureOptions = null)

## 命名空间 Nerosoft.Euonia.Bus.RabbitMq

### Nerosoft.Euonia.Bus.RabbitMq.IPersistentConnection

**类型**：interface
接口 System.IDisposable

- System.Boolean IsConnected { get; }
- Task<RabbitMQ.Client.IChannel> CreateChannelAsync()
- Task<RabbitMQ.Client.IChannel> CreateChannelAsync(RabbitMQ.Client.CreateChannelOptions options)
- System.Threading.Tasks.Task TryConnectAsync()

### Nerosoft.Euonia.Bus.RabbitMq.RabbitMqBusModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Bus.RabbitMq.RabbitMqBusModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Bus.RabbitMq.RabbitMqBusOptions

**类型**：class

- new Nerosoft.Euonia.Bus.RabbitMq.RabbitMqBusOptions()
- System.Boolean AutoAck { get; set; }
- System.String Connection { get; set; }
- System.Boolean Enabled { get; set; }
- System.String ExchangeNamePrefix { get; set; }
- System.Boolean IsDeadLetterEnabled { get; set; }
- System.Boolean Mandatory { get; set; }
- System.Int32 MaxFailureRetries { get; set; }
- System.Int32 MaxPriority { get; set; }
- System.String Name { get; set; }
- System.Boolean Persistent { get; set; }
- System.Int32 PrefetchCount { get; set; }
- System.Int32 PrefetchSize { get; set; }
- System.String QueueNamePrefix { get; set; }
- System.String RoutingKey { get; set; }
- System.String SerializerProvider { get; set; }
- System.String SubscriptionId { get; set; }
