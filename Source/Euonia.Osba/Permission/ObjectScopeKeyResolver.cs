using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 由对象状态推断策略键，实现 <see cref="IScopeKeyResolver"/>。
/// </summary>
/// <remarks>
/// 「资源当前代表哪个操作」是对象模型的知识——可编辑对象有新增/更改/删除状态、
/// 命令对象是 <see cref="ICommandObject"/>、只读对象是 <see cref="IReadOnlyObject"/>。
/// <see cref="ScopeOperationMap"/> 是这套映射的<b>唯一</b>实现，因此本类只做「拿到操作后交给
/// <see cref="ScopeKeyResolver"/> 按键」这一步，不重复实现任何映射。
/// </remarks>
internal sealed class ObjectScopeKeyResolver : IScopeKeyResolver
{
	private readonly ScopeModelRegistry _registry;

	internal ObjectScopeKeyResolver(ScopeModelRegistry registry)
	{
		_registry = registry;
	}

	/// <inheritdoc />
	public string Resolve(object resource, string explicitKey)
	{
		if (!string.IsNullOrWhiteSpace(explicitKey))
		{
			return explicitKey;
		}

		// 对象没有待执行操作（如尚未变更的可编辑对象）时回落到默认键，而不是抛异常：
		// 「还没打算写」不该让读侧断言失败。
		if (resource == null || !ScopeOperationMap.TryResolve(resource, out var operation))
		{
			return ScopeKeys.Default;
		}

		return _registry.TryGetInherited(resource.GetType(), out var registration)
			? ScopeKeyResolver.Resolve(registration, registration.Descriptor.ResourceType, operation, ObjectPermissionCodeSource.Instance)
			: ScopeKeys.For(operation);
	}
}
