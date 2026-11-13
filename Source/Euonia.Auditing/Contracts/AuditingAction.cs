namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 作用域内的一个被审计动作（应用服务方法、消息处理器等）。
/// </summary>
public sealed class AuditingAction
{
	/// <summary>
	/// 获取或设置动作标识（UUID v7）。
	/// </summary>
	public Guid ActionId { get; set; }

	/// <summary>
	/// 获取或设置父动作标识；无嵌套时为 <c>null</c>。
	/// </summary>
	public Guid? ParentActionId { get; set; }

	/// <summary>
	/// 获取或设置幂等键；消息场景使用消息标识。
	/// </summary>
	public string? IdempotencyKey { get; set; }

	/// <summary>
	/// 获取或设置服务名称（通常是接口全名）。
	/// </summary>
	public string? ServiceName { get; set; }

	/// <summary>
	/// 获取或设置方法名称。
	/// </summary>
	public string? MethodName { get; set; }

	/// <summary>
	/// 获取或设置动作显示名称。
	/// </summary>
	public string? ActionName { get; set; }

	/// <summary>
	/// 获取或设置开始执行时间（UTC）。
	/// </summary>
	public DateTime ExecutionTime { get; set; }

	/// <summary>
	/// 获取或设置执行耗时（毫秒）。
	/// </summary>
	public int ExecutionDuration { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示动作是否成功；<c>null</c> 表示未封口。
	/// </summary>
	public bool? IsSuccess { get; set; }

	/// <summary>
	/// 获取或设置脱敏后的参数 JSON。
	/// </summary>
	public string? Parameters { get; set; }

	/// <summary>
	/// 获取或设置脱敏后的结果 JSON。
	/// </summary>
	public string? Result { get; set; }

	/// <summary>
	/// 获取或设置异常类型全名。
	/// </summary>
	public string? ExceptionType { get; set; }

	/// <summary>
	/// 获取或设置异常消息。
	/// </summary>
	public string? ExceptionMessage { get; set; }

	/// <summary>
	/// 获取或设置异常堆栈。
	/// </summary>
	public string? ExceptionStackTrace { get; set; }

	/// <summary>
	/// 获取或设置扩展属性 JSON。
	/// </summary>
	public string? ExtraProperties { get; set; }
}
