using System.Linq.Expressions;
using System.Reflection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 已注册的数据权限模型：资源类型 → 模型描述与策略。
/// </summary>
/// <remarks>
/// <para>
/// 由 <c>AddBusinessObject</c> 在程序集扫描时构建，并在<b>注册期</b>完成校验（见
/// <see cref="Create"/>）。注册表是<b>实例</b>而非进程级静态状态，因此不同的容器/测试之间天然隔离。
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
	/// 获取空注册表。
	/// </summary>
	public static ScopeModelRegistry Empty { get; } = new([]);

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
	/// <exception cref="InvalidOperationException">
	/// 同一资源类型存在多个模型、模型无法实例化、策略引用未映射的维度等情况下抛出。
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
	public static ScopeModelRegistry Create(params Assembly[] assemblies)
	{
		var registrations = new Dictionary<Type, ScopeModelRegistration>();

		foreach (var modelType in GetModelTypes(assemblies))
		{
			ScopeModelDescriptor descriptor;
			IScopeModel modelObject;

			try
			{
				modelObject = (IScopeModel)Activator.CreateInstance(modelType)!;
				descriptor = ScopeModelDescriptor.Create(modelObject);
			}
			catch (Exception exception) when (exception is not InvalidOperationException)
			{
				throw new InvalidOperationException($"无法构建权限模型 '{modelType.FullName}'：{exception.Message}", exception);
			}

			Check.Ensure(
				!registrations.ContainsKey(descriptor.ResourceType),
				"资源类型 '{0}' 存在多个权限模型（{1} 与 {2}）。请确保每个资源类型只声明一个权限模型。",
				descriptor.ResourceType.FullName,
				registrations.TryGetValue(descriptor.ResourceType, out var existing) ? existing.Descriptor.ResourceType.Name : "?",
				modelType.Name);

			var policy = modelObject.PolicyObject;

			Check.Ensure(
				policy != null,
				"权限模型 '{0}' 未提供策略。策略与模型必须写在同一个声明类型里，缺少任何一个都无法通过校验。",
				modelType.Name);

			ValidatePolicy(modelType, descriptor, policy);

			registrations[descriptor.ResourceType] = new ScopeModelRegistration(descriptor, policy);
		}

		return registrations.Count == 0 ? Empty : new ScopeModelRegistry(registrations);
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

	private static IEnumerable<Type> GetModelTypes(IEnumerable<Assembly> assemblies)
	{
		if (assemblies == null)
		{
			yield break;
		}

		foreach (var assembly in assemblies.Where(assembly => assembly != null))
		{
			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException exception)
			{
				types = exception.Types.Where(type => type != null).ToArray();
			}

			foreach (var type in types)
			{
				if (type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(IScopeModel)))
				{
					yield return type;
				}
			}
		}
	}

	/// <summary>
	/// 用探针主体集试编译策略，借此暴露「策略引用了未映射维度」等配置错误。
	/// </summary>
	/// <remarks>
	/// 刻意<b>不用空主体集</b>：空集合会让任何基于 Grant 的策略都退化成恒假，
	/// 从而把「本维度未被授予」误判成「策略结构性恒不放行」。
	/// 这里给每个已声明维度都填一个哨兵值，使策略结构被真实地走一遍。
	/// </remarks>
	private static void ValidatePolicy(Type modelType, ScopeModelDescriptor descriptor, object policy)
	{
		var probe = ScopeSubjectSet.CreateBuilder();
		foreach (var dimension in descriptor.Dimensions)
		{
			probe.Add(dimension, $"__scope_probe__{dimension}");
		}

		var compile = typeof(ScopePolicyCompiler)
		              .GetMethod(nameof(ScopePolicyCompiler.Compile))!
		              .MakeGenericMethod(descriptor.ResourceType);

		try
		{
			var compiled = compile.Invoke(null, [policy, descriptor, probe.Build()]);

			// 注意：只拒绝「恒不放行」的策略。只有拒绝条件的策略（All(Deny(...))）是合法的
			// 拒绝清单语义——其 Allow 恒真，不应被误报。
			var allow = (LambdaExpression)compiled!.GetType().GetProperty(nameof(CompiledScopePolicy<object>.Allow))!.GetValue(compiled)!;

			Check.Ensure(
				allow.Body is not ConstantExpression { Value: false },
				"权限模型 '{0}' 的策略结构性恒不放行（Allow 恒假），通常意味着 Any 之下全是拒绝条件。请确认策略构成。",
				modelType.Name);
		}
		catch (TargetInvocationException exception) when (exception.InnerException != null)
		{
			throw new InvalidOperationException($"权限模型 '{modelType.FullName}' 的策略校验失败：{exception.InnerException.Message}", exception.InnerException);
		}
	}
}
