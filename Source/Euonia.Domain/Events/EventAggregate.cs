namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 事件聚合根。
/// </summary>
public class EventAggregate : IAggregateRoot<string>
{
    /// <inheritdoc />
    public object[] GetKeys()
    {
		return [Id];
    }

    /// <summary>
    /// 获取或设置当前实例的标识符。
    /// </summary>
    public string Id { get; set; }

	/// <summary>
	/// 获取或设置事件标识符。
	/// </summary>
	public string EventId { get; set; }

	/// <summary>
	/// 获取或设置时间戳。
	/// </summary>
	public DateTime Timestamp { get; set; }

	/// <summary>
	/// 获取或设置类型名称。
	/// </summary>
	public string TypeName { get; set; }

	/// <summary>
	/// 获取或设置事件意图。
	/// </summary>
	public string EventIntent { get; set; }

	/// <summary>
	/// 获取或设置发起方类型。
	/// </summary>
	public string OriginatorType { get; set; }

	/// <summary>
	/// 获取或设置发起方标识符。
	/// </summary>
	public string OriginatorId { get; set; }

	/// <summary>
	/// 获取或设置事件负载。
	/// </summary>
	public object EventPayload { get; set; }

	/// <summary>
	/// 获取或设置事件序号。
	/// </summary>
	public long EventSequence { get; set; }

	/// <summary>
	/// 返回表示此实例的 <see cref="string" />。
	/// </summary>
	/// <returns>表示此实例的 <see cref="string" />。</returns>
	public override string ToString() => EventIntent;
}