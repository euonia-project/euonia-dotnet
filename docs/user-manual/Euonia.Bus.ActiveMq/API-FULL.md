# Euonia.Bus.ActiveMq 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Bus.ActiveMq |
| 公共类型 | 4 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Bus.ActiveMq

### Nerosoft.Euonia.Bus.ActiveMq.ActiveMqBusModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Bus.ActiveMq.ActiveMqBusModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Bus.ActiveMq.ActiveMqBusOptions

**类型**：class

- new Nerosoft.Euonia.Bus.ActiveMq.ActiveMqBusOptions()
- System.String Connection { get; set; }
- System.Boolean Enabled { get; set; }
- System.Int32 MaxFailureRetries { get; set; }
- System.String Name { get; set; }
- System.String SerializerProvider { get; set; }
- System.String SubscriptionId { get; set; }

### Nerosoft.Euonia.Bus.ActiveMq.IPersistentConnection

**类型**：interface
接口 System.IDisposable

- System.Boolean IsConnected { get; }
- event System.EventHandler Reconnected
- Task<Apache.NMS.ISession> CreateSessionAsync()
- System.Threading.Tasks.Task TryConnectAsync()

### Nerosoft.Euonia.Bus.ActiveMq.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddActiveMqBus(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String name, Action<Nerosoft.Euonia.Bus.ActiveMq.ActiveMqBusOptions> configureOptions = null)
