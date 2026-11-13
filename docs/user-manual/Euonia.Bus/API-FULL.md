# Euonia.Bus 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Bus |
| 公共类型 | 62 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddConfiguratorBuilder(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Bus.IConfigurator> configure)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEuoniaBus(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddInMemoryDeadLetters(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddInMemoryInbox(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddInMemoryOutbox(Microsoft.Extensions.DependencyInjection.IServiceCollection services)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMessageHandler(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime, System.Reflection.Assembly[] assemblies)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMessageHandler<THandler>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMessageHandler(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime, System.Type[] handlerTypes)

## 命名空间 Nerosoft.Euonia.Bus

### Nerosoft.Euonia.Bus.AnnotationMessageConvention

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageConvention

- new Nerosoft.Euonia.Bus.AnnotationMessageConvention()
- System.String Name { get; }
- System.Boolean IsMulticast(System.String channel, System.Type type)
- System.Boolean IsRequest(System.String channel, System.Type type)
- System.Boolean IsUnicast(System.String channel, System.Type type)

### Nerosoft.Euonia.Bus.AnnotationTransportStrategy

**类型**：class
接口 Nerosoft.Euonia.Bus.ITransportStrategy

- new Nerosoft.Euonia.Bus.AnnotationTransportStrategy(IEnumerable<System.String> requiredTransports)
- System.String Name { get; }
- System.Boolean Incoming(System.String channel, System.Type type)
- System.Boolean Outgoing(System.String channel, System.Type type)

### Nerosoft.Euonia.Bus.AuditingMessageScopeParticipant

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageExecutionScopeParticipant

- new Nerosoft.Euonia.Bus.AuditingMessageScopeParticipant()
- System.Int32 Order { get; }
- ValueTask<Nerosoft.Euonia.Bus.MessageExecutionScopeEntry> EnterAsync(Nerosoft.Euonia.Bus.IMessageContext message, System.IServiceProvider services, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.BaseMessageConvention

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageConvention

- new Nerosoft.Euonia.Bus.BaseMessageConvention()
- System.String Name { get; }
- System.Boolean IsMulticast(System.String channel, System.Type type)
- System.Boolean IsRequest(System.String channel, System.Type type)
- System.Boolean IsUnicast(System.String channel, System.Type type)

### Nerosoft.Euonia.Bus.BaseTransportStrategy

**类型**：class
接口 Nerosoft.Euonia.Bus.ITransportStrategy

- new Nerosoft.Euonia.Bus.BaseTransportStrategy()
- System.String Name { get; }
- System.Boolean Incoming(System.String channel, System.Type type)
- System.Boolean Outgoing(System.String channel, System.Type type)

### Nerosoft.Euonia.Bus.BytesMessageDataConverter

**类型**：class
接口 IMessageDataConverter<System.Byte[]>

- new Nerosoft.Euonia.Bus.BytesMessageDataConverter()
- Task<System.Byte[]> ConvertAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)

### CallBuilder<TMessage, TResult>

**类型**：class
基类 DispatchBuilder<CallBuilder<TMessage, TResult>, Nerosoft.Euonia.Bus.CallOptions, TMessage, TResult>

- new CallBuilder<TMessage, TResult>(Nerosoft.Euonia.Bus.IBus bus, TMessage message, Nerosoft.Euonia.Bus.CallOptions options)
- Task<TResult> ExecuteAsync(System.Threading.CancellationToken cancellationToken = default)
- CallBuilder<TMessage, TResult> WithCorrelationId(System.String correlationId)

### Nerosoft.Euonia.Bus.CallOptions

**类型**：class
基类 Nerosoft.Euonia.Bus.ExtendableOptions

- new Nerosoft.Euonia.Bus.CallOptions()
- System.String CorrelationId { get; set; }

### Nerosoft.Euonia.Bus.ConfiguratorBuilder

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.ConfiguratorBuilder(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(Nerosoft.Euonia.Bus.IConfigurator configurator, System.AsyncCallback callback, System.Object object)
- void EndInvoke(System.IAsyncResult result)
- void Invoke(Nerosoft.Euonia.Bus.IConfigurator configurator)

### Nerosoft.Euonia.Bus.DefaultMessageConvention

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageConvention

- new Nerosoft.Euonia.Bus.DefaultMessageConvention()
- System.String Name { get; }
- System.Boolean IsMulticast(System.String channel, System.Type type)
- System.Boolean IsRequest(System.String channel, System.Type type)
- System.Boolean IsUnicast(System.String channel, System.Type type)

### Nerosoft.Euonia.Bus.DefaultMessageConventionBuilder

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageConventionBuilder

- new Nerosoft.Euonia.Bus.DefaultMessageConventionBuilder()
- Nerosoft.Euonia.Bus.IMessageConvention Convention { get; }
- Nerosoft.Euonia.Bus.IMessageConventionBuilder Add<TConvention>(TConvention convention)
- Nerosoft.Euonia.Bus.IMessageConventionBuilder Add<TConvention>()
- Nerosoft.Euonia.Bus.IMessageConventionBuilder Evaluate(Func<System.String, System.Type, Nerosoft.Euonia.Bus.MessageConventionType> convention)
- Nerosoft.Euonia.Bus.IMessageConventionBuilder EvaluateMulticast(Func<System.String, System.Type, System.Boolean> convention)
- Nerosoft.Euonia.Bus.IMessageConventionBuilder EvaluateRequest(Func<System.String, System.Type, System.Boolean> convention)
- Nerosoft.Euonia.Bus.IMessageConventionBuilder EvaluateUnicast(Func<System.String, System.Type, System.Boolean> convention)

### Nerosoft.Euonia.Bus.DefaultTransportStrategyBuilder

**类型**：class
接口 Nerosoft.Euonia.Bus.ITransportStrategyBuilder

- new Nerosoft.Euonia.Bus.DefaultTransportStrategyBuilder()
- Nerosoft.Euonia.Bus.ITransportStrategy Strategy { get; }
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder Add<TStrategy>(TStrategy strategy)
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder Add<TStrategy>(Func<TStrategy> strategyFactory)
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder Add<TStrategy>()
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder EvaluateIncoming(Func<System.String, System.Type, System.Boolean> strategy)
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder EvaluateOutgoing(Func<System.String, System.Type, System.Boolean> strategy)

### DispatchBuilder<TBuilder, TOptions, TMessage, TResult>

**类型**：class

- TBuilder WithChannel(System.String channel)
- TBuilder WithDelay(System.Int64 delay)
- TBuilder WithMessageId(System.String messageId)
- TBuilder WithMetadata(Action<Nerosoft.Euonia.Bus.MessageMetadata> metadataBuilder)
- TBuilder WithPipeline(Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> pipelineBuilder)
- TBuilder WithPriority(System.Int32 priority)
- TBuilder WithQueue(System.String queue)
- TBuilder WithTimeout(System.Int64 timeout)
- TBuilder WithTimeout(System.TimeSpan timeout)

### Nerosoft.Euonia.Bus.DistributedMessageTransportStrategy

**类型**：class
接口 Nerosoft.Euonia.Bus.ITransportStrategy

- new Nerosoft.Euonia.Bus.DistributedMessageTransportStrategy()
- System.String Name { get; }
- System.Boolean Incoming(System.String channel, System.Type type)
- System.Boolean Outgoing(System.String channel, System.Type type)

### Nerosoft.Euonia.Bus.ExecutionContextMessageScopeParticipant

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageExecutionScopeParticipant

- new Nerosoft.Euonia.Bus.ExecutionContextMessageScopeParticipant()
- System.Int32 Order { get; }
- ValueTask<Nerosoft.Euonia.Bus.MessageExecutionScopeEntry> EnterAsync(Nerosoft.Euonia.Bus.IMessageContext message, System.IServiceProvider services, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.ExtendableOptions

**类型**：class

- System.String Channel { get; set; }
- System.Int64 Delay { get; set; }
- System.String MessageId { get; set; }
- Action<Nerosoft.Euonia.Bus.MessageMetadata> MetadataSetter { get; set; }
- System.Int32 Priority { get; set; }
- System.String Queue { get; set; }
- System.String RequestTraceId { get; set; }
- System.Int64 Timeout { get; set; }
- Nullable<System.Boolean> UseInbox { get; set; }
- Nullable<System.Boolean> UseOutbox { get; set; }

### HandlerBase<TMessage>

**类型**：class
接口 IHandler<TMessage>、IHandler<TMessage, System.Unit>

- System.Boolean CanHandle(System.Type messageType)
- Task<System.Unit> HandleAsync(TMessage message, Nerosoft.Euonia.Bus.IMessageContext messageContext, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.HandlerDelegate

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.HandlerDelegate(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(System.Object message, Nerosoft.Euonia.Bus.IMessageContext context, System.Threading.CancellationToken cancellationToken, System.AsyncCallback callback, System.Object object)
- Task<System.Object> EndInvoke(System.IAsyncResult result)
- Task<System.Object> Invoke(System.Object message, Nerosoft.Euonia.Bus.IMessageContext context, System.Threading.CancellationToken cancellationToken = default)

### HandlerDelegate<TMessage>

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new HandlerDelegate<TMessage>(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(TMessage message, Nerosoft.Euonia.Bus.IMessageContext context, System.Threading.CancellationToken cancellationToken, System.AsyncCallback callback, System.Object object)
- System.Threading.Tasks.Task EndInvoke(System.IAsyncResult result)
- System.Threading.Tasks.Task Invoke(TMessage message, Nerosoft.Euonia.Bus.IMessageContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.HandlerFactory

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.HandlerFactory(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(System.IServiceProvider provider, System.AsyncCallback callback, System.Object object)
- Nerosoft.Euonia.Bus.HandlerDelegate EndInvoke(System.IAsyncResult result)
- Nerosoft.Euonia.Bus.HandlerDelegate Invoke(System.IServiceProvider provider)

### Nerosoft.Euonia.Bus.IBus

**类型**：interface

- CallBuilder<TMessage, TResult> Call<TMessage, TResult>(TMessage message)
- Task<TResult> CallAsync<TMessage, TResult>(TMessage message, System.Threading.CancellationToken cancellationToken = default)
- Task<TResult> CallAsync<TMessage, TResult>(TMessage message, Nerosoft.Euonia.Bus.CallOptions options, System.Threading.CancellationToken cancellationToken = default)
- Task<TResult> CallAsync<TMessage, TResult>(TMessage message, Nerosoft.Euonia.Bus.CallOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> behavior, System.Threading.CancellationToken cancellationToken = default)
- Task<TResult> CallAsync<TResult>(IRequest<TResult> request, System.Threading.CancellationToken cancellationToken = default)
- Task<TResult> CallAsync<TResult>(IRequest<TResult> request, Nerosoft.Euonia.Bus.CallOptions options, System.Threading.CancellationToken cancellationToken = default)
- Task<TResult> CallAsync<TResult>(IRequest<TResult> request, Nerosoft.Euonia.Bus.CallOptions options, Action<IPipeline<IMessageEnvelope<IRequest<TResult>>, TResult>> behavior, System.Threading.CancellationToken cancellationToken = default)
- Task<TResult> CallAsync<TResult>(Func<System.IServiceProvider, Task<TResult>> handler, System.Threading.CancellationToken cancellationToken = default)
- Task<TResult> CallAsync<TResult>(Func<Task<TResult>> handler, System.Threading.CancellationToken cancellationToken = default)
- Task<TResult> CallAsync<TService, TResult>(Func<TService, Task<TResult>> handler, System.Threading.CancellationToken cancellationToken = default)
- PublishBuilder<TMessage> Publish<TMessage>(TMessage message)
- System.Threading.Tasks.Task PublishAsync<TMessage>(TMessage message, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task PublishAsync<TMessage>(TMessage message, Nerosoft.Euonia.Bus.PublishOptions options, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task PublishAsync<TMessage>(TMessage message, Nerosoft.Euonia.Bus.PublishOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, System.Unit>> behavior, System.Threading.CancellationToken cancellationToken = default)
- SendBuilder<TMessage, TResult> Send<TMessage, TResult>(TMessage message)
- SendBuilder<TMessage, System.Unit> Send<TMessage>(TMessage message)
- System.Threading.Tasks.Task SendAsync<TMessage>(TMessage message, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SendAsync<TMessage>(TMessage message, Nerosoft.Euonia.Bus.SendOptions options, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SendAsync<TMessage>(TMessage message, Nerosoft.Euonia.Bus.SendOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, System.Unit>> behavior, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, Nerosoft.Euonia.Bus.SendOptions options, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SendAsync<TMessage, TResult>(TMessage message, Subject<TResult> callback, Nerosoft.Euonia.Bus.SendOptions options, Action<IPipeline<IMessageEnvelope<TMessage>, TResult>> behavior, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.IDispatcher

**类型**：interface

- IEnumerable<System.String> Determine(System.String channel, System.Type type)

### IHandler<TMessage>

**类型**：interface
接口 IHandler<TMessage, System.Unit>

- System.Threading.Tasks.Task HandleAsync(TMessage message, Nerosoft.Euonia.Bus.IMessageContext context, System.Threading.CancellationToken cancellationToken = default)

### IHandler<TMessage, TResult>

**类型**：interface

- Task<TResult> HandleAsync(TMessage message, Nerosoft.Euonia.Bus.IMessageContext context, System.Threading.CancellationToken cancellationToken = default)

### IMessageDataConverter<T>

**类型**：interface

- Task<T> ConvertAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Bus.IMessageExecutionScopeParticipant

**类型**：interface

- System.Int32 Order { get; }
- ValueTask<Nerosoft.Euonia.Bus.MessageExecutionScopeEntry> EnterAsync(Nerosoft.Euonia.Bus.IMessageContext message, System.IServiceProvider services, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.InboxOptions

**类型**：class

- new Nerosoft.Euonia.Bus.InboxOptions()
- System.Boolean Enabled { get; set; }
- System.Int32 MaxRetryAttempts { get; set; }
- System.TimeSpan PollingInterval { get; set; }
- System.TimeSpan RetentionPeriod { get; set; }

### Nerosoft.Euonia.Bus.InMemoryDeadLetterStore

**类型**：class
接口 Nerosoft.Euonia.Bus.IDeadLetterStore

- new Nerosoft.Euonia.Bus.InMemoryDeadLetterStore()
- System.Boolean Add(Nerosoft.Euonia.Bus.DeadLetterEntry entry)
- Nerosoft.Euonia.Bus.DeadLetterEntry Get(System.String messageId)
- IReadOnlyList<Nerosoft.Euonia.Bus.DeadLetterEntry> GetAll()
- Nerosoft.Euonia.Bus.DeadLetterEntry GetById(System.String id)
- System.Boolean Remove(System.String messageId)
- System.Boolean RemoveById(System.String id)
- void Update(Nerosoft.Euonia.Bus.DeadLetterEntry entry)

### Nerosoft.Euonia.Bus.InMemoryInboxStore

**类型**：class
接口 Nerosoft.Euonia.Bus.IInboxStore

- new Nerosoft.Euonia.Bus.InMemoryInboxStore()
- void Cleanup(System.DateTime cutoff)
- Nerosoft.Euonia.Bus.InboxEntry Get(System.String messageId)
- IReadOnlyList<Nerosoft.Euonia.Bus.InboxHandler> GetFailedMessages()
- System.Boolean Insert(Nerosoft.Euonia.Bus.InboxEntry entry)
- void MarkAsDeadLettered(System.String messageId, System.String handler, System.String errorMessage)
- void MarkAsFailed(System.String messageId, System.String handler, System.String errorMessage)
- void MarkAsSuccess(System.String messageId, System.String handler)

### Nerosoft.Euonia.Bus.InMemoryOutboxStore

**类型**：class
接口 Nerosoft.Euonia.Bus.IOutboxStore

- new Nerosoft.Euonia.Bus.InMemoryOutboxStore()
- void Cleanup(System.DateTime cutoff)
- Nerosoft.Euonia.Bus.OutboxEntry Get(System.String messageId)
- IReadOnlyList<Nerosoft.Euonia.Bus.OutboxTransport> GetFailedMessages()
- IReadOnlyList<Nerosoft.Euonia.Bus.OutboxTransport> GetPendingMessages(System.DateTime cutoff)
- System.Boolean Insert(Nerosoft.Euonia.Bus.OutboxEntry entry)
- void MarkAsDeadLettered(System.String messageId, System.String transport, System.String errorMessage)
- void MarkAsFailed(System.String messageId, System.String transport, System.String errorMessage)
- void MarkAsSuccess(System.String messageId, System.String transport)

### JsonMessageDataConverter<T>

**类型**：class
接口 IMessageDataConverter<T>

- new JsonMessageDataConverter<T>(Nerosoft.Euonia.Bus.IMessageSerializer serializer)
- Task<T> ConvertAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)

### LambdaHandler<TMessage>

**类型**：class

- new LambdaHandler<TMessage>(Func<TMessage, Nerosoft.Euonia.Bus.IMessageContext, System.Threading.Tasks.Task> handler)
- System.Threading.Tasks.Task HandleAsync(TMessage message, Nerosoft.Euonia.Bus.IMessageContext messageContext)

### LambdaHandler<TMessage, TResult>

**类型**：class

- new LambdaHandler<TMessage, TResult>(Func<TMessage, Nerosoft.Euonia.Bus.IMessageContext, Task<TResult>> handler)
- Task<TResult> HandleAsync(TMessage message, Nerosoft.Euonia.Bus.IMessageContext messageContext)

### Nerosoft.Euonia.Bus.LocalMessageTransportStrategy

**类型**：class
接口 Nerosoft.Euonia.Bus.ITransportStrategy

- new Nerosoft.Euonia.Bus.LocalMessageTransportStrategy()
- System.String Name { get; }
- System.Boolean Incoming(System.String channel, System.Type type)
- System.Boolean Outgoing(System.String channel, System.Type type)

### Nerosoft.Euonia.Bus.MessageBusModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Bus.MessageBusModule()
- void AheadConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)
- void OnApplicationInitialization(Nerosoft.Euonia.Modularity.ApplicationInitializationContext context)

### Nerosoft.Euonia.Bus.MessageBusOptions

**类型**：class

- new Nerosoft.Euonia.Bus.MessageBusOptions()
- System.String[] AutoLoadAssemblies { get; set; }
- System.String DefaultTransporter { get; set; }
- Nerosoft.Euonia.Bus.InboxOptions Inbox { get; set; }
- Nerosoft.Euonia.Bus.OutboxOptions Outbox { get; set; }

### Nerosoft.Euonia.Bus.MessageConvert

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.MessageConvert(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(System.Object source, System.Type targetType, System.AsyncCallback callback, System.Object object)
- System.Object EndInvoke(System.IAsyncResult result)
- System.Object Invoke(System.Object source, System.Type targetType)

### Nerosoft.Euonia.Bus.MessageDeadLetteredException

**类型**：class
基类 System.BusinessException；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.MessageDeadLetteredException(System.String messageId, System.String reason)

### Nerosoft.Euonia.Bus.MessageExecutionResult

**类型**：struct

- new Nerosoft.Euonia.Bus.MessageExecutionResult(Nerosoft.Euonia.Bus.MessageExecutionScopeAction action)
- Nerosoft.Euonia.Bus.MessageExecutionScopeAction Action { get; }
- static Nerosoft.Euonia.Bus.MessageExecutionResult Completed { get; }
- static Nerosoft.Euonia.Bus.MessageExecutionResult DeadLetter { get; }
- System.Boolean DeadLettered { get; }
- static Nerosoft.Euonia.Bus.MessageExecutionResult Skip { get; }
- System.Boolean Skipped { get; }

### Nerosoft.Euonia.Bus.MessageExecutionScopeAction

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Continue |
| Skip |
| DeadLetter |


### Nerosoft.Euonia.Bus.MessageExecutionScopeEntry

**类型**：struct

- new Nerosoft.Euonia.Bus.MessageExecutionScopeEntry(Func<System.IDisposable> install = null, Nerosoft.Euonia.Bus.MessageExecutionScopeAction action = 0)
- Nerosoft.Euonia.Bus.MessageExecutionScopeAction Action { get; }
- Func<System.IDisposable> Install { get; }

### MessageLoggingBehavior<TMessage, TResponse>

**类型**：class
接口 IPipelineBehavior<TMessage, TResponse>

- new MessageLoggingBehavior<TMessage, TResponse>(ILogger<MessageLoggingBehavior<TMessage, TResponse>> logger)
- Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)

### Nerosoft.Euonia.Bus.MessageSerializerOptions

**类型**：class

- new Nerosoft.Euonia.Bus.MessageSerializerOptions()
- System.Text.Encoding Encoding { get; set; }
- System.Boolean IgnoreNullValues { get; set; }
- Nullable<Nerosoft.Euonia.Bus.MessageSerializerOptions.ReferenceLoopStrategy> ReferenceLoop { get; set; }
- System.Boolean UseConstructorHandling { get; set; }

### Nerosoft.Euonia.Bus.NewtonsoftJsonSerializer

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageSerializer

- new Nerosoft.Euonia.Bus.NewtonsoftJsonSerializer(IOptions<Nerosoft.Euonia.Bus.MessageSerializerOptions> options)
- T Deserialize<T>(System.Byte[] source)
- T Deserialize<T>(System.String source)
- T Deserialize<T>(System.IO.Stream source)
- System.Object Deserialize(System.String source, System.Type type)
- Task<T> DeserializeAsync<T>(System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default)
- Task<T> DeserializeAsync<T>(System.Byte[] source, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Bus.IMessageEnvelope DeserializeEnvelope(System.String source, System.Type payloadType)
- System.String Serialize<T>(T source)
- Task<System.Byte[]> SerializeAsync<T>(T source, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.OutboxOptions

**类型**：class

- new Nerosoft.Euonia.Bus.OutboxOptions()
- System.Boolean Enabled { get; set; }
- System.Int32 MaxRetryAttempts { get; set; }
- System.TimeSpan PendingReclaimGracePeriod { get; set; }
- System.TimeSpan PollingInterval { get; set; }
- System.TimeSpan RetentionPeriod { get; set; }

### PublishBuilder<TMessage>

**类型**：class
基类 DispatchBuilder<PublishBuilder<TMessage>, Nerosoft.Euonia.Bus.PublishOptions, TMessage, System.Unit>

- new PublishBuilder<TMessage>(Nerosoft.Euonia.Bus.IBus bus, TMessage message, Nerosoft.Euonia.Bus.PublishOptions options)
- System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.PublishOptions

**类型**：class
基类 Nerosoft.Euonia.Bus.ExtendableOptions

- new Nerosoft.Euonia.Bus.PublishOptions()

### Nerosoft.Euonia.Bus.MessageSerializerOptions.ReferenceLoopStrategy

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Ignore |
| Preserve |
| Serialize |


### Nerosoft.Euonia.Bus.RemoteError

**类型**：class

- new Nerosoft.Euonia.Bus.RemoteError()
- System.String Message { get; set; }
- System.String StackTrace { get; set; }
- System.String Type { get; set; }
- static Nerosoft.Euonia.Bus.RemoteError Create(System.Exception exception)
- System.Exception ToException()

### Nerosoft.Euonia.Bus.RemoteReceiver

**类型**：static class

- static Task<System.String> ReceiveAsync(Nerosoft.Euonia.Bus.IMessageSerializer serializer, Nerosoft.Euonia.Bus.IHandlerContext handler, System.String payload, System.Threading.CancellationToken cancellationToken = default)
- static Task<System.String> ReceiveAsync(Nerosoft.Euonia.Bus.IMessageSerializer serializer, Nerosoft.Euonia.Bus.IHandlerContext handler, System.String payload, System.IServiceProvider requestServices, System.Threading.CancellationToken cancellationToken = default, System.String transport = "http")

### RemoteReply<TResult>

**类型**：class

- new RemoteReply<TResult>()
- new RemoteReply<TResult>(TResult result, System.Boolean isSuccess, Nerosoft.Euonia.Bus.RemoteError error)
- Nerosoft.Euonia.Bus.RemoteError Error { get; set; }
- System.Boolean IsSuccess { get; set; }
- TResult Result { get; set; }
- static RemoteReply<TResult> Failure(Nerosoft.Euonia.Bus.RemoteError error)
- static RemoteReply<TResult> Success(TResult result)

### Nerosoft.Euonia.Bus.RoutedMessage

**类型**：class

- System.String Authorization { get; set; }
- System.String Channel { get; set; }
- System.String ConversationId { get; set; }
- System.String CorrelationId { get; set; }
- System.String MessageId { get; set; }
- Nerosoft.Euonia.Bus.MessageMetadata Metadata { get; set; }
- System.String RequestTraceId { get; set; }
- System.Int64 Timestamp { get; set; }
- System.Security.Claims.ClaimsPrincipal User { get; set; }
- System.String GetTypeName()
- System.String ToString()

### RoutedMessage<T>

**类型**：class
基类 Nerosoft.Euonia.Bus.RoutedMessage；接口 IMessageEnvelope<T>、Nerosoft.Euonia.Bus.IMessageEnvelope

- new RoutedMessage<T>(T payload, System.String channel)
- new RoutedMessage<T>(T payload, System.String channel, System.String messageId, System.String correlationId, System.String conversationId)
- T Payload { get; set; }
- System.String TypeName { get; }

### RoutedMessage<TData, TResponse>

**类型**：class
基类 RoutedMessage<TData>；接口 IMessageEnvelope<TData>、Nerosoft.Euonia.Bus.IMessageEnvelope

- new RoutedMessage<TData, TResponse>(TData payload, System.String channel)
- new RoutedMessage<TData, TResponse>(TData payload, System.String channel, System.String messageId, System.String correlationId, System.String conversationId)

### SendBuilder<TMessage, TResult>

**类型**：class
基类 DispatchBuilder<SendBuilder<TMessage, TResult>, Nerosoft.Euonia.Bus.SendOptions, TMessage, TResult>

- new SendBuilder<TMessage, TResult>(Nerosoft.Euonia.Bus.IBus bus, TMessage message, Nerosoft.Euonia.Bus.SendOptions options)
- System.Threading.Tasks.Task ExecuteAsync(System.Threading.CancellationToken cancellationToken = default)
- Task<TResult> ExecuteWithResultAsync(System.Threading.CancellationToken cancellationToken = default)
- SendBuilder<TMessage, TResult> WithCallback(Subject<TResult> subject)
- SendBuilder<TMessage, TResult> WithCorrelationId(System.String correlationId)

### Nerosoft.Euonia.Bus.SendOptions

**类型**：class
基类 Nerosoft.Euonia.Bus.ExtendableOptions

- new Nerosoft.Euonia.Bus.SendOptions()
- System.String CorrelationId { get; set; }

### Nerosoft.Euonia.Bus.ServiceActivator

**类型**：class
基类 Microsoft.Extensions.Hosting.BackgroundService；接口 Microsoft.Extensions.Hosting.IHostedService、System.IDisposable

- new Nerosoft.Euonia.Bus.ServiceActivator(System.IServiceProvider provider, Nerosoft.Euonia.Bus.IConfigurator configurator, IOptions<Nerosoft.Euonia.Bus.MessageBusOptions> options)
- System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Bus.StreamMessageDataConverter

**类型**：class
接口 IMessageDataConverter<System.IO.Stream>

- new Nerosoft.Euonia.Bus.StreamMessageDataConverter()
- Task<System.IO.Stream> ConvertAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Bus.StringMessageDataConverter

**类型**：class
接口 IMessageDataConverter<System.String>

- new Nerosoft.Euonia.Bus.StringMessageDataConverter()
- Task<System.String> ConvertAsync(System.IO.Stream stream, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Bus.SystemTextJsonSerializer

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageSerializer

- new Nerosoft.Euonia.Bus.SystemTextJsonSerializer(IOptions<Nerosoft.Euonia.Bus.MessageSerializerOptions> options)
- T Deserialize<T>(System.Byte[] source)
- T Deserialize<T>(System.String source)
- T Deserialize<T>(System.IO.Stream stream)
- System.Object Deserialize(System.String source, System.Type type)
- Task<T> DeserializeAsync<T>(System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default)
- Task<T> DeserializeAsync<T>(System.Byte[] source, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Bus.IMessageEnvelope DeserializeEnvelope(System.String source, System.Type payloadType)
- System.String Serialize<T>(T source)
- Task<System.Byte[]> SerializeAsync<T>(T source, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.TenantMessageScopeParticipant

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageExecutionScopeParticipant

- new Nerosoft.Euonia.Bus.TenantMessageScopeParticipant()
- System.Int32 Order { get; }
- ValueTask<Nerosoft.Euonia.Bus.MessageExecutionScopeEntry> EnterAsync(Nerosoft.Euonia.Bus.IMessageContext message, System.IServiceProvider services, System.Threading.CancellationToken cancellationToken = default)
