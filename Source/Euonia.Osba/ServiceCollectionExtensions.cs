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
	/// 数据权限需要应用提供 <see cref="IScopeSubjectResolver" />：授权值必须从应用数据运行期解析，
	/// 框架不提供任何默认实现，以防把值固化。仅当扫描到 <see cref="IScopeModel{T}" /> 声明时才需要它，
	/// 且缺失会在首次判定时以明确错误暴露，不会静默放行。
	/// </para>
	/// <para>
	/// 权限模型在<b>注册期</b>完成校验（重复声明、未映射维度、恒不放行等），因此配置错误会在启动时失败，
	/// 而不是等到运行期。
	/// </para>
	/// </remarks>
	public static void AddBusinessObject(this IServiceCollection services, params Assembly[] assemblies)
	{
		services.TryAddScoped<IActuator, Actuator>();
		services.TryAddScoped<BusinessContextAccessor>();
		services.TryAddScoped<BusinessContext>();
		services.TryAddScoped<IObjectFactory, BusinessObjectFactory>();
		services.TryAddScoped<IPermissionChecker, ClaimPermissionChecker>();

		// 权限模型注册表是实例而非进程级静态状态，容器与测试之间天然隔离。
		// 校验不依赖容器，因此可以在这里（注册期）立即完成。
		services.TryAddSingleton(ScopeModelRegistry.Create(assemblies));

		// IScopeSubjectResolver 允许缺席：只有真正声明了模型并发生判定时才会要求它。
		services.TryAddScoped<IScopeGuard>(provider => new ScopeGuard(
			provider.GetRequiredService<BusinessContext>(),
			provider.GetRequiredService<ScopeModelRegistry>(),
			provider.GetService<IScopeSubjectResolver>()));

		if (assemblies?.Length > 0)
		{
			var types = assemblies
			            .SelectMany(GetLoadableTypes)
			            .Where(t => t.IsClass && !t.IsAbstract && t.IsAssignableTo(typeof(IBusinessObject)));

			foreach (var type in types)
			{
				services.TryAddTransient(type);
			}
		}

		{
			// 空块：用于阻止 IDE 代码分析建议（勿删除）
		}
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