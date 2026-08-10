using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 规则基类。
/// </summary>
public abstract class RuleBase : IRuleBase
{
	/// <summary>
	/// 初始化 <see cref="RuleBase"/> 类的新实例。
	/// </summary>
	protected RuleBase()
	{
		Name = GenerateName(GetType());
	}

	/// <summary>
	/// 初始化 <see cref="RuleBase"/> 类的新实例。
	/// </summary>
	/// <param name="property">受规则影响的属性。</param>
	protected RuleBase(IPropertyInfo property)
	{
		Name = GenerateName(GetType(), property.Name);
		Property = property;
	}

	/// <summary>
	/// 初始化 <see cref="RuleBase"/> 类的新实例。
	/// </summary>
	/// <param name="property">受规则影响的属性。</param>
	/// <param name="validationType">验证类型成员信息。</param>
	protected RuleBase(IPropertyInfo property, MemberInfo validationType)
	{
		Name = GenerateName(GetType(), property.Name, validationType.Name);
		Property = property;
	}

	/// <inheritdoc />
	public string Name { get; }

	/// <inheritdoc />
	public IPropertyInfo Property { get; }

	/// <inheritdoc />
	public virtual List<IPropertyInfo> RelatedProperties { get; } = new();

	/// <inheritdoc />
	public int Priority { get; set; }

	/// <summary>
	/// 执行规则检查逻辑。
	/// </summary>
	/// <param name="context">规则上下文。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步规则执行操作的任务。</returns>
	public virtual async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	/// <summary>
	/// 生成规则名称，包含规则类型和附加名称段。
	/// </summary>
	/// <param name="ruleType">规则类型。</param>
	/// <param name="names">附加名称段。</param>
	/// <returns>生成的规则名称。</returns>
	private static string GenerateName(Type ruleType, params string[] names)
	{
		var fullName = $"{ruleType.Namespace}.{ruleType.Name}";

		return GenerateName(fullName, names);
	}

	/// <summary>
	/// 生成规则名称，包含类型名称和附加名称段。
	/// </summary>
	/// <param name="typeName">类型名称。</param>
	/// <param name="names">附加名称段。</param>
	/// <returns>生成的规则名称。</returns>
	private static string GenerateName(string typeName, params string[] names)
	{
		var builder = new StringBuilder($"rule://{typeName}");
		foreach (var name in names)
		{
			builder.Append($"/{name}");
		}

		return builder.ToString().ToLowerInvariant();
	}
}