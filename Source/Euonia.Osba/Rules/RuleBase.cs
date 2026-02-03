using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// The rule base.
/// </summary>
public abstract class RuleBase : IRuleBase
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RuleBase"/> class.
	/// </summary>
	protected RuleBase()
	{
		Name = GenerateName(GetType());
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RuleBase"/> class.
	/// </summary>
	/// <param name="property"></param>
	protected RuleBase(IPropertyInfo property)
	{
		Name = GenerateName(GetType(), property.Name);
		Property = property;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RuleBase"/> class.
	/// </summary>
	/// <param name="property"></param>
	/// <param name="validationType"></param>
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
	/// Execute the rule check logic.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="cancellationToken"></param>
	public virtual async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	private static string GenerateName(Type ruleType, params string[] names)
	{
		var fullName = $"{ruleType.Namespace}.{ruleType.Name}";

		return GenerateName(fullName, names);
	}

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