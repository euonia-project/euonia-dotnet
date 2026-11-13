# Euonia.Bus.InMemory 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Bus.InMemory |
| 公共类型 | 23 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddInMemoryBus(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String name, Action<Nerosoft.Euonia.Bus.InMemory.InMemoryBusOptions> configureOptions = null)

## 命名空间 Nerosoft.Euonia.Bus.InMemory

### AsyncCollectionRequestMessage<T>

**类型**：class
接口 IAsyncEnumerable<T>

- new AsyncCollectionRequestMessage<T>()
- System.Threading.CancellationToken CancellationToken { get; }
- IAsyncEnumerator<T> GetAsyncEnumerator(System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyCollection<T>> GetResponsesAsync(System.Threading.CancellationToken cancellationToken = default)
- void Reply(T response)
- void Reply(Task<T> response)
- void Reply(Func<System.Threading.CancellationToken, Task<T>> response)

### AsyncRequestMessage<T>

**类型**：class

- new AsyncRequestMessage<T>()
- System.Boolean HasReceivedResponse { get; set; }
- Task<T> Response { get; }
- TaskAwaiter<T> GetAwaiter()
- void Reply(T response)
- void Reply(Task<T> response)

### CollectionRequestMessage<T>

**类型**：class
接口 IEnumerable<T>、System.Collections.IEnumerable

- new CollectionRequestMessage<T>()
- IReadOnlyCollection<T> Responses { get; }
- IEnumerator<T> GetEnumerator()
- void Reply(T response)

### Nerosoft.Euonia.Bus.InMemory.IMessenger

**类型**：interface

- void Cleanup()
- System.Boolean IsRegistered<TMessage, TToken>(System.Object recipient, TToken token)
- void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler)
- void Reset()
- TMessage Send<TMessage, TToken>(TMessage message, TToken token)
- void Unregister<TMessage, TToken>(System.Object recipient, TToken token)
- void UnregisterAll(System.Object recipient)
- void UnregisterAll<TToken>(System.Object recipient, TToken token)

### Nerosoft.Euonia.Bus.InMemory.InMemoryBusModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Bus.InMemory.InMemoryBusModule()
- void AheadConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Bus.InMemory.InMemoryBusOptions

**类型**：class

- new Nerosoft.Euonia.Bus.InMemory.InMemoryBusOptions()
- System.Boolean LazyInitialize { get; set; }
- System.Int32 MaxConcurrentCalls { get; set; }
- System.Boolean MultipleSubscriberInstance { get; set; }
- System.String Name { get; set; }

### Nerosoft.Euonia.Bus.InMemory.InMemoryConsumer

**类型**：class
基类 InMemoryRecipient<Nerosoft.Euonia.Bus.InMemory.InMemoryConsumer>；接口 System.IDisposable、IRecipient<Nerosoft.Euonia.Bus.InMemory.MessagePack>、Nerosoft.Euonia.Bus.IConsumer、Nerosoft.Euonia.Bus.IRecipient

- new Nerosoft.Euonia.Bus.InMemory.InMemoryConsumer(Nerosoft.Euonia.Bus.IHandlerContext handler, Microsoft.Extensions.Logging.ILoggerFactory logger)
- System.String Name { get; }

### Nerosoft.Euonia.Bus.InMemory.InMemoryExecutor

**类型**：class
基类 InMemoryRecipient<Nerosoft.Euonia.Bus.InMemory.InMemoryExecutor>；接口 System.IDisposable、IRecipient<Nerosoft.Euonia.Bus.InMemory.MessagePack>、Nerosoft.Euonia.Bus.IExecutor、Nerosoft.Euonia.Bus.IRecipient

- new Nerosoft.Euonia.Bus.InMemory.InMemoryExecutor(Nerosoft.Euonia.Bus.IHandlerContext handler, Microsoft.Extensions.Logging.ILoggerFactory factory)
- System.String Name { get; }

### Nerosoft.Euonia.Bus.InMemory.InMemoryMessengerProvider

**类型**：class

- new Nerosoft.Euonia.Bus.InMemory.InMemoryMessengerProvider()
- Nerosoft.Euonia.Bus.InMemory.StrongReferenceMessenger Strong { get; }
- Nerosoft.Euonia.Bus.InMemory.WeakReferenceMessenger Weak { get; }

### InMemoryRecipient<TRecipient>

**类型**：class
基类 System.DisposableObject；接口 System.IDisposable、IRecipient<Nerosoft.Euonia.Bus.InMemory.MessagePack>

- System.String Name { get; }
- event EventHandler<Nerosoft.Euonia.Bus.MessageAcknowledgedEventArgs> MessageAcknowledged
- event EventHandler<Nerosoft.Euonia.Bus.MessageReceivedEventArgs> MessageReceived
- void Receive(Nerosoft.Euonia.Bus.InMemory.MessagePack pack)

### Nerosoft.Euonia.Bus.InMemory.InMemoryRecipientRegistrar

**类型**：class
接口 Nerosoft.Euonia.Bus.IRecipientRegistrar、System.IDisposable、System.IAsyncDisposable

- new Nerosoft.Euonia.Bus.InMemory.InMemoryRecipientRegistrar(Nerosoft.Euonia.Bus.IConfigurator configurator, System.IServiceProvider provider, IOptions<Nerosoft.Euonia.Bus.InMemory.InMemoryBusOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, Nerosoft.Euonia.Bus.InMemory.InMemoryMessengerProvider messengers)
- void Dispose()
- System.Threading.Tasks.Task RegisterAsync(IDictionary<System.String, Nerosoft.Euonia.Bus.ChannelRegistration> registrations, System.String defaultTransporter, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.InMemory.InMemorySubscriber

**类型**：class
基类 InMemoryRecipient<Nerosoft.Euonia.Bus.InMemory.InMemorySubscriber>；接口 System.IDisposable、IRecipient<Nerosoft.Euonia.Bus.InMemory.MessagePack>、Nerosoft.Euonia.Bus.ISubscriber、Nerosoft.Euonia.Bus.IRecipient

- new Nerosoft.Euonia.Bus.InMemory.InMemorySubscriber(Nerosoft.Euonia.Bus.IHandlerContext handler, Microsoft.Extensions.Logging.ILoggerFactory logger)
- System.String Name { get; }

### Nerosoft.Euonia.Bus.InMemory.InMemoryTransporter

**类型**：class
基类 System.DisposableObject；接口 System.IDisposable、Nerosoft.Euonia.Bus.ITransporter

- new Nerosoft.Euonia.Bus.InMemory.InMemoryTransporter(IOptions<Nerosoft.Euonia.Bus.InMemory.InMemoryBusOptions> options, Microsoft.Extensions.Logging.ILoggerFactory logger, Nerosoft.Euonia.Bus.InMemory.InMemoryMessengerProvider messengers = null, System.IServiceAccessor accessor = null, Nerosoft.Euonia.Modularity.IRequestContextAccessor requestContextAccessor = null)
- System.String Name { get; }
- event EventHandler<Nerosoft.Euonia.Bus.MessageDeliveredEventArgs> Delivered
- Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SendAsync<TMessage>(IMessageEnvelope<TMessage> message, System.Threading.CancellationToken cancellationToken = default)
- Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, System.Threading.CancellationToken cancellationToken = default)

### IRecipient<TMessage>

**类型**：interface

- void Receive(TMessage pack)

### MessageHandler<TRecipient, TMessage>

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new MessageHandler<TRecipient, TMessage>(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(TRecipient recipient, TMessage message, System.AsyncCallback callback, System.Object object)
- void EndInvoke(System.IAsyncResult result)
- void Invoke(TRecipient recipient, TMessage message)

### Nerosoft.Euonia.Bus.InMemory.MessagePack

**类型**：class

- new Nerosoft.Euonia.Bus.InMemory.MessagePack(Nerosoft.Euonia.Bus.IMessageEnvelope message, Nerosoft.Euonia.Bus.MessageContext context)
- System.Threading.CancellationToken Aborted { get; set; }
- Nerosoft.Euonia.Bus.MessageContext Context { get; }
- Nerosoft.Euonia.Bus.IMessageEnvelope Message { get; }

### Nerosoft.Euonia.Bus.InMemory.MessengerExtensions

**类型**：static class

- static IObservable<TMessage> CreateObservable<TMessage>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger)
- static IObservable<TMessage> CreateObservable<TMessage, TToken>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, TToken token)
- static System.Boolean IsRegistered<TMessage>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, System.Object recipient)
- static void Register<TMessage>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, IRecipient<TMessage> recipient)
- static void Register<TMessage, TToken>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, IRecipient<TMessage> recipient, TToken token)
- static void Register<TMessage>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, System.Object recipient, MessageHandler<System.Object, TMessage> handler)
- static void Register<TRecipient, TMessage>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, TRecipient recipient, MessageHandler<TRecipient, TMessage> handler)
- static void Register<TMessage, TToken>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, System.Object recipient, TToken token, MessageHandler<System.Object, TMessage> handler)
- static TMessage Send<TMessage>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger)
- static TMessage Send<TMessage>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, TMessage message)
- static TMessage Send<TMessage, TToken>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, TToken token)
- static void Unregister<TMessage>(Nerosoft.Euonia.Bus.InMemory.IMessenger messenger, System.Object recipient)

### Nerosoft.Euonia.Bus.InMemory.MessengerReferenceType

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| StrongReference |
| WeakReference |


### RequestMessage<T>

**类型**：class

- new RequestMessage<T>()
- System.Boolean HasReceivedResponse { get; set; }
- T Response { get; }
- static T operator Implicit(RequestMessage<T> message)
- void Reply(T response)

### Nerosoft.Euonia.Bus.InMemory.StrongReferenceMessenger

**类型**：class
接口 Nerosoft.Euonia.Bus.InMemory.IMessenger

- new Nerosoft.Euonia.Bus.InMemory.StrongReferenceMessenger()
- static Nerosoft.Euonia.Bus.InMemory.StrongReferenceMessenger Default { get; }
- System.Boolean IsRegistered<TMessage, TToken>(System.Object recipient, TToken token)
- void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler)
- void Reset()
- TMessage Send<TMessage, TToken>(TMessage message, TToken token)
- void Unregister<TMessage, TToken>(System.Object recipient, TToken token)
- void UnregisterAll(System.Object recipient)
- void UnregisterAll<TToken>(System.Object recipient, TToken token)

### ValueChangedMessage<T>

**类型**：class

- new ValueChangedMessage<T>(T value)
- T Value { get; }

### Nerosoft.Euonia.Bus.InMemory.WeakReferenceMessenger

**类型**：class
接口 Nerosoft.Euonia.Bus.InMemory.IMessenger

- new Nerosoft.Euonia.Bus.InMemory.WeakReferenceMessenger()
- static Nerosoft.Euonia.Bus.InMemory.WeakReferenceMessenger Default { get; }
- void Cleanup()
- System.Boolean IsRegistered<TMessage, TToken>(System.Object recipient, TToken token)
- void Register<TRecipient, TMessage, TToken>(TRecipient recipient, TToken token, MessageHandler<TRecipient, TMessage> handler)
- void Reset()
- TMessage Send<TMessage, TToken>(TMessage message, TToken token)
- void Unregister<TMessage, TToken>(System.Object recipient, TToken token)
- void UnregisterAll(System.Object recipient)
- void UnregisterAll<TToken>(System.Object recipient, TToken token)
