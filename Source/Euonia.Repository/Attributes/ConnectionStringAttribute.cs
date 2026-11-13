namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 指示某个类（通常是数据上下文类型）关联特定的数据库连接字符串。
/// </summary>
/// <remarks>
/// <para>该特性应用于类且不参与继承（<see cref="AttributeUsageAttribute.Inherited"/> 为 <c>false</c>）。</para>
/// <para>
/// 连接字符串按以下优先级解析（从高到低），取第一个非空白结果：
/// </para>
/// <list type="number">
/// <item>配置/环境变量 <c>ConnectionStrings:&lt;Name&gt;</c>（<see cref="Name"/> 对应的键）。</item>
/// <item>配置/环境变量 <c>ConnectionStrings:&lt;数据上下文类型名&gt;</c>。</item>
/// <item>配置/环境变量 <c>ConnectionStrings:Default</c>。</item>
/// <item><see cref="IConnectionStringResolver{TContext}"/> 注册的实现（仅数据上下文工厂使用）。</item>
/// <item><see cref="Value"/>（代码默认值，最后回退，可被配置或环境变量覆盖）。</item>
/// </list>
/// <para>
/// 环境变量通过 <c>__</c> 映射参与前三个配置来源，例如 <c>ConnectionStrings__Default</c> 对应
/// <c>ConnectionStrings:Default</c>；自定义大写单下划线格式同样提供根级连接串，例如
/// <c>CONNECTION_STRING_DEFAULT</c> 与 <c>CONNECTIONSTRINGS_DEFAULT</c> 都映射为 <c>ConnectionStrings:Default</c>
/// （两种拼写同时设置时 <c>CONNECTION_STRING_DEFAULT</c> 获胜）。因此环境变量优先于配置文件与 <see cref="Value"/>。
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ConnectionStringAttribute : Attribute
{
	/// <summary>
	/// 获取或设置连接字符串的名称。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 该名称用于在配置中定位对应的连接字符串，即调用 <c>IConfiguration.GetConnectionString(Name)</c>
	/// 读取 <c>ConnectionStrings:&lt;Name&gt;</c>；这是优先级最高的配置来源（高于数据上下文类型名、<c>Default</c> 与 <see cref="Value"/>）。
	/// </para>
	/// <para>名称可为空；为空时跳过该配置来源，直接按数据上下文类型名、<c>Default</c>、<see cref="Value"/> 依次回退。</para>
	/// </remarks>
	public string? Name { get; set; }

	/// <summary>
	/// 获取或设置连接字符串的具体值。
	/// </summary>
	/// <value>
	/// 连接字符串内容，作为**代码默认值**参与解析：解析顺序中位于配置/环境变量（<see cref="Name"/> 键、数据上下文类型名键、
	/// <c>Default</c>）与 <see cref="IConnectionStringResolver{TContext}"/> 之后，仅在它们都未提供非空白值时使用。
	/// </value>
	public string? Value { get; set; }
}