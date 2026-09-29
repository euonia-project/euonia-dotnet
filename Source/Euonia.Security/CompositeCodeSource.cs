namespace Nerosoft.Euonia.Security;

/// <summary>
/// 多个 <see cref="IPermissionCodeSource"/> 的合并：操作取并集，权限码取并集去重。
/// </summary>
internal sealed class CompositeCodeSource : IPermissionCodeSource
{
	private readonly IPermissionCodeSource[] _sources;
	private readonly IReadOnlyList<string> _operations;

	public CompositeCodeSource(IPermissionCodeSource[] sources)
	{
		_sources = sources;

		var operations = new List<string>();

		foreach (var operation in sources.SelectMany(source => source.AllOperations))
		{
			if (operation != null && !operations.Contains(operation, StringComparer.Ordinal))
			{
				operations.Add(operation);
			}
		}

		_operations = Array.AsReadOnly(operations.ToArray());
	}

	/// <inheritdoc />
	/// <remarks>只读快照，无法通过强转回写。</remarks>
	public IReadOnlyList<string> AllOperations => _operations;

	/// <inheritdoc />
	/// <remarks>
	/// 权限码按 <see cref="StringComparer.OrdinalIgnoreCase"/> 去重：两个模块分别声明
	/// <c>repo:delete</c> 与 <c>Repo:Delete</c> 时必须收敛成一个码，否则既绕过去重，
	/// 也让 <see cref="ScopeKeyResolver"/> 的「同一操作最多一个有策略的码」校验误报歧义。
	/// </remarks>
	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		var codes = new List<string>();

		foreach (var source in _sources)
		{
			foreach (var code in source.CodesFor(type, operation))
			{
				if (!string.IsNullOrEmpty(code) && !codes.Contains(code, StringComparer.OrdinalIgnoreCase))
				{
					codes.Add(code);
				}
			}
		}

		// 只读包装：直接返回 List 的话，调用方强转一下就能改动本来源算出的码
		return codes.AsReadOnly();
	}

	/// <inheritdoc />
	/// <remarks>
	/// 逐个来源取要求并去重：只给权限码的来源由接口的默认实现折算成「有码、无角色」，这里不必特判。
	/// 去重键按 <see cref="StringComparer.OrdinalIgnoreCase"/> 比较，与 <see cref="CodesFor"/> 同口径。
	/// </remarks>
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		var requirements = new List<PermissionAttribute>();
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var source in _sources)
		{
			foreach (var requirement in source.RequirementsFor(type, operation))
			{
				// 同一条要求可能被多个来源同时给出（例如模块级来源与全局来源都声明了同一个码）
				if (seen.Add(Describe(requirement)))
				{
					requirements.Add(requirement);
				}
			}
		}

		// 只读包装：与 CodesFor 同口径
		return requirements.AsReadOnly();
	}

	/// <summary>要求的去重键：权限码 + 角色集合。</summary>
	private static string Describe(PermissionAttribute requirement)
	{
		return $"{requirement.Permission}\u001f{string.Join("\u001e", requirement.Roles)}";
	}
}
