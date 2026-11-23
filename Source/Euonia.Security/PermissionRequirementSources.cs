using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 内建的要求来源：直接读写在<b>类型</b>与<b>将被调用的方法</b>上的 <see cref="PermissionAttribute"/>。
/// </summary>
/// <remarks>
/// 忽略 <c>operation</c>：方法级要求由方法决定，与数据权限的操作名无关；
/// 因此它也是「零配置即可生效」的那条路径（读取命中 <see cref="PermissionRequirements"/> 的缓存）。
/// </remarks>
internal sealed class AttributeRequirementSource : IPermissionRequirementSource
{
	/// <summary>
	/// 共享实例（无可变状态）。
	/// </summary>
	internal static AttributeRequirementSource Instance { get; } = new();

	/// <inheritdoc />
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, MethodInfo? method, string operation)
	{
		return PermissionRequirements.For(type, method);
	}
}

/// <summary>
/// 多个要求来源的合并：逐源收集后按「码 + 角色」去重，并保持首次出现顺序。
/// </summary>
/// <remarks>
/// 内建声明、<c>Requirements(...)</c> 登记的自定义来源、以及按 <c>(类型, 操作)</c> 回答的来源的适配器
/// 都在这里取并集：任何一方给出的要求都不会被另一方覆盖，重复项只保留首次出现的那一条。
/// </remarks>
internal sealed class CompositeRequirementSource : IPermissionRequirementSource
{
	/// <summary>
	/// 被组合的来源，按声明顺序参与合并。
	/// </summary>
	private readonly IPermissionRequirementSource[] _sources;

	/// <summary>
	/// 初始化组合来源。
	/// </summary>
	/// <param name="sources">被组合的要求来源；调用方保证非空且不含 <see langword="null"/> 项。</param>
	public CompositeRequirementSource(IPermissionRequirementSource[] sources)
	{
		ArgumentNullException.ThrowIfNull(sources);

		_sources = sources;
	}

	/// <inheritdoc />
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, MethodInfo? method, string operation)
	{
		// 只有一个来源时直接回答：判定路径上不额外分配一层数组
		if (_sources.Length == 1)
		{
			return _sources[0].RequirementsFor(type, method, operation);
		}

		var sets = new IEnumerable<PermissionAttribute>[_sources.Length];

		for (var index = 0; index < _sources.Length; index++)
		{
			sets[index] = _sources[index].RequirementsFor(type, method, operation);
		}

		return PermissionRequirements.Merge(sets);
	}
}
