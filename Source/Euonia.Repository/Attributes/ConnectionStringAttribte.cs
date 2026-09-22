namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 指示某个类（通常是数据上下文类型）关联特定的数据库连接字符串。
/// </summary>
/// <remarks>
/// <para>该特性应用于类且不参与继承（<see cref="AttributeUsageAttribute.Inherited"/> 为 <c>false</c>）。</para>
/// <para>在解析连接字符串时，<see cref="Value"/> 的优先级高于 <see cref="Name"/>：
/// 若 <see cref="Value"/> 非空白则直接使用；否则以 <see cref="Name"/> 作为键从配置中读取连接字符串。</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ConnectionStringAttribute : Attribute
{
	/// <summary>
	/// 获取或设置连接字符串的名称。
	/// </summary>
	/// <remarks>
	/// 该名称用于在配置文件中定位对应的连接字符串；
	/// 当 <see cref="Value"/> 为空时，将使用此名称调用 <c>IConfiguration.GetConnectionString(string)</c>。
	/// </remarks>
	public string Name { get; set; }

	/// <summary>
	/// 获取或设置连接字符串的具体值。
	/// </summary>
	/// <value>连接字符串内容；为空或空白时回退到按 <see cref="Name"/> 从配置中查找。</value>
	public string Value { get; set; }
}