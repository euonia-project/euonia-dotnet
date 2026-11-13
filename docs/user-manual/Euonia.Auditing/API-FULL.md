# Euonia.Auditing 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Auditing |
| 公共类型 | 27 |
| 命名空间 | 1 |

## 命名空间 Nerosoft.Euonia.Auditing

### Nerosoft.Euonia.Auditing.AuditedAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Auditing.AuditedAttribute()
- System.String ActionName { get; set; }
- System.Boolean RecordResult { get; set; }

### Nerosoft.Euonia.Auditing.AuditingAction

**类型**：class

- new Nerosoft.Euonia.Auditing.AuditingAction()
- System.Guid ActionId { get; set; }
- System.String ActionName { get; set; }
- System.String ExceptionMessage { get; set; }
- System.String ExceptionStackTrace { get; set; }
- System.String ExceptionType { get; set; }
- System.Int32 ExecutionDuration { get; set; }
- System.DateTime ExecutionTime { get; set; }
- System.String ExtraProperties { get; set; }
- System.String IdempotencyKey { get; set; }
- Nullable<System.Boolean> IsSuccess { get; set; }
- System.String MethodName { get; set; }
- System.String Parameters { get; set; }
- Nullable<System.Guid> ParentActionId { get; set; }
- System.String Result { get; set; }
- System.String ServiceName { get; set; }

### Nerosoft.Euonia.Auditing.AuditingDelivery

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| Immediate |
| Batched |


### Nerosoft.Euonia.Auditing.AuditingMetrics

**类型**：static class

- static const System.String DroppedMetric = euonia.auditing.records.dropped
- static const System.String MeterName = Nerosoft.Euonia.Auditing
- static const System.String RecordsMetric = euonia.auditing.records
- static const System.String RejectedMetric = euonia.auditing.records.rejected
- static const System.String WriteDurationMetric = euonia.auditing.write.duration
- static const System.String WriteFailuresMetric = euonia.auditing.write.failures
- static void RecordDropped()
- static void RecordRejected()
- static void RecordWrite(System.Boolean success, System.Double elapsedSeconds)

### Nerosoft.Euonia.Auditing.AuditingOptions

**类型**：class

- new Nerosoft.Euonia.Auditing.AuditingOptions()
- System.String ApplicationName { get; set; }
- System.TimeSpan BatchInterval { get; set; }
- System.Int32 BatchSize { get; set; }
- Func<Nerosoft.Euonia.Auditing.AuditingQuery, System.Boolean> CrossTenantQueryAuthorizer { get; set; }
- System.Boolean Enabled { get; set; }
- Nerosoft.Euonia.Auditing.AuditingDelivery FailureDelivery { get; set; }
- System.Int32 MaxDepth { get; set; }
- Nerosoft.Euonia.Auditing.AuditingTriggerMode Mode { get; set; }
- System.Int32 QueueCapacity { get; set; }
- System.Boolean RecordAnonymous { get; set; }
- System.Boolean RecordParameters { get; set; }
- System.Boolean RecordResult { get; set; }
- System.Boolean RecordStackTrace { get; set; }
- Nerosoft.Euonia.Auditing.AuditingDelivery SuccessDelivery { get; set; }
- System.Int32 WriteRetryCount { get; set; }
- static const System.String ConfigurationSection = Euonia:Auditing

### Nerosoft.Euonia.Auditing.AuditingQuery

**类型**：class

- new Nerosoft.Euonia.Auditing.AuditingQuery()
- System.Boolean AllowCrossTenant { get; set; }
- System.String CorrelationId { get; set; }
- System.String CrossTenantReason { get; set; }
- Nullable<System.DateTime> From { get; set; }
- Nullable<System.Boolean> IsSuccess { get; set; }
- System.Int32 Skip { get; set; }
- System.Int32 Take { get; set; }
- System.String TenantId { get; set; }
- Nullable<System.DateTime> To { get; set; }
- System.String UserId { get; set; }

### Nerosoft.Euonia.Auditing.AuditingQueryAuthorizer

**类型**：static class

- static System.String DeleteTenantScope { get; }
- static void Authorize(Nerosoft.Euonia.Auditing.AuditingQuery query)
- static void ConfigureCrossTenantAuthorization(Func<Nerosoft.Euonia.Auditing.AuditingQuery, System.Boolean> authorization)
- static void EnsureDeleteAuthorized()
- static void EnsureScopeAccess(System.String scopeId)
- static Nerosoft.Euonia.Auditing.AuditingQuery Normalize(Nerosoft.Euonia.Auditing.AuditingQuery query)

### Nerosoft.Euonia.Auditing.AuditingRecord

**类型**：class

- new Nerosoft.Euonia.Auditing.AuditingRecord()
- List<Nerosoft.Euonia.Auditing.AuditingAction> Actions { get; set; }
- System.String ApplicationName { get; set; }
- System.String BrowserInfo { get; set; }
- System.String ClientId { get; set; }
- System.String ClientIpAddress { get; set; }
- System.String ClientName { get; set; }
- System.String CorrelationId { get; set; }
- System.String CrossTenantReason { get; set; }
- System.Int32 ExecutionDuration { get; set; }
- System.DateTime ExecutionTime { get; set; }
- System.String ExtraProperties { get; set; }
- System.String HttpMethod { get; set; }
- Nullable<System.Int32> HttpStatusCode { get; set; }
- System.Guid Id { get; set; }
- System.Boolean IsCrossTenant { get; set; }
- Nullable<System.Boolean> IsSuccess { get; set; }
- System.DateTime RecordedAt { get; set; }
- System.String RequestTraceId { get; set; }
- System.String ScopeId { get; set; }
- System.String SpanId { get; set; }
- System.String TargetTenantId { get; set; }
- System.String TenantId { get; set; }
- System.String TenantName { get; set; }
- System.String TraceId { get; set; }
- System.String UnitOfWorkId { get; set; }
- System.String Url { get; set; }
- System.String UserId { get; set; }
- System.String UserName { get; set; }

### Nerosoft.Euonia.Auditing.AuditingRecorder

**类型**：class
接口 Nerosoft.Euonia.Auditing.IAuditingRecorder、System.IAsyncDisposable

- new Nerosoft.Euonia.Auditing.AuditingRecorder(Nerosoft.Euonia.Auditing.AuditingOptions options, Nerosoft.Euonia.Auditing.IAuditingWriter writer = null, ILogger<Nerosoft.Euonia.Auditing.AuditingRecorder> logger = null)
- Nerosoft.Euonia.Auditing.IAuditingScope CurrentScope { get; }
- System.Boolean IsEnabled { get; }
- void Attach(Nerosoft.Euonia.Auditing.IAuditingScope scope)
- void CompleteInBackground(Nerosoft.Euonia.Auditing.IAuditingScope scope)
- Nerosoft.Euonia.Auditing.IAuditingScope CreateDetachedScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options = null)
- Nerosoft.Euonia.Auditing.IAuditingScope CreateScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options = null)
- void Detach(Nerosoft.Euonia.Auditing.IAuditingScope scope)
- System.Threading.Tasks.ValueTask DisposeAsync()
- Nerosoft.Euonia.Auditing.IAuditingScope EnsureScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options = null)
- Nerosoft.Euonia.Auditing.IAuditingScope EnsureScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options, System.Boolean force)
- System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.ValueTask SubmitAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.AuditingRecordExtensions

**类型**：static class

- static System.String MaskExceptionMessage(System.String message)
- static void MergeExtraProperties(Nerosoft.Euonia.Auditing.AuditingRecord record, IReadOnlyDictionary<System.String, System.String> values)

### Nerosoft.Euonia.Auditing.AuditingRecordFactory

**类型**：static class

- static System.Guid NewId()

### Nerosoft.Euonia.Auditing.AuditingScopeOptions

**类型**：class

- new Nerosoft.Euonia.Auditing.AuditingScopeOptions()
- System.String ApplicationName { get; set; }
- System.Boolean AutoComplete { get; set; }
- System.String BrowserInfo { get; set; }
- System.String ClientIpAddress { get; set; }
- System.String ClientName { get; set; }
- System.String HttpMethod { get; set; }
- System.String IdempotencyKey { get; set; }
- System.String OperationKey { get; set; }
- System.String Url { get; set; }

### Nerosoft.Euonia.Auditing.AuditingServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddAuditing(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Auditing.AuditingOptions> configure = null)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddFileAuditingSpool(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String directory)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddInMemoryAuditingStore(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

### Nerosoft.Euonia.Auditing.AuditingTriggerMode

**类型**：enum
接口 System.IComparable、System.ISpanFormattable、System.IFormattable、System.IConvertible

| 值 |
|---|
| All |
| WritesOnly |


### Nerosoft.Euonia.Auditing.AuditingWriter

**类型**：class
接口 Nerosoft.Euonia.Auditing.IAuditingWriter、Microsoft.Extensions.Hosting.IHostedService、System.IAsyncDisposable

- new Nerosoft.Euonia.Auditing.AuditingWriter(System.IServiceProvider services, IOptions<Nerosoft.Euonia.Auditing.AuditingOptions> options, ILogger<Nerosoft.Euonia.Auditing.AuditingWriter> logger = null, Nerosoft.Euonia.Auditing.IAuditingSpool spool = null)
- System.Threading.Tasks.ValueTask DisposeAsync()
- System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task StartAsync(System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.Task StopAsync(System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.ValueTask WriteAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.FileAuditingSpool

**类型**：class
接口 Nerosoft.Euonia.Auditing.IAuditingSpool

- new Nerosoft.Euonia.Auditing.FileAuditingSpool(System.String directory, ILogger<Nerosoft.Euonia.Auditing.FileAuditingSpool> logger = null)
- System.Threading.Tasks.ValueTask DeleteAsync(System.String key, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<IReadOnlyList<KeyValuePair<System.String, Nerosoft.Euonia.Auditing.AuditingRecord>>> LoadPendingAsync(System.Threading.CancellationToken cancellationToken = default)
- ValueTask<System.String> PersistAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.IAuditingAction

**类型**：interface

- System.Guid ActionId { get; }
- System.String IdempotencyKey { get; }
- void Complete(System.Object result)
- void Fail(System.Exception exception)
- void SetExtraProperty(System.String name, System.Object value)
- void SetParameter(System.String name, System.Object value)
- void SetRecordResult(System.Boolean recordResult)

### Nerosoft.Euonia.Auditing.IAuditingRecorder

**类型**：interface

- Nerosoft.Euonia.Auditing.IAuditingScope CurrentScope { get; }
- System.Boolean IsEnabled { get; }
- void Attach(Nerosoft.Euonia.Auditing.IAuditingScope scope)
- void CompleteInBackground(Nerosoft.Euonia.Auditing.IAuditingScope scope)
- Nerosoft.Euonia.Auditing.IAuditingScope CreateDetachedScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options = null)
- Nerosoft.Euonia.Auditing.IAuditingScope CreateScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options = null)
- void Detach(Nerosoft.Euonia.Auditing.IAuditingScope scope)
- Nerosoft.Euonia.Auditing.IAuditingScope EnsureScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options = null)
- Nerosoft.Euonia.Auditing.IAuditingScope EnsureScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options, System.Boolean force)
- System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.ValueTask SubmitAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.IAuditingScope

**类型**：interface
接口 System.IAsyncDisposable

- System.Boolean IsCompleted { get; }
- Nerosoft.Euonia.Auditing.AuditingRecord Record { get; }
- System.String ScopeId { get; }
- System.Threading.Tasks.ValueTask AbortAsync(System.Exception exception, System.Threading.CancellationToken cancellationToken = default)
- Nerosoft.Euonia.Auditing.IAuditingAction BeginAction(System.String serviceName, System.String methodName, System.String actionName = null)
- System.Threading.Tasks.ValueTask CompleteAsync(System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.IAuditingSpool

**类型**：interface

- System.Threading.Tasks.ValueTask DeleteAsync(System.String key, System.Threading.CancellationToken cancellationToken = default)
- ValueTask<IReadOnlyList<KeyValuePair<System.String, Nerosoft.Euonia.Auditing.AuditingRecord>>> LoadPendingAsync(System.Threading.CancellationToken cancellationToken = default)
- ValueTask<System.String> PersistAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.IAuditingStore

**类型**：interface

- System.Threading.Tasks.Task SaveAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SaveManyAsync(IReadOnlyCollection<Nerosoft.Euonia.Auditing.AuditingRecord> records, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.IAuditingStoreQuery

**类型**：interface

- Task<System.Int32> DeleteBeforeAsync(System.DateTime cutoff, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Auditing.AuditingRecord> FindByScopeAsync(System.String scopeId, System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyList<Nerosoft.Euonia.Auditing.AuditingRecord>> QueryAsync(Nerosoft.Euonia.Auditing.AuditingQuery query, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.IAuditingStoreRouter

**类型**：interface

- Nerosoft.Euonia.Auditing.IAuditingStore GetStore(Nerosoft.Euonia.Auditing.AuditingRecord record)

### Nerosoft.Euonia.Auditing.IAuditingWriter

**类型**：interface

- System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.ValueTask WriteAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.InMemoryAuditingStore

**类型**：class
接口 Nerosoft.Euonia.Auditing.IAuditingStore、Nerosoft.Euonia.Auditing.IAuditingStoreQuery

- new Nerosoft.Euonia.Auditing.InMemoryAuditingStore()
- IReadOnlyCollection<Nerosoft.Euonia.Auditing.AuditingRecord> Records { get; }
- void Clear()
- Task<System.Int32> DeleteBeforeAsync(System.DateTime cutoff, System.Threading.CancellationToken cancellationToken = default)
- Task<Nerosoft.Euonia.Auditing.AuditingRecord> FindByScopeAsync(System.String scopeId, System.Threading.CancellationToken cancellationToken = default)
- Task<IReadOnlyList<Nerosoft.Euonia.Auditing.AuditingRecord>> QueryAsync(Nerosoft.Euonia.Auditing.AuditingQuery query, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task SaveAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.NullAuditingRecorder

**类型**：class
接口 Nerosoft.Euonia.Auditing.IAuditingRecorder

- Nerosoft.Euonia.Auditing.IAuditingScope CurrentScope { get; }
- System.Boolean IsEnabled { get; }
- static readonly Nerosoft.Euonia.Auditing.NullAuditingRecorder Instance
- void Attach(Nerosoft.Euonia.Auditing.IAuditingScope scope)
- void CompleteInBackground(Nerosoft.Euonia.Auditing.IAuditingScope scope)
- Nerosoft.Euonia.Auditing.IAuditingScope CreateDetachedScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options = null)
- Nerosoft.Euonia.Auditing.IAuditingScope CreateScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options = null)
- void Detach(Nerosoft.Euonia.Auditing.IAuditingScope scope)
- Nerosoft.Euonia.Auditing.IAuditingScope EnsureScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options = null)
- Nerosoft.Euonia.Auditing.IAuditingScope EnsureScope(Nerosoft.Euonia.Auditing.AuditingScopeOptions options, System.Boolean force)
- System.Threading.Tasks.Task FlushAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.ValueTask SubmitAsync(Nerosoft.Euonia.Auditing.AuditingRecord record, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Auditing.SingleAuditingStoreRouter

**类型**：class
接口 Nerosoft.Euonia.Auditing.IAuditingStoreRouter

- new Nerosoft.Euonia.Auditing.SingleAuditingStoreRouter(Nerosoft.Euonia.Auditing.IAuditingStore store)
- Nerosoft.Euonia.Auditing.IAuditingStore GetStore(Nerosoft.Euonia.Auditing.AuditingRecord record)
