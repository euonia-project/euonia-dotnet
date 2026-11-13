namespace Nerosoft.Euonia.Execution;

/// <summary>
/// 跨能力共享的执行身份快照，供租户隔离、审计与遥测只读消费。
/// </summary>
/// <remarks>
/// <para>
/// 该类型只承载“这次执行属于谁”的身份信息，不参与授权判断，也不承载业务数据。
/// 租户过滤、审计字段与遥测标签都从同一个快照读取，避免多套 <see cref="AsyncLocal{T}"/> 产生不一致。
/// </para>
/// <para>
/// 写入方由单写者规则约束：<see cref="ActorTenantId"/>、<see cref="UserId"/>、<see cref="CorrelationId"/>、
/// <see cref="RequestTraceId"/> 由宿主入口（Hosting / Bus）写入；<see cref="TenantId"/>、
/// <see cref="TargetTenantId"/>、<see cref="IsCrossTenant"/> 与 <see cref="CrossTenantReason"/> 由租户作用域写入。
/// 其他代码只读。
/// </para>
/// </remarks>
public sealed class ExecutionContextValues
{
	/// <summary>
	/// 获取或设置当前数据作用域租户标识；全租户跨租户作用域时为 <c>null</c>。
	/// </summary>
	public string? TenantId { get; set; }

	/// <summary>
	/// 获取或设置当前租户显示名称；仅用于诊断与审计展示。
	/// </summary>
	public string? TenantName { get; set; }

	/// <summary>
	/// 获取或设置操作人所属租户标识；登录时确定，不随数据作用域变化。
	/// </summary>
	public string? ActorTenantId { get; set; }

	/// <summary>
	/// 获取或设置跨租户操作显式指定的目标租户标识。
	/// </summary>
	public string? TargetTenantId { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示当前是否处于显式跨租户作用域。
	/// </summary>
	public bool IsCrossTenant { get; set; }

	/// <summary>
	/// 获取或设置跨租户操作的原因；普通作用域为 <c>null</c>。
	/// </summary>
	public string? CrossTenantReason { get; set; }

	/// <summary>
	/// 获取或设置当前用户标识。
	/// </summary>
	public string? UserId { get; set; }

	/// <summary>
	/// 获取或设置当前用户名称。
	/// </summary>
	public string? UserName { get; set; }

	/// <summary>
	/// 获取或设置业务关联标识；跨进程传播，供审计与日志使用。
	/// </summary>
	public string? CorrelationId { get; set; }

	/// <summary>
	/// 获取或设置现有 <c>x-request-trace-id</c> / <c>TraceIdentifier</c> 语义的业务追踪标识。
	/// </summary>
	public string? RequestTraceId { get; set; }

	/// <summary>
	/// 获取或设置扩展槽；键由写入方定义，读取方不得跨键猜测语义。
	/// </summary>
	public IDictionary<string, object?> Items { get; set; } = new Dictionary<string, object?>();

	/// <summary>
	/// 创建当前快照的浅拷贝；<see cref="Items"/> 会被复制为新字典。
	/// </summary>
	/// <returns>与当前实例值相同、但可独立修改的新实例。</returns>
	public ExecutionContextValues Clone()
	{
		return new ExecutionContextValues
		{
			TenantId = TenantId,
			TenantName = TenantName,
			ActorTenantId = ActorTenantId,
			TargetTenantId = TargetTenantId,
			IsCrossTenant = IsCrossTenant,
			CrossTenantReason = CrossTenantReason,
			UserId = UserId,
			UserName = UserName,
			CorrelationId = CorrelationId,
			RequestTraceId = RequestTraceId,
			Items = new Dictionary<string, object?>(Items),
		};
	}
}
