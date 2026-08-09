namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 消息序列化器的配置选项。
/// 控制序列化过程中对 null 值、引用循环和编码的处理方式。
/// </summary>
public class MessageSerializerOptions
{
	/// <summary>
	/// 获取或设置引用循环的处理策略。
	/// </summary>
	public ReferenceLoopStrategy? ReferenceLoop { get; set; }

	/// <summary>
	/// 获取或设置是否使用构造函数处理。
	/// </summary>
	public bool UseConstructorHandling { get; set; } = true;

	/// <summary>
	/// 获取或设置序列化使用的字符编码。
	/// </summary>
	public Encoding Encoding { get; set; } = Encoding.UTF8;

	/// <summary>
	/// 获取或设置序列化时是否忽略 null 值。
	/// </summary>
	public bool IgnoreNullValues { get; set; } = true;

	/// <summary>
	/// 定义反序列化期间引用循环的处理策略。
	/// </summary>
	public enum ReferenceLoopStrategy
	{
		/// <summary>
		/// 忽略引用循环。
		/// </summary>
		Ignore,

		/// <summary>
		/// 保留引用循环（序列化时保留对象引用关系）。
		/// </summary>
		Preserve,

		/// <summary>
		/// 序列化引用循环（将循环引用序列化为完整对象图）。
		/// </summary>
		Serialize
	}
}