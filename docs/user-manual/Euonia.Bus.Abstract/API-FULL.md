# Euonia.Bus.Abstract 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Bus.Abstract |
| 公共类型 | 74 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Bus

### Nerosoft.Euonia.Bus.ChannelAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Bus.ChannelAttribute(System.String name)
- System.String Name { get; }

### Nerosoft.Euonia.Bus.ChannelHandler

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Bus.ChannelHandler>

- new Nerosoft.Euonia.Bus.ChannelHandler(System.Type HandlerType, System.Reflection.MethodInfo Method, System.Object Instance = null, System.Type ImplementationType = null)
- System.Type HandlerType { get; set; }
- System.Type ImplementationType { get; set; }
- System.Object Instance { get; set; }
- System.Reflection.MethodInfo Method { get; set; }

### Nerosoft.Euonia.Bus.ChannelNotRegisterException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.ChannelNotRegisterException(System.String channel)
- new Nerosoft.Euonia.Bus.ChannelNotRegisterException(System.String channel, System.String message)
- System.String Channel { get; }

### Nerosoft.Euonia.Bus.ChannelRegisteredEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Bus.ChannelRegisteredEventArgs(System.String channel, System.Type type, Nerosoft.Euonia.Bus.ChannelHandler handler)
- System.String Channel { get; }
- Nerosoft.Euonia.Bus.ChannelHandler Handler { get; }
- System.Type Type { get; }

### Nerosoft.Euonia.Bus.ChannelRegistration

**类型**：class

- new Nerosoft.Euonia.Bus.ChannelRegistration(System.Type messageType)
- IList<Nerosoft.Euonia.Bus.ChannelHandler> Handlers { get; }
- System.Type MessageType { get; }
- Nerosoft.Euonia.Bus.ChannelRegistration AddHandler(Nerosoft.Euonia.Bus.ChannelHandler handler)
- Nerosoft.Euonia.Bus.ChannelRegistration AddHandler(System.Type handlerType, System.String methodName)
- Nerosoft.Euonia.Bus.ChannelRegistration AddHandler(System.Type handlerType, System.Reflection.MethodInfo method)

### Nerosoft.Euonia.Bus.DeadLetterEntry

**类型**：class

- new Nerosoft.Euonia.Bus.DeadLetterEntry()
- System.String Channel { get; set; }
- Nerosoft.Euonia.Bus.IMessageEnvelope Content { get; set; }
- System.DateTime DeadLetteredAt { get; set; }
- System.String Error { get; set; }
- System.String Id { get; }
- System.String MessageId { get; set; }
- System.String MessageType { get; set; }
- System.Int32 RetryAttempts { get; set; }
- Nerosoft.Euonia.Bus.DeadLetterSource Source { get; set; }
- System.String Target { get; set; }
- System.String ToString()

### Nerosoft.Euonia.Bus.DeadLetterSource

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Outbox |
| Inbox |


### Nerosoft.Euonia.Bus.DispatchInAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Bus.DispatchInAttribute(System.String[] transports)
- IEnumerable<System.String> Transports { get; }

### Nerosoft.Euonia.Bus.DistributedMessageAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Bus.DistributedMessageAttribute()

### Nerosoft.Euonia.Bus.EnqueueAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Bus.EnqueueAttribute(System.String name)
- System.String Name { get; }
- System.Int32 Priority { get; set; }

### Nerosoft.Euonia.Bus.IConfigurator

**类型**：interface

- Func<System.Type, System.String> ChannelResolver { get; }
- Nerosoft.Euonia.Bus.IMessageConvention Convention { get; }
- Nerosoft.Euonia.Bus.IMessageConventionBuilder ConventionBuilder { get; }
- IDictionary<System.String, Nerosoft.Euonia.Bus.ChannelRegistration> Registrations { get; }
- List<System.String> StrategyAssignedTypes { get; }
- IDictionary<System.String, Nerosoft.Euonia.Bus.ITransportStrategyBuilder> StrategyBuilders { get; }
- System.Int64 StrategyVersion { get; }
- event EventHandler<Nerosoft.Euonia.Bus.ChannelRegisteredEventArgs> ChannelRegistered
- Nerosoft.Euonia.Bus.ITransportStrategy GetStrategy(System.String transport)
- Nerosoft.Euonia.Bus.IConfigurator RegisterChannel<TMessage, TResult>(System.String channel, Func<TMessage, Nerosoft.Euonia.Bus.IMessageContext, Task<TResult>> handler)
- Nerosoft.Euonia.Bus.IConfigurator RegisterChannel<TMessage>(System.String channel, Func<TMessage, Nerosoft.Euonia.Bus.IMessageContext, System.Threading.Tasks.Task> handler)
- Nerosoft.Euonia.Bus.IConfigurator RegisterChannel(System.Reflection.Assembly[] assemblies)
- Nerosoft.Euonia.Bus.IConfigurator RegisterChannel(Func<IEnumerable<System.Type>> typesFactory)
- Nerosoft.Euonia.Bus.IConfigurator RegisterChannel(System.Type[] types)
- Nerosoft.Euonia.Bus.IConfigurator RegisterChannel(IEnumerable<System.Type> types)
- Nerosoft.Euonia.Bus.IConfigurator SetChannelResolver(Func<System.Type, System.String> channelResolver, System.Boolean replaceDefault = false)
- Nerosoft.Euonia.Bus.IConfigurator SetConvention(Action<Nerosoft.Euonia.Bus.IMessageConventionBuilder> conventionConfigurator)
- Nerosoft.Euonia.Bus.IConfigurator SetStrategy(System.String name, Action<Nerosoft.Euonia.Bus.ITransportStrategyBuilder> strategyConfigurator)

### Nerosoft.Euonia.Bus.IConsumer

**类型**：interface
接口 Nerosoft.Euonia.Bus.IRecipient、System.IDisposable

_无公共成员。_

### Nerosoft.Euonia.Bus.IDeadLetterService

**类型**：interface

- System.Boolean Discard(System.String messageId)
- System.Boolean DiscardById(System.String id)
- Nerosoft.Euonia.Bus.DeadLetterEntry Get(System.String messageId)
- IReadOnlyList<Nerosoft.Euonia.Bus.DeadLetterEntry> GetAll()
- Nerosoft.Euonia.Bus.DeadLetterEntry GetById(System.String id)
- Task<System.Boolean> ReplayAsync(System.String messageId, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Boolean> ReplayByIdAsync(System.String id, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.IDeadLetterStore

**类型**：interface

- System.Boolean Add(Nerosoft.Euonia.Bus.DeadLetterEntry entry)
- Nerosoft.Euonia.Bus.DeadLetterEntry Get(System.String messageId)
- IReadOnlyList<Nerosoft.Euonia.Bus.DeadLetterEntry> GetAll()
- Nerosoft.Euonia.Bus.DeadLetterEntry GetById(System.String id)
- System.Boolean Remove(System.String messageId)
- System.Boolean RemoveById(System.String id)
- void Update(Nerosoft.Euonia.Bus.DeadLetterEntry entry)

### Nerosoft.Euonia.Bus.IdentityAccessor

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.IdentityAccessor(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(System.AsyncCallback callback, System.Object object)
- System.Security.Principal.IPrincipal EndInvoke(System.IAsyncResult result)
- System.Security.Principal.IPrincipal Invoke()

### Nerosoft.Euonia.Bus.IExecutor

**类型**：interface
接口 Nerosoft.Euonia.Bus.IRecipient、System.IDisposable

_无公共成员。_

### Nerosoft.Euonia.Bus.IHandlerContext

**类型**：interface

- event EventHandler<Nerosoft.Euonia.Bus.MessageSubscribedEventArgs> MessageSubscribed
- Task<System.Object> HandleAsync(System.String channel, System.Object message, Nerosoft.Euonia.Bus.MessageContext context, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.IIdentityProvider

**类型**：interface

- System.Security.Principal.IPrincipal GetIdentity()

### Nerosoft.Euonia.Bus.IInboxStore

**类型**：interface

- void Cleanup(System.DateTime cutoff)
- void ClearCache()
- Nerosoft.Euonia.Bus.InboxEntry Get(System.String messageId)
- Nerosoft.Euonia.Bus.InboxEntry GetAndCache(System.String messageId)
- IReadOnlyList<Nerosoft.Euonia.Bus.InboxHandler> GetFailedMessages()
- System.Boolean Insert(System.String channel, Nerosoft.Euonia.Bus.IMessageEnvelope message, IReadOnlyList<System.String> handlers)
- System.Boolean Insert(Nerosoft.Euonia.Bus.InboxEntry entry)
- void MarkAsDeadLettered(System.String messageId, System.String handler, System.String errorMessage)
- void MarkAsFailed(System.String messageId, System.String handler, System.String errorMessage)
- void MarkAsSuccess(System.String messageId, System.String handler)

### Nerosoft.Euonia.Bus.IMessageContent

**类型**：interface

- Nullable<System.Int64> Length { get; }
- System.Byte[] ReadAsBytes()
- System.IO.Stream ReadAsStream()
- System.String ReadAsString()

### Nerosoft.Euonia.Bus.IMessageContext

**类型**：interface
接口 System.IDisposable

- System.String Authorization { get; }
- System.String ConversationId { get; }
- System.String CorrelationId { get; }
- IReadOnlyDictionary<System.String, System.String> Headers { get; }
- System.String MessageId { get; }
- Nerosoft.Euonia.Bus.MessageMetadata Metadata { get; }
- System.String RequestTraceId { get; }
- System.String Transport { get; }
- System.Security.Principal.IPrincipal User { get; }
- void Failure(System.Exception exception)
- void Response<TMessage>(TMessage message)

### Nerosoft.Euonia.Bus.IMessageConvention

**类型**：interface

- System.String Name { get; }
- Nerosoft.Euonia.Bus.MessageConventionType Detect(System.String channel, System.Type type)
- System.Boolean IsMulticast(System.String channel, System.Type type)
- System.Boolean IsRequest(System.String channel, System.Type type)
- System.Boolean IsUnicast(System.String channel, System.Type type)

### Nerosoft.Euonia.Bus.IMessageConventionBuilder

**类型**：interface

- Nerosoft.Euonia.Bus.IMessageConvention Convention { get; }
- Nerosoft.Euonia.Bus.IMessageConventionBuilder Add<TConvention>()
- Nerosoft.Euonia.Bus.IMessageConventionBuilder Add<TConvention>(TConvention convention)
- Nerosoft.Euonia.Bus.IMessageConventionBuilder Evaluate(Func<System.String, System.Type, Nerosoft.Euonia.Bus.MessageConventionType> predicate)
- Nerosoft.Euonia.Bus.IMessageConventionBuilder EvaluateMulticast(Func<System.String, System.Type, System.Boolean> predicate)
- Nerosoft.Euonia.Bus.IMessageConventionBuilder EvaluateRequest(Func<System.String, System.Type, System.Boolean> predicate)
- Nerosoft.Euonia.Bus.IMessageConventionBuilder EvaluateUnicast(Func<System.String, System.Type, System.Boolean> predicate)

### Nerosoft.Euonia.Bus.IMessageEnvelope

**类型**：interface

- System.String Authorization { get; }
- System.String Channel { get; }
- System.String ConversationId { get; }
- System.String CorrelationId { get; }
- System.String MessageId { get; }
- Nerosoft.Euonia.Bus.MessageMetadata Metadata { get; }
- System.Object Payload { get; }
- System.String RequestTraceId { get; }
- System.Int64 Timestamp { get; }
- System.String TypeName { get; }
- System.Security.Claims.ClaimsPrincipal User { get; }

### IMessageEnvelope<T>

**类型**：interface
接口 Nerosoft.Euonia.Bus.IMessageEnvelope

- T Payload { get; }

### Nerosoft.Euonia.Bus.IMessageSerializer

**类型**：interface

- T Deserialize<T>(System.Byte[] source)
- T Deserialize<T>(System.IO.Stream source)
- T Deserialize<T>(System.String source)
- System.Object Deserialize(System.String source, System.Type type)
- Task<T> DeserializeAsync<T>(System.IO.Stream source, System.Threading.CancellationToken cancellationToken = default)
- Task<T> DeserializeAsync<T>(System.Byte[] source, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Bus.IMessageEnvelope DeserializeEnvelope(System.String source, System.Type payloadType)
- System.String Serialize<T>(T source)
- Task<System.Byte[]> SerializeAsync<T>(T source, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.IMulticast

**类型**：interface
接口 Nerosoft.Euonia.Bus.ITransportable

_无公共成员。_

### Nerosoft.Euonia.Bus.InboxEntry

**类型**：class

- new Nerosoft.Euonia.Bus.InboxEntry()
- System.String Channel { get; set; }
- Nerosoft.Euonia.Bus.IMessageEnvelope Content { get; set; }
- System.DateTime CreatedAt { get; set; }
- IReadOnlyList<Nerosoft.Euonia.Bus.InboxHandler> Handlers { get; }
- System.String MessageId { get; set; }
- System.String MessageType { get; set; }
- void AddHandler(Nerosoft.Euonia.Bus.InboxHandler handler)
- void AddHandler(System.String handlerName)
- Nerosoft.Euonia.Bus.InboxHandler GetHandler(System.String name)
- System.String ToString()

### Nerosoft.Euonia.Bus.InboxHandler

**类型**：class

- new Nerosoft.Euonia.Bus.InboxHandler()
- System.String Error { get; set; }
- System.String MessageId { get; set; }
- System.String Name { get; set; }
- System.Int32 RetryAttempts { get; set; }
- Nerosoft.Euonia.Bus.InboxHandlerStatus Status { get; set; }
- void MarkAsDeadLettered(System.String error)
- void MarkAsFailed(System.String error)
- void MarkAsSuccess()
- System.String ToString()

### Nerosoft.Euonia.Bus.InboxHandlerStatus

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Pending |
| Success |
| Failed |
| DeadLettered |


### Nerosoft.Euonia.Bus.IOutboxStore

**类型**：interface

- void Cleanup(System.DateTime cutoff)
- void ClearCache()
- Nerosoft.Euonia.Bus.OutboxEntry Get(System.String messageId)
- Nerosoft.Euonia.Bus.OutboxEntry GetAndCache(System.String messageId)
- IReadOnlyList<Nerosoft.Euonia.Bus.OutboxTransport> GetFailedMessages()
- IReadOnlyList<Nerosoft.Euonia.Bus.OutboxTransport> GetPendingMessages(System.DateTime cutoff)
- System.Boolean Insert(Nerosoft.Euonia.Bus.IMessageEnvelope message, IReadOnlyList<System.String> transports)
- System.Boolean Insert(Nerosoft.Euonia.Bus.OutboxEntry entry)
- void MarkAsDeadLettered(System.String messageId, System.String transport, System.String errorMessage)
- void MarkAsFailed(System.String messageId, System.String transport, System.String errorMessage)
- void MarkAsSuccess(System.String messageId, System.String transport)

### Nerosoft.Euonia.Bus.IRecipient

**类型**：interface
接口 System.IDisposable

- System.String Name { get; }
- event EventHandler<Nerosoft.Euonia.Bus.MessageAcknowledgedEventArgs> MessageAcknowledged
- event EventHandler<Nerosoft.Euonia.Bus.MessageReceivedEventArgs> MessageReceived

### Nerosoft.Euonia.Bus.IRecipientRegistrar

**类型**：interface
接口 System.IDisposable、System.IAsyncDisposable

- System.Threading.Tasks.Task RegisterAsync(IDictionary<System.String, Nerosoft.Euonia.Bus.ChannelRegistration> registrations, System.String defaultTransporter, System.Threading.CancellationToken cancellationToken = default)

### IRequest<TResponse>

**类型**：interface
接口 Nerosoft.Euonia.Bus.ITransportable

_无公共成员。_

### Nerosoft.Euonia.Bus.ISubscriber

**类型**：interface
接口 Nerosoft.Euonia.Bus.IRecipient、System.IDisposable

_无公共成员。_

### Nerosoft.Euonia.Bus.ITransportable

**类型**：interface

_无公共成员。_

### Nerosoft.Euonia.Bus.ITransporter

**类型**：interface

- System.String Name { get; }
- event EventHandler<Nerosoft.Euonia.Bus.MessageDeliveredEventArgs> Delivered
- Task<TResponse> CallAsync<TRequest, TResponse>(IMessageEnvelope<TRequest> message, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task PublishAsync<TMessage>(IMessageEnvelope<TMessage> message, System.Threading.CancellationToken cancellationToken = default)
- Task<TResponse> SendAsync<TMessage, TResponse>(IMessageEnvelope<TMessage> message, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Bus.ITransportStrategy

**类型**：interface

- System.String Name { get; }
- System.Boolean Incoming(System.String channel, System.Type type)
- System.Boolean Outgoing(System.String channel, System.Type type)

### Nerosoft.Euonia.Bus.ITransportStrategyBuilder

**类型**：interface

- Nerosoft.Euonia.Bus.ITransportStrategy Strategy { get; }
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder Add<TStrategy>(TStrategy strategy)
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder Add<TStrategy>(Func<TStrategy> strategyFactory)
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder Add<TStrategy>()
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder EvaluateIncoming(Func<System.String, System.Type, System.Boolean> strategy)
- Nerosoft.Euonia.Bus.ITransportStrategyBuilder EvaluateOutgoing(Func<System.String, System.Type, System.Boolean> strategy)

### Nerosoft.Euonia.Bus.IUnicast

**类型**：interface
接口 Nerosoft.Euonia.Bus.ITransportable

_无公共成员。_

### Nerosoft.Euonia.Bus.LocalMessageAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Bus.LocalMessageAttribute()

### Nerosoft.Euonia.Bus.MessageAcknowledgedEventArgs

**类型**：class
基类 Nerosoft.Euonia.Bus.MessageProcessedEventArgs

- new Nerosoft.Euonia.Bus.MessageAcknowledgedEventArgs(System.Object message, Nerosoft.Euonia.Bus.IMessageContext context)

### Nerosoft.Euonia.Bus.MessageBusException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.MessageBusException(System.Object messageContext)
- new Nerosoft.Euonia.Bus.MessageBusException(System.Object messageContext, System.String message)
- new Nerosoft.Euonia.Bus.MessageBusException(System.Object messageContext, System.String message, System.Exception innerException)
- System.Object MessageContext { get; }

### Nerosoft.Euonia.Bus.MessageContext

**类型**：class
接口 Nerosoft.Euonia.Bus.IMessageContext、System.IDisposable

- new Nerosoft.Euonia.Bus.MessageContext()
- new Nerosoft.Euonia.Bus.MessageContext(Nerosoft.Euonia.Bus.IMessageEnvelope envelope)
- System.String Authorization { get; set; }
- System.String ConversationId { get; set; }
- System.String CorrelationId { get; set; }
- Nerosoft.Euonia.Bus.MessageExecutionDescriptor ExecutionDescriptor { get; set; }
- IReadOnlyDictionary<System.String, System.String> Headers { get; }
- System.String MessageId { get; set; }
- Nerosoft.Euonia.Bus.MessageMetadata Metadata { get; set; }
- System.String RequestTraceId { get; set; }
- System.String Transport { get; set; }
- System.Security.Principal.IPrincipal User { get; }
- event EventHandler<Nerosoft.Euonia.Bus.MessageHandledEventArgs> Completed
- event EventHandler<System.Exception> Failed
- event EventHandler<Nerosoft.Euonia.Bus.MessageRepliedEventArgs> Responded
- void Complete(System.String messageId)
- void Complete(System.String messageId, System.Type handlerType)
- void Dispose()
- void Failure(System.Exception exception)
- void Response(System.Object message)
- void Response<TMessage>(TMessage message)

### Nerosoft.Euonia.Bus.MessageConventionException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.MessageConventionException()
- new Nerosoft.Euonia.Bus.MessageConventionException(System.String message)
- new Nerosoft.Euonia.Bus.MessageConventionException(System.String message, System.Exception innerException)

### Nerosoft.Euonia.Bus.MessageConventionType

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| None |
| Unicast |
| Multicast |
| Request |


### Nerosoft.Euonia.Bus.MessageDeliveredEventArgs

**类型**：class
基类 Nerosoft.Euonia.Bus.MessageProcessedEventArgs

- new Nerosoft.Euonia.Bus.MessageDeliveredEventArgs(System.Object message, Nerosoft.Euonia.Bus.IMessageContext context)

### Nerosoft.Euonia.Bus.MessageDeliverException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.MessageDeliverException()
- new Nerosoft.Euonia.Bus.MessageDeliverException(System.String message)
- new Nerosoft.Euonia.Bus.MessageDeliverException(System.String message, System.Exception innerException)

### Nerosoft.Euonia.Bus.MessageExecutionDescriptor

**类型**：struct
接口 IEquatable<Nerosoft.Euonia.Bus.MessageExecutionDescriptor>

- System.IServiceProvider BorrowedProvider { get; }
- static Nerosoft.Euonia.Bus.MessageExecutionDescriptor DistributedInbound { get; }
- static Nerosoft.Euonia.Bus.MessageExecutionDescriptor OutboxDispatch { get; }
- Nerosoft.Euonia.Bus.MessageProviderLease ProviderLease { get; }
- System.RequestContext PublisherContext { get; }
- static Nerosoft.Euonia.Bus.MessageExecutionDescriptor RemoteInbound { get; }
- static Nerosoft.Euonia.Bus.MessageExecutionDescriptor Replay { get; }
- Nerosoft.Euonia.Bus.MessageRequestContextSource RequestContextSource { get; }
- static Nerosoft.Euonia.Bus.MessageExecutionDescriptor InProcessCall(System.IServiceProvider provider, System.RequestContext publisherContext)
- static Nerosoft.Euonia.Bus.MessageExecutionDescriptor InProcessPublish(System.RequestContext publisherContext)
- static Nerosoft.Euonia.Bus.MessageExecutionDescriptor RemoteInboundBorrowed(System.IServiceProvider requestServices)

### Nerosoft.Euonia.Bus.MessageHandledEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Bus.MessageHandledEventArgs(System.String messageId)
- System.Type HandlerType { get; set; }
- System.String MessageId { get; }

### Nerosoft.Euonia.Bus.MessageHeaders

**类型**：static class

- static const System.String Authorization = x-authorization
- static const System.String Channel = x-channel
- static const System.String ContentEncoding = x-content-encoding
- static const System.String ContentType = x-content-type
- static const System.String ConversationId = x-conversation-id
- static const System.String CorrelationId = x-correlation-id
- static const System.String DeliveryMode = x-delivery-mode
- static const System.String Expiration = x-expiration
- static const System.String MessageId = x-message-id
- static const System.String MessageType = x-message-type
- static const System.String Priority = x-priority
- static const System.String ReplyTo = x-reply-to
- static const System.String RequestTraceId = x-request-trace-id
- static const System.String RoutingKey = x-routing-key
- static const System.String Timestamp = x-timestamp
- static const System.String Type = x-type
- static const System.String UserId = x-user-id

### Nerosoft.Euonia.Bus.MessageMetadata

**类型**：class
接口 IDictionary<System.String, System.Object>、ICollection<KeyValuePair<System.String, System.Object>>、IEnumerable<KeyValuePair<System.String, System.Object>>、System.Collections.IEnumerable

- new Nerosoft.Euonia.Bus.MessageMetadata()
- System.Int32 Count { get; }
- System.Boolean IsReadOnly { get; }
- System.Object this[System.String key] { get; set; }
- ICollection<System.String> Keys { get; }
- ICollection<System.Object> Values { get; }
- void Add(System.String key, System.Object value)
- void Add(KeyValuePair<System.String, System.Object> item)
- void Clear()
- System.Boolean Contains(KeyValuePair<System.String, System.Object> item)
- System.Boolean ContainsKey(System.String key)
- void CopyTo(KeyValuePair<System.String, System.Object>[] items, System.Int32 index)
- IEnumerator<KeyValuePair<System.String, System.Object>> GetEnumerator()
- System.Boolean Remove(KeyValuePair<System.String, System.Object> item)
- System.Boolean Remove(System.String key)
- System.Boolean TryGetValue(System.String key, out System.Object value)

### Nerosoft.Euonia.Bus.MessagePersistentException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.MessagePersistentException()
- new Nerosoft.Euonia.Bus.MessagePersistentException(System.String message)
- new Nerosoft.Euonia.Bus.MessagePersistentException(System.String message, System.Exception innerException)

### Nerosoft.Euonia.Bus.MessageProcessedEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Bus.MessageProcessedEventArgs(System.Object message, Nerosoft.Euonia.Bus.IMessageContext context, Nerosoft.Euonia.Bus.MessageProcessType processType)
- Nerosoft.Euonia.Bus.IMessageContext Context { get; }
- System.Object Message { get; }
- Nerosoft.Euonia.Bus.MessageProcessType ProcessType { get; }

### Nerosoft.Euonia.Bus.MessageProcessingException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.MessageProcessingException(System.String messageId)
- new Nerosoft.Euonia.Bus.MessageProcessingException(System.String messageId, System.String message)
- new Nerosoft.Euonia.Bus.MessageProcessingException(System.String messageId, System.String message, System.Exception innerException)
- System.String MessageId { get; }

### Nerosoft.Euonia.Bus.MessageProcessType

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Send |
| Dispatch |
| Receive |


### Nerosoft.Euonia.Bus.MessageProperties

**类型**：static class

- static const System.String PriorityKey = $nerosoft.euonia:message.priority
- static const System.String QueueKey = $nerosoft.euonia:message.queue
- static Nullable<System.Int32> GetPriority(Nerosoft.Euonia.Bus.IMessageEnvelope message)
- static System.String GetQueue(Nerosoft.Euonia.Bus.IMessageEnvelope message)
- static void SetPriority(Nerosoft.Euonia.Bus.IMessageEnvelope message, System.Int32 priority)
- static void SetQueue(Nerosoft.Euonia.Bus.IMessageEnvelope message, System.String queue)

### Nerosoft.Euonia.Bus.MessageProviderLease

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| OwnScope |
| Borrow |


### Nerosoft.Euonia.Bus.MessageReceivedEventArgs

**类型**：class
基类 Nerosoft.Euonia.Bus.MessageProcessedEventArgs

- new Nerosoft.Euonia.Bus.MessageReceivedEventArgs(System.Object message, Nerosoft.Euonia.Bus.IMessageContext context)

### Nerosoft.Euonia.Bus.MessageRepliedEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Bus.MessageRepliedEventArgs(System.Object result)
- System.Object Result { get; }

### Nerosoft.Euonia.Bus.MessageRequestContextSource

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Envelope |
| EnvelopeOverride |
| Inherit |
| PublisherSnapshot |


### Nerosoft.Euonia.Bus.MessageSubscribedEventArgs

**类型**：class
基类 System.EventArgs

- new Nerosoft.Euonia.Bus.MessageSubscribedEventArgs(System.String channel, System.Type messageType, System.Type handlerType)
- System.String Channel { get; }
- System.Type HandlerType { get; }
- System.Type MessageType { get; }

### Nerosoft.Euonia.Bus.MessageTransportException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.MessageTransportException()
- new Nerosoft.Euonia.Bus.MessageTransportException(System.String message)
- new Nerosoft.Euonia.Bus.MessageTransportException(System.String message, System.Exception innerException)

### Nerosoft.Euonia.Bus.MessageTypeException

**类型**：class
基类 System.Exception；接口 System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Bus.MessageTypeException()
- new Nerosoft.Euonia.Bus.MessageTypeException(System.String message)
- new Nerosoft.Euonia.Bus.MessageTypeException(System.String message, System.Exception innerException)

### Nerosoft.Euonia.Bus.MulticastAttribute

**类型**：class
基类 Nerosoft.Euonia.Bus.TransportableAttribute

- new Nerosoft.Euonia.Bus.MulticastAttribute()

### Nerosoft.Euonia.Bus.OutboxEntry

**类型**：class

- new Nerosoft.Euonia.Bus.OutboxEntry()
- System.String Channel { get; set; }
- Nerosoft.Euonia.Bus.IMessageEnvelope Content { get; set; }
- System.DateTime CreatedAt { get; set; }
- System.String MessageId { get; set; }
- System.String MessageType { get; set; }
- IReadOnlyList<Nerosoft.Euonia.Bus.OutboxTransport> Transports { get; }
- void AddTransport(Nerosoft.Euonia.Bus.OutboxTransport transport)
- void AddTransport(System.String transportName)
- Nerosoft.Euonia.Bus.OutboxTransport GetTransport(System.String name)
- System.String ToString()

### Nerosoft.Euonia.Bus.OutboxTransport

**类型**：class

- new Nerosoft.Euonia.Bus.OutboxTransport()
- System.String Error { get; set; }
- System.String MessageId { get; set; }
- System.String Name { get; set; }
- System.Int32 RetryAttempts { get; set; }
- Nerosoft.Euonia.Bus.OutboxTransportStatus Status { get; set; }
- void MarkAsDeadLettered(System.String error)
- void MarkAsFailed(System.String error)
- void MarkAsSuccess()
- System.String ToString()

### Nerosoft.Euonia.Bus.OutboxTransportStatus

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Pending |
| Success |
| Failed |
| DeadLettered |


### Nerosoft.Euonia.Bus.ReceiveInAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Bus.ReceiveInAttribute(System.String[] transports)
- IEnumerable<System.String> Transports { get; }

### Nerosoft.Euonia.Bus.RequestAttribute

**类型**：class
基类 Nerosoft.Euonia.Bus.TransportableAttribute

- new Nerosoft.Euonia.Bus.RequestAttribute(System.Type responseType)
- System.Type ResponseType { get; }

### Nerosoft.Euonia.Bus.SubscribeAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Bus.SubscribeAttribute()
- new Nerosoft.Euonia.Bus.SubscribeAttribute(System.String name)
- System.String Group { get; set; }
- System.String Name { get; }

### Nerosoft.Euonia.Bus.TransportableAttribute

**类型**：class
基类 System.Attribute

_无公共成员。_

### Nerosoft.Euonia.Bus.TransportError

**类型**：class

- new Nerosoft.Euonia.Bus.TransportError()
- System.String Message { get; set; }
- System.String Type { get; set; }
- static Nerosoft.Euonia.Bus.TransportError From(System.Exception exception)
- System.Exception ToException()

### Nerosoft.Euonia.Bus.UnicastAttribute

**类型**：class
基类 Nerosoft.Euonia.Bus.TransportableAttribute

- new Nerosoft.Euonia.Bus.UnicastAttribute()
