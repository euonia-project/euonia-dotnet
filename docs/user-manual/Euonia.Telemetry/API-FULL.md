# Euonia.Telemetry 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Telemetry |
| 公共类型 | 20 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Telemetry

### Nerosoft.Euonia.Telemetry.DefaultTelemetryContextProvider

**类型**：class
接口 Nerosoft.Euonia.Telemetry.ITelemetryContextProvider

### Nerosoft.Euonia.Telemetry.DefaultTelemetryPropagator

**类型**：class
接口 Nerosoft.Euonia.Telemetry.ITelemetryPropagator

- new Nerosoft.Euonia.Telemetry.DefaultTelemetryPropagator()
- Nullable<System.Diagnostics.ActivityContext> Extract(IDictionary<System.String, System.Object> carrier)
- void Inject(IDictionary<System.String, System.Object> carrier, System.Diagnostics.Activity activity = null)

### Nerosoft.Euonia.Telemetry.DefaultTelemetryRedactor

**类型**：class
接口 Nerosoft.Euonia.Telemetry.ITelemetryRedactor

- new Nerosoft.Euonia.Telemetry.DefaultTelemetryRedactor(IOptionsMonitor<Nerosoft.Euonia.Telemetry.TelemetryOptions> options)
- static const System.String Mask = ***
- System.Object Redact(System.String key, System.Object value)
- System.Boolean ShouldRedact(System.String key)

### Nerosoft.Euonia.Telemetry.ITelemetryContextProvider

**类型**：interface

- Nerosoft.Euonia.Telemetry.TelemetryContext GetContext()

### Nerosoft.Euonia.Telemetry.ITelemetryEnricher

**类型**：interface

- void Enrich(System.Diagnostics.Activity activity, Nerosoft.Euonia.Telemetry.TelemetryContext context)

### Nerosoft.Euonia.Telemetry.ITelemetryPropagator

**类型**：interface

- Nullable<System.Diagnostics.ActivityContext> Extract(IDictionary<System.String, System.Object> carrier)
- void Inject(IDictionary<System.String, System.Object> carrier, System.Diagnostics.Activity activity = null)

### Nerosoft.Euonia.Telemetry.ITelemetryRedactor

**类型**：interface

- System.Object Redact(System.String key, System.Object value)
- System.Boolean ShouldRedact(System.String key)

### Nerosoft.Euonia.Telemetry.ITelemetrySourceFactory

**类型**：interface

- IReadOnlyCollection<Nerosoft.Euonia.Telemetry.TelemetrySource> Sources { get; }
- Nerosoft.Euonia.Telemetry.TelemetrySource Create(System.String sourceName, System.String meterName = null)

### Nerosoft.Euonia.Telemetry.TelemetryActivity

**类型**：struct
接口 System.IDisposable

- new Nerosoft.Euonia.Telemetry.TelemetryActivity(System.Diagnostics.Activity activity, Nerosoft.Euonia.Telemetry.ITelemetryRedactor redactor = null)
- System.Diagnostics.Activity Activity { get; }
- System.Boolean IsRecording { get; }
- void AddEvent(System.String name, KeyValuePair<System.String, System.Object>[] tags)
- void Dispose()
- void RecordException(System.Exception exception)
- void SetOutcome(System.Boolean success, System.String errorType = null)
- void SetTag(System.String key, System.Object value)

### Nerosoft.Euonia.Telemetry.TelemetryComponentOptions

**类型**：class

- new Nerosoft.Euonia.Telemetry.TelemetryComponentOptions()
- System.Boolean Enabled { get; set; }
- System.Boolean MetricsEnabled { get; set; }
- System.Boolean TracesEnabled { get; set; }

### Nerosoft.Euonia.Telemetry.TelemetryContext

**类型**：class
接口 IEquatable<Nerosoft.Euonia.Telemetry.TelemetryContext>

- new Nerosoft.Euonia.Telemetry.TelemetryContext(System.String TraceId, System.String SpanId, System.String CorrelationId, System.String RequestTraceId, System.String TenantId, System.String ActorTenantId, System.String UserId, System.Boolean IsCrossTenant)
- System.String ActorTenantId { get; set; }
- System.String CorrelationId { get; set; }
- System.Boolean IsCrossTenant { get; set; }
- System.String RequestTraceId { get; set; }
- System.String SpanId { get; set; }
- System.String TenantId { get; set; }
- System.String TraceId { get; set; }
- System.String UserId { get; set; }

### Nerosoft.Euonia.Telemetry.TelemetryContextEnricher

**类型**：class
接口 Nerosoft.Euonia.Telemetry.ITelemetryEnricher

### Nerosoft.Euonia.Telemetry.TelemetryLogScope

**类型**：static class

- static System.IDisposable Begin(Microsoft.Extensions.Logging.ILogger logger, Nerosoft.Euonia.Telemetry.ITelemetryContextProvider contextProvider)

### Nerosoft.Euonia.Telemetry.TelemetryNames

**类型**：static class

- static const System.String AuditingPersist = euonia.auditing.persist
- static const System.String AuditingRecords = euonia.auditing.records
- static const System.String AuditingRecordsDropped = euonia.auditing.records.dropped
- static const System.String AuditingWriteDuration = euonia.auditing.write.duration
- static const System.String AuditingWriteFailures = euonia.auditing.write.failures
- static const System.String BusDeadLetter = euonia.bus.deadletter
- static const System.String BusInboxPending = euonia.bus.inbox.pending
- static const System.String BusInboxProcess = euonia.bus.inbox.process
- static const System.String BusOutboxDispatch = euonia.bus.outbox.dispatch
- static const System.String BusOutboxPending = euonia.bus.outbox.pending
- static const System.String BusProcess = euonia.bus.process
- static const System.String BusProcessDuration = euonia.bus.process.duration
- static const System.String BusProcessFailures = euonia.bus.process.failures
- static const System.String CacheGet = euonia.cache.get
- static const System.String CacheRemove = euonia.cache.remove
- static const System.String CacheSet = euonia.cache.set
- static const System.String CorrelationId = euonia.correlation_id
- static const System.String ErrorType = error.type
- static const System.String LockAcquire = euonia.lock.acquire
- static const System.String MessagingConversationId = messaging.conversation_id
- static const System.String MessagingDestination = messaging.destination.name
- static const System.String MessagingMessageId = messaging.message.id
- static const System.String MessagingMessageType = messaging.message.type
- static const System.String MessagingOperationType = messaging.operation.type
- static const System.String MessagingSystem = messaging.system
- static const System.String MessagingTransport = messaging.transport
- static const System.String Outcome = euonia.outcome
- static const System.String QuartzExecute = euonia.quartz.execute
- static const System.String RepositoryExecute = euonia.repository.execute
- static const System.String RequestTraceId = euonia.request_trace_id
- static const System.String SecurityAuthorize = euonia.security.authorize
- static const System.String TenancyCross = euonia.tenancy.cross
- static const System.String TenancyResolve = euonia.tenancy.resolve
- static const System.String TenantActorId = tenant.actor.id
- static const System.String TenantCross = tenant.cross
- static const System.String TenantCrossReason = tenant.cross.reason
- static const System.String TenantId = tenant.id
- static const System.String UnitOfWorkCommit = euonia.uow.commit
- static const System.String UnitOfWorkRollback = euonia.uow.rollback
- static const System.String UserId = euonia.user.id

### Nerosoft.Euonia.Telemetry.TelemetryOptions

**类型**：class

- new Nerosoft.Euonia.Telemetry.TelemetryOptions()
- Dictionary<System.String, Nerosoft.Euonia.Telemetry.TelemetryComponentOptions> Components { get; set; }
- System.Boolean Enabled { get; set; }
- System.String Environment { get; set; }
- System.String MessageTypeTag { get; set; }
- System.Boolean MetricsEnabled { get; set; }
- System.Boolean RecordTenantId { get; set; }
- System.Boolean RecordUserId { get; set; }
- Nerosoft.Euonia.Telemetry.TelemetryRedactionOptions Redaction { get; set; }
- System.String ServiceName { get; set; }
- System.String ServiceVersion { get; set; }
- System.Boolean TracesEnabled { get; set; }
- static const System.String ConfigurationSection = Euonia:Telemetry

### Nerosoft.Euonia.Telemetry.TelemetryRedactionOptions

**类型**：class

- new Nerosoft.Euonia.Telemetry.TelemetryRedactionOptions()
- System.Boolean Enabled { get; set; }
- System.String[] Keys { get; set; }

### Nerosoft.Euonia.Telemetry.TelemetryServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEuoniaTelemetry(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Telemetry.TelemetryOptions> configure = null)

### Nerosoft.Euonia.Telemetry.TelemetrySource

**类型**：class
接口 System.IDisposable

- new Nerosoft.Euonia.Telemetry.TelemetrySource(System.String sourceName, System.String meterName = null, System.Boolean tracesEnabled = true, System.Boolean metricsEnabled = true, Nerosoft.Euonia.Telemetry.ITelemetryRedactor redactor = null, IReadOnlyList<Nerosoft.Euonia.Telemetry.ITelemetryEnricher> enrichers = null, Nerosoft.Euonia.Telemetry.ITelemetryContextProvider contextProvider = null)
- System.Diagnostics.ActivitySource ActivitySource { get; }
- System.Boolean IsMetricEnabled { get; }
- System.Boolean IsTraceEnabled { get; }
- System.Diagnostics.Metrics.Meter Meter { get; }
- System.String MeterName { get; }
- System.String SourceName { get; }
- Counter<System.Int64> Counter(System.String name, System.String unit = null, System.String description = null)
- void Dispose()
- Histogram<System.Double> Histogram(System.String name, System.String unit = null, System.String description = null)
- Nerosoft.Euonia.Telemetry.TelemetryActivity StartActivity(System.String name, System.Diagnostics.ActivityKind kind = 0, Nullable<System.Diagnostics.ActivityContext> parent = default, IEnumerable<System.Diagnostics.ActivityLink> links = null, IEnumerable<KeyValuePair<System.String, System.Object>> tags = null)

### Nerosoft.Euonia.Telemetry.TelemetrySourceFactory

**类型**：class
接口 Nerosoft.Euonia.Telemetry.ITelemetrySourceFactory、System.IDisposable

- new Nerosoft.Euonia.Telemetry.TelemetrySourceFactory(IOptionsMonitor<Nerosoft.Euonia.Telemetry.TelemetryOptions> options, Nerosoft.Euonia.Telemetry.ITelemetryRedactor redactor, IEnumerable<Nerosoft.Euonia.Telemetry.ITelemetryEnricher> enrichers, Nerosoft.Euonia.Telemetry.ITelemetryContextProvider contextProvider = null, Nerosoft.Euonia.Telemetry.TelemetrySourceRegistry registry = null)
- IReadOnlyCollection<Nerosoft.Euonia.Telemetry.TelemetrySource> Sources { get; }
- Nerosoft.Euonia.Telemetry.TelemetrySource Create(System.String sourceName, System.String meterName = null)
- void Dispose()

### Nerosoft.Euonia.Telemetry.TelemetrySourceRegistry

**类型**：class
接口 System.IDisposable

- new Nerosoft.Euonia.Telemetry.TelemetrySourceRegistry()
- void Dispose()
- IReadOnlyCollection<Nerosoft.Euonia.Telemetry.TelemetrySource> GetAll()
- void Register(Nerosoft.Euonia.Telemetry.TelemetrySource source)
