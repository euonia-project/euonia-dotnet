using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Osba;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于在 <see cref="IServiceCollection" /> 中设置业务对象相关服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 向指定的 <see cref="IServiceCollection" /> 添加业务对象相关服务。
	/// </summary>
	/// <param name="services">要注册业务对象服务的 <see cref="IServiceCollection" />。</param>
	/// <param name="assemblies">要扫描业务对象类型与数据权限模型的程序集数组。</param>
	/// <remarks>
	/// <para>
	/// 权限（操作权限的权限码、数据权限的行级授予）需要应用提供 <see cref="IScopeSubjectResolver" />：
	/// 授权值必须从应用数据运行期解析，框架不提供任何默认实现，以防把值固化——
	/// 固化在令牌里会导致「取消授权后旧令牌仍然有效」。
	/// </para>
	/// <para>
	/// 权限模型在<b>注册期</b>完成校验（重复声明、未映射维度、恒不放行、策略键歧义等），
	/// 因此配置错误会在启动时失败，而不是等到运行期。
	/// </para>
	/// <para>
	/// 解析器本身允许在 <see cref="AddBusinessObject"/> 之后再注册，因此这里只记录「是否需要解析器」
	/// （见 <see cref="PermissionSetup"/>）。应用应在构建容器后调用
	/// <c>provider.ValidatePermissionSetup()</c> 完成启动期检查；
	/// 若遗漏，首次判定时也会以明确错误暴露，不会静默放行。
	/// </para>
	/// </remarks>
	public static void AddBusinessObject(this IServiceCollection services, params Assembly[] assemblies)
	{
		services.TryAddScoped<IActuator, Actuator>();
		services.TryAddScoped<BusinessContextAccessor>();
		services.TryAddScoped<BusinessContext>();
		services.TryAddScoped<IObjectFactory, BusinessObjectFactory>();

		// 权限码来自授权数据，而非令牌声明（见 SubjectPermissionChecker 的备注）
		services.TryAddScoped<IPermissionChecker, SubjectPermissionChecker>();

		// 权限模型注册表是实例而非进程级静态状态，容器与测试之间天然隔离。
		// 校验不依赖容器，因此可以在这里（注册期）立即完成。
		var registry = ScopeModelRegistry.Create(assemblies);
		services.TryAddSingleton(registry);

		var businessObjectTypes = GetBusinessObjectTypes(assemblies);

		services.TryAddSingleton(new PermissionSetup(registry.HasDeclarations || HasPermissionDeclarations(businessObjectTypes)));

		// IScopeSubjectResolver 允许缺席：只有真正声明了模型或权限码并发生判定时才会要求它。
		services.TryAddScoped<IScopeGuard>(provider => new ScopeGuard(
			provider.GetRequiredService<BusinessContext>(),
			provider.GetRequiredService<ScopeModelRegistry>(),
			provider.GetService<IScopeSubjectResolver>()));

		foreach (var type in businessObjectTypes)
		{
			services.TryAddTransient(type);
		}

		{
			// 空块：用于阻止 IDE 代码分析建议（勿删除）
		}
	}

	/// <summary>
	/// 判断给定类型中是否存在 <see cref="PermissionAttribute" /> 声明。
	/// </summary>
	/// <param name="types">业务对象类型。</param>
	/// <returns>存在声明则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>
	/// 只检查类型级声明与工厂方法上的声明：只要出现权限码，就需要解析器提供「用户持有哪些码」。
	/// </remarks>
	private static bool HasPermissionDeclarations(IEnumerable<Type> types)
	{
		foreach (var type in types)
		{
			if (type.GetCustomAttributes<PermissionAttribute>(true).Any())
			{
				return true;
			}

			foreach (var operation in PermissionRequirements.AllOperations)
			{
				if (PermissionRequirements.CodesFor(type, operation).Count > 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// 扫描程序集中的业务对象类型。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>可实例化的业务对象类型序列。</returns>
	private static IReadOnlyList<Type> GetBusinessObjectTypes(Assembly[] assemblies)
	{
		if (assemblies?.Length is not > 0)
		{
			return Array.Empty<Type>();
		}

		return assemblies
		       .SelectMany(GetLoadableTypes)
		       .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(IBusinessObject)))
		       .ToArray();
	}

	/// <summary>
	/// 获取程序集中可加载的类型，跳过因依赖缺失而无法加载的类型，避免 <see cref="ReflectionTypeLoadException"/>。
	/// </summary>
	/// <param name="assembly">目标程序集。</param>
	/// <returns>可加载的类型序列。</returns>
	private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
	{
		try
		{
			return assembly.GetTypes();
		}
		catch (ReflectionTypeLoadException ex)
		{
			return ex.Types.Where(t => t != null);
		}
	}
}