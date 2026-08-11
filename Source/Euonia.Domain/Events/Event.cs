using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Domain;

/// <summary>
/// 实现 <see cref="IEvent"/> 的抽象类。
/// </summary>
public abstract class Event : IEvent
{
	private const string PROPERTY_ID = "nerosoft.euonia.internal.event.id";

	/// <summary>
	/// 初始化 <see cref="Event"/> 类的新实例，并设置事件意图与唯一标识符。
	/// </summary>
	protected Event()
	{
		var type = GetType();
		EventIntent = type.Name;
		Properties[PROPERTY_ID] = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();
	}

	/// <summary>
	/// 获取事件的扩展属性。
	/// </summary>
	public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

	/// <summary>
	/// 获取或设置具有指定名称的事件属性。
	/// </summary>
	/// <param name="name">属性名称。</param>
	public string this[string name]
	{
		get => Properties.TryGetValue(name, out var value) ? value : default;
		set => Properties[name] = value;
	}

	/// <summary>
	/// 获取指定名称 <paramref name="name"/> 的属性值。
	/// </summary>
	/// <param name="name">属性名称。</param>
	/// <typeparam name="T">属性值的类型。</typeparam>
	/// <returns>转换后的属性值。</returns>
	public virtual T GetProperty<T>(string name)
	{
		return TypeHelper.CoerceValue<T, string>(this[name]);
	}

	/// <summary>
	/// 获取或设置事件标识符。
	/// </summary>
	public string EventId
	{
		get => this[PROPERTY_ID];
		set => this[PROPERTY_ID] = value;
	}

	/// <summary>
	/// 获取或设置当前事件的序号。
	/// </summary>
	public long Sequence { get; set; } = DateTime.UtcNow.Ticks;

	/// <summary>
	/// 获取事件意图。
	/// </summary>
	/// <returns>事件的意图。</returns>
	public virtual string EventIntent { get; set; }

	/// <summary>
	/// 获取事件发起方的 .NET CLR 类型。
	/// </summary>
	/// <returns>事件发起方的 .NET CLR 类型。</returns>
	public virtual string OriginatorType { get; set; }

	/// <summary>
	/// 获取发起方标识符。
	/// </summary>
	/// <returns>发起方标识符。</returns>
	public virtual string OriginatorId { get; set; }
}