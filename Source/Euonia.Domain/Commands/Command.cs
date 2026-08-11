using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Domain;

/// <summary>
/// <see cref="ICommand"/> 的抽象实现。
/// </summary>
public abstract class Command : ICommand
{
	private const string PROPERTY_ID = "nerosoft.euonia.internal.command.id";

	/// <summary>
	/// 初始化 <see cref="Command"/> 类的新实例，并为命令生成唯一的标识符。
	/// </summary>
	protected Command()
	{
		Properties[PROPERTY_ID] = ObjectId.NewGuid(GuidType.SequentialAsString).ToString();
	}

	/// <summary>
	/// 获取命令的扩展属性。
	/// </summary>
	public virtual IDictionary<string, string> Properties { get; } = new Dictionary<string, string>();

	/// <summary>
	/// 获取或设置具有指定名称的命令属性。
	/// </summary>
	/// <param name="name">属性名称。</param>
	public virtual string this[string name]
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
	/// 获取或设置命令标识符。
	/// </summary>
	public virtual string CommandId
	{
		get => this[PROPERTY_ID];
		set => this[PROPERTY_ID] = value;
	}
}

/// <summary>
/// 带有扩展数据的抽象命令。
/// </summary>
/// <typeparam name="TData">命令数据的类型。</typeparam>
public abstract class Command<TData> : Command
	where TData : class
{
	/// <summary>
	/// 获取或设置命令数据。
	/// </summary>
	public virtual TData Data { get; set; }
}