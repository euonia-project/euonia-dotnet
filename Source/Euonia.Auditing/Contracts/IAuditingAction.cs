namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 作用域内的单个动作；纯内存、幂等。
/// </summary>
public interface IAuditingAction
{
	/// <summary>
	/// 获取动作标识。
	/// </summary>
	Guid ActionId { get; }

	/// <summary>
	/// 获取幂等键。
	/// </summary>
	string IdempotencyKey { get; }

	/// <summary>
	/// 设置参数值；写入前会脱敏。
	/// </summary>
	/// <param name="name">参数名称。</param>
	/// <param name="value">参数值。</param>
	void SetParameter(string name, object? value);

	/// <summary>
	/// 设置扩展属性。
	/// </summary>
	/// <param name="name">属性名称。</param>
	/// <param name="value">属性值。</param>
	void SetExtraProperty(string name, object? value);

	/// <summary>
	/// 标记动作成功完成。
	/// </summary>
	/// <param name="result">返回值；不需要记录时为 <c>null</c>。</param>
	void Complete(object? result);

	/// <summary>
	/// 标记动作失败。
	/// </summary>
	/// <param name="exception">导致失败的异常。</param>
	void Fail(Exception exception);
}
