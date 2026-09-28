using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 已注册的数据权限模型：资源类型 → 模型描述与策略。
/// </summary>
/// <remarks>
/// <para>
/// 由 <see cref="ScopeModelRegistryBuilder"/> 构建（程序集扫描或程序化注册），并在<b>注册期</b>完成校验。
/// 注册表按容器构建、不含进程级可变状态，因此不同的容器与测试之间天然隔离
/// （共享的 <see cref="Empty"/> 是空表，不可变）。
/// </para>
/// <para>
/// 校验只在「声明了模型」时生效：没有任何 <see cref="IScopeModel{T}"/> 的应用照常启动，
/// 只是全部资源都不受数据权限约束。
/// </para>
/// </remarks>
public sealed class ScopeModelRegistry
{
	private readonly Dictionary<Type, ScopeModelRegistration> _registrations;

	private ScopeModelRegistry(Dictionary<Type, ScopeModelRegistration> registrations)
	{
		_registrations = registrations;
	}

	/// <summary>
	/// 不含任何模型的共享空表（不可变）：<see cref="ScopeModelRegistryBuilder.Build"/> 未注册到模型时返回它，
	/// 此时全部资源都不受数据权限约束。
	/// </summary>
	public static ScopeModelRegistry Empty { get; } = new([]);

	/// <summary>
	/// 已注册模型的资源类型；只含显式注册的类型，实体框架的代理类型（派生类）不在其中，未注册任何模型时为空集合。
	/// 判断某个具体类型是否受约束请用 <see cref="IsDeclared"/>。
	/// </summary>
	public IReadOnlyCollection<Type> ResourceTypes => _registrations.Keys;

	/// <summary>
	/// 是否存在任何已声明的权限模型；为 <see langword="false"/> 时（例如 <see cref="Empty"/>）应用照常启动，
	/// 只是全部资源都不受数据权限约束。
	/// </summary>
	public bool HasDeclarations => _registrations.Count > 0;

	/// <summary>
	/// 从给定程序集中扫描权限模型并完成注册期校验。
	/// </summary>
	/// <param name="codeSource">权限码来源，用于校验策略键解析。见 <see cref="IPermissionCodeSource"/>。</param>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>构建好的注册表。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="codeSource"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ScopeModelValidationException">
	/// 存在配置问题时抛出，携带<b>全部</b>诊断。
	/// </exception>
	/// <remarks>
	/// 校验项清单与各自的修法见 README §5.6。所有问题一次报全
	/// （<see cref="ScopeModelValidationException.Diagnostics"/>），而不是修一个跑一次。
	/// </remarks>
	public static ScopeModelRegistry Create(IPermissionCodeSource codeSource, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(codeSource);

		return new ScopeModelRegistryBuilder()
			.AddFrom(assemblies)
			.Build(codeSource);
	}

	/// <summary>
	/// 尝试获取指定资源类型的注册项。
	/// </summary>
	/// <param name="resourceType">资源类型。</param>
	/// <param name="registration">注册项。</param>
	/// <returns>存在则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	public bool TryGet(Type resourceType, out ScopeModelRegistration registration)
	{
		if (resourceType == null)
		{
			registration = null;
			return false;
		}

		return _registrations.TryGetValue(resourceType, out registration);
	}

	/// <summary>
	/// 判断指定资源类型是否声明了权限模型。
	/// </summary>
	/// <param name="resourceType">资源类型。</param>
	/// <returns>已声明则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>
	/// 沿基类链查找：实体框架的代理类型是派生类，直接按 <c>GetType()</c> 查找会漏。
	/// </remarks>
	public bool IsDeclared(Type resourceType)
	{
		for (var type = resourceType; type != null && type != typeof(object); type = type.BaseType)
		{
			if (_registrations.ContainsKey(type))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 沿基类链查找指定资源类型的注册项。
	/// </summary>
	/// <param name="resourceType">资源类型。</param>
	/// <param name="registration">注册项。</param>
	/// <returns>找到则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	public bool TryGetInherited(Type resourceType, out ScopeModelRegistration registration)
	{
		for (var type = resourceType; type != null && type != typeof(object); type = type.BaseType)
		{
			if (_registrations.TryGetValue(type, out registration))
			{
				return true;
			}
		}

		registration = null;
		return false;
	}

	/// <summary>
	/// 供 <see cref="ScopeModelRegistryBuilder"/> 构建已校验的注册表。
	/// </summary>
	internal static ScopeModelRegistry Create(Dictionary<Type, ScopeModelRegistration> registrations)
	{
		return new(registrations);
	}
}
