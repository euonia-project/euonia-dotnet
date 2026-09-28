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
}
