namespace Nerosoft.Euonia.Security;

/// <summary>
/// 多个 <see cref="IPermissionCodeSource"/> 的合并：操作取并集，权限码取并集去重。
/// </summary>
internal sealed class CompositeCodeSource : IPermissionCodeSource
{
	private readonly IPermissionCodeSource[] _sources;
	private readonly string[] _operations;

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

		_operations = operations.ToArray();
	}

	/// <inheritdoc />
	public IReadOnlyList<string> AllOperations => _operations;

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		var codes = new List<string>();

		foreach (var source in _sources)
		{
			foreach (var code in source.CodesFor(type, operation))
			{
				if (!string.IsNullOrEmpty(code) && !codes.Contains(code, StringComparer.Ordinal))
				{
					codes.Add(code);
				}
			}
		}

		return codes;
	}

	/// <inheritdoc />
	/// <remarks>
	/// 逐个来源取要求并去重：只给权限码的来源由接口的默认实现折算成「有码、无角色」，这里不必特判。
	/// </remarks>
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		var requirements = new List<PermissionAttribute>();
		var seen = new HashSet<string>(StringComparer.Ordinal);

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

		return requirements;
	}

	/// <summary>要求的去重键：权限码 + 角色集合。</summary>
	private static string Describe(PermissionAttribute requirement)
	{
		return $"{requirement.Permission}\u001f{string.Join("\u001e", requirement.Roles)}";
	}
}
