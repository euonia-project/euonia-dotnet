namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 遥测 Activity、标签与指标名称约定。
/// </summary>
public static class TelemetryNames
{
	/// <summary>总线入站处理。</summary>
	public const string BusProcess = "euonia.bus.process";

	/// <summary>发件箱重投。</summary>
	public const string BusOutboxDispatch = "euonia.bus.outbox.dispatch";

	/// <summary>收件箱处理。</summary>
	public const string BusInboxProcess = "euonia.bus.inbox.process";

	/// <summary>死信处理。</summary>
	public const string BusDeadLetter = "euonia.bus.deadletter";

	/// <summary>仓储执行。</summary>
	public const string RepositoryExecute = "euonia.repository.execute";

	/// <summary>工作单元提交。</summary>
	public const string UnitOfWorkCommit = "euonia.uow.commit";

	/// <summary>工作单元回滚。</summary>
	public const string UnitOfWorkRollback = "euonia.uow.rollback";

	/// <summary>缓存读取。</summary>
	public const string CacheGet = "euonia.cache.get";

	/// <summary>缓存写入。</summary>
	public const string CacheSet = "euonia.cache.set";

	/// <summary>缓存移除。</summary>
	public const string CacheRemove = "euonia.cache.remove";

	/// <summary>授权决策。</summary>
	public const string SecurityAuthorize = "euonia.security.authorize";

	/// <summary>Quartz 作业执行。</summary>
	public const string QuartzExecute = "euonia.quartz.execute";

	/// <summary>锁获取。</summary>
	public const string LockAcquire = "euonia.lock.acquire";

	/// <summary>租户解析。</summary>
	public const string TenancyResolve = "euonia.tenancy.resolve";

	/// <summary>跨租户作用域。</summary>
	public const string TenancyCross = "euonia.tenancy.cross";

	/// <summary>审计持久化。</summary>
	public const string AuditingPersist = "euonia.auditing.persist";

	/// <summary>消息系统标签键。</summary>
	public const string MessagingSystem = "messaging.system";

	/// <summary>消息操作类型标签键。</summary>
	public const string MessagingOperationType = "messaging.operation.type";

	/// <summary>消息传输通道标签键（http / grpc / rabbitmq / activemq 等）。</summary>
	public const string MessagingTransport = "messaging.transport";

	/// <summary>消息目标标签键。</summary>
	public const string MessagingDestination = "messaging.destination.name";

	/// <summary>消息标识标签键。</summary>
	public const string MessagingMessageId = "messaging.message.id";

	/// <summary>会话标识标签键。</summary>
	public const string MessagingConversationId = "messaging.conversation_id";

	/// <summary>消息类型标签键。</summary>
	public const string MessagingMessageType = "messaging.message.type";

	/// <summary>关联标识标签键。</summary>
	public const string CorrelationId = "euonia.correlation_id";

	/// <summary>请求追踪标识标签键。</summary>
	public const string RequestTraceId = "euonia.request_trace_id";

	/// <summary>结果标签键。</summary>
	public const string Outcome = "euonia.outcome";

	/// <summary>异常类型标签键。</summary>
	public const string ErrorType = "error.type";

	/// <summary>数据租户标签键。</summary>
	public const string TenantId = "tenant.id";

	/// <summary>操作人租户标签键。</summary>
	public const string TenantActorId = "tenant.actor.id";

	/// <summary>跨租户标签键。</summary>
	public const string TenantCross = "tenant.cross";

	/// <summary>跨租户原因标签键。</summary>
	public const string TenantCrossReason = "tenant.cross.reason";

	/// <summary>用户标识标签键；默认关闭。</summary>
	public const string UserId = "euonia.user.id";

	/// <summary>总线处理耗时指标。</summary>
	public const string BusProcessDuration = "euonia.bus.process.duration";

	/// <summary>总线处理失败指标。</summary>
	public const string BusProcessFailures = "euonia.bus.process.failures";

	/// <summary>发件箱积压指标。</summary>
	public const string BusOutboxPending = "euonia.bus.outbox.pending";

	/// <summary>收件箱积压指标。</summary>
	public const string BusInboxPending = "euonia.bus.inbox.pending";

	/// <summary>审计记录数指标。</summary>
	public const string AuditingRecords = "euonia.auditing.records";

	/// <summary>审计丢弃数指标。</summary>
	public const string AuditingRecordsDropped = "euonia.auditing.records.dropped";

	/// <summary>审计写入失败指标。</summary>
	public const string AuditingWriteFailures = "euonia.auditing.write.failures";

	/// <summary>审计写入耗时指标。</summary>
	public const string AuditingWriteDuration = "euonia.auditing.write.duration";
}
