using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Security;

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
	/// <param name="assemblies">要扫描业务对象类型的程序集数组。</param>
	/// <remarks>
	/// 不注册任何权限服务：Osba 本身不认识任何鉴权实现。
	/// 需要权限时，接引擎用
	/// <c>AddPermission(p =&gt; { p.Scan(assemblies); p.Source(ObjectPermissionCodeSource.Instance); })</c>
	/// （规则来源就用 Osba 的工厂约定），或注册自己的
	/// <c>IPermissionCodeSource</c> / <c>IPermissionChecker</c> / <c>IObjectScopeAuthorizer</c>。
	/// </remarks>
	public static IServiceCollection AddBusinessObject(this IServiceCollection services, params Assembly[] assemblies)
	{
		return AddBusinessObject(services, _ => { }, assemblies);
	}

	/// <summary>
	/// 向指定的 <see cref="IServiceCollection" /> 添加业务对象相关服务，并配置 <see cref="OsbaOptions" />。
	/// </summary>
	/// <param name="services">要注册业务对象服务的 <see cref="IServiceCollection" />。</param>
	/// <param name="configure">选项配置回调。</param>
	/// <param name="assemblies">要扫描业务对象类型的程序集数组。</param>
	/// <returns>同一个 <see cref="IServiceCollection" />，便于链式调用。</returns>
	public static IServiceCollection AddBusinessObject(this IServiceCollection services, Action<OsbaOptions> configure, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);

		var options = new OsbaOptions();
		configure?.Invoke(options);
		options.Assemblies.AddRange(assemblies ?? []);

		// 第一次注册为准；重复调用不会覆盖宿主已经做出的选择。
		services.TryAddSingleton(options);

		// 指标/追踪是进程级静态仪表；以本次装配的选项为准。
		OsbaDiagnostics.Enabled = options.EnableDiagnostics;

		services.TryAddScoped<IActuator, Actuator>();

		// 规则的共享边界是容器，不是进程：每个容器一个注册表，测试/租户/插件互不可见。
		services.TryAddSingleton<IRuleRegistry, RuleRegistry>();

		// 「对象状态 → 业务操作」是对象模型自己的知识，与是否启用鉴权无关：
		// 这里注册一次，任何鉴权实现（引擎或宿主自建）都能直接消费，不必再写适配
		services.TryAddSingleton<IObjectOperationResolver, ObjectOperationResolver>();
		services.TryAddScoped<BusinessContextAccessor>();
		services.TryAddScoped<BusinessContext>();
		services.TryAddScoped<IObjectFactory, BusinessObjectFactory>();

		foreach (var type in BusinessObjectTypeScanner.GetBusinessObjectTypes(assemblies))
		{
			services.TryAddTransient(type);
		}

		// 启动期校验是显式选项：默认关闭，避免测试程序集中的半成品类型让 Host 启动失败。
		if (options.ValidateOnStartup)
		{
			var diagnostics = OsbaStartupValidator.Validate(assemblies, options);
			OsbaStartupValidator.ThrowIfErrors(diagnostics);
		}

		return services;
	}

}