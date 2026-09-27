using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 已注册的数据权限模型：资源类型 → 模型描述与策略。
/// </summary>
/// <remarks>
/// <para>
/// 由 <see cref="ScopeModelRegistryBuilder"/> 构建（程序集扫描或程序化注册），并在<b>注册期</b>完成校验。
/// 注册表是<b>实例</b>而非进程级静态状态，因此不同的容器/测试之间天然隔离。
/// </para>
/// <para>
/// 校验只在「声明了模型」时生效：没有任何 <see cref="IScopeModel{T}"/> 的应用照常启动，
/// 只是全部资源都不受数据权限约束。
/// </para>
/// </remarks>
public sealed class ScopeModelRegistry
{
	private readonly Dictionary<Type, ScopeModelRegistration> _registrations;
	private readonly IPermissionCodeSource _codeSource;

	private ScopeModelRegistry(Dictionary<Type, ScopeModelRegistration> registrations, IPermissionCodeSource codeSource)
	{
		_registrations = registrations;
		_codeSource = codeSource;
	}

	/// <summary>
	/// 获取空注册表。
	/// </summary>
	public static ScopeModelRegistry Empty { get; } = new([], EmptyCodeSource.Instance);

	/// <summary>
	/// 获取已注册的资源类型集合。
	/// </summary>
	public IReadOnlyCollection<Type> ResourceTypes => _registrations.Keys;

	/// <summary>
	/// 获取一个值，指示是否存在任何已声明的权限模型。
	/// </summary>
	public bool HasDeclarations => _registrations.Count > 0;

	/// <summary>
	/// 从给定程序集中扫描权限模型并完成注册期校验。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>构建好的注册表。</returns>
	/// <exception cref="ScopeModelValidationException">
	/// 存在配置问题时抛出，携带<b>全部</b>诊断。
	/// </exception>
	/// <remarks>
	/// 校验项（全部在启动期暴露，避免运行期出现「看似启用了数据权限、实际没有生效」）：
	/// <list type="number">
	/// <item><description>同一资源类型不得有多个权限模型。</description></item>
	/// <item><description>模型必须可实例化（公共无参构造）。</description></item>
	/// <item><description>模型必须至少声明一个维度。</description></item>
	/// <item><description>策略引用的每个维度都必须在模型中映射过——这是「策略写了却没映射 ⇒ 静默放行」的根治点。
	/// 做法是用空主体集试编译一次策略，未映射的维度会在此抛出。</description></item>
	/// </list>
	/// </remarks>
	/// <param name="codeSource">权限码来源，用于校验策略键解析。由对象模型提供，见 <see cref="IPermissionCodeSource"/>。</param>
	/// <exception cref="ArgumentNullException"><paramref name="codeSource"/> 为 <see langword="null"/> 时抛出。</exception>
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
	internal static ScopeModelRegistry Create(Dictionary<Type, ScopeModelRegistration> registrations, IPermissionCodeSource codeSource)
	{
		return new(registrations, codeSource);
	}
}
