using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Reflection;
using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 权限体系的注册入口：一个 <c>AddPermission</c> 回调声明扫描范围、操作入口规则与断言，
/// 据此构建模型注册表与判定服务。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 注册权限体系：扫描范围、操作入口规则、断言都在同一个回调里声明。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="configure">配置回调，例如 <c>p =&gt; { p.Scan(typeof(Order).Assembly); p.OnAttributeOrName(…); }</c>。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <exception cref="InvalidOperationException">配置自相矛盾时抛出（如既断言无码又声明了入口规则、既断言无模型又传入了扫描程序集）。</exception>
	/// <remarks>
	/// <para>
	/// 这是权限体系的<b>唯一入口</b>。规则、扫描范围与断言在同一处声明，
	/// 注册期即完成模型扫描与全部校验——配置错误在启动时失败而非运行期。
	/// </para>
	/// <para>
	/// 使用了权限（方法级权限码或数据权限模型）时，应用还需注册
	/// <see cref="IScopeSubjectResolver"/>（从授权数据实时解析授权值）与 <see cref="UserPrincipal"/>
	/// （判定主体），并调用 <c>provider.ValidatePermissionSetup()</c> 使缺漏在启动时暴露。
	/// </para>
	/// </remarks>
	public static IServiceCollection AddPermission(this IServiceCollection services, Action<PermissionOptions> configure)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configure);

		var options = new PermissionOptions();
		configure(options);
		Validate(options);

		// 什么都不说不是合法配置：没有任何规则，方法级 [Permission] 无从收集；
		// 没有扫描范围也没有断言，行级数据权限会静默失效。两种「说清楚意图」的方式都拒绝空回调。
		Check.Ensure(
			options.Rules != null || options.NoCodesAsserted || options.NoModelsAsserted || options.Assemblies.Count > 0,
			Resources.IDS_PERMISSION_CONFIG_EMPTY);

		var setup = GetOrCreateSetup(services);
		Apply(services, setup, options);
		return services;
	}

	/// <summary>
	/// 注册权限体系并从配置节读取操作入口规则。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="configuration">规则所在的配置节，例如 <c>configuration.GetSection("Permission")</c>。</param>
	/// <param name="assemblies">要扫描的程序集；配置里的入口特性类型名也在其中解析。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <exception cref="InvalidOperationException">
	/// 配置缺少 <c>Operations</c> 节点、操作下有未知节点、某个操作没有声明任何规则、
	/// 写成标量而不是数组、含空项，或入口特性类型名无法解析 / 有歧义 / 不是特性时抛出。
	/// </exception>
	/// <remarks>
	/// 配置键：<c>Operations</c> 下每个子节点是一个操作，可给出 <c>Attributes</c>（入口特性类型名数组）
	/// 与 <c>Names</c>（入口方法名数组），二者并存时为「命中其一即为入口」的或语义。配置只在注册期读取一次。
	/// </remarks>
	public static IServiceCollection AddPermission(this IServiceCollection services, IConfigurationSection configuration, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		var options = new PermissionOptions().Scan(assemblies ?? []);
		ConfigurationRuleBinder.Bind(configuration, options.RulesBuilder(), KnownAssemblies(services, assemblies));

		var setup = GetOrCreateSetup(services);
		Apply(services, setup, options);
		return services;
	}

	/// <summary>
	/// 注册权限体系并使用给定的权限码来源。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="codeSource">权限码来源，回答「某类型在某操作上声明了哪些权限码」。</param>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <remarks>
	/// 规则不在代码也不在配置里（例如来自数据库）时的扩展点。
	/// </remarks>
	public static IServiceCollection AddPermission(this IServiceCollection services, IPermissionCodeSource codeSource, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(codeSource);

		var options = new PermissionOptions().Scan(assemblies ?? []);
		var setup = GetOrCreateSetup(services);

		Apply(services, setup, options, codeSource);
		return services;
	}

	/// <summary>
	/// 校验配置的自洽性。
	/// </summary>
	private static void Validate(PermissionOptions options)
	{
		var hasRules = options.Rules is { } rules && OperationCodeSource.HasDeclarations(rules);

		Check.Ensure(
			!(options.NoCodesAsserted && hasRules),
			Resources.IDS_PERMISSION_CONFIG_CONTRADICTS_CODES);

		Check.Ensure(
			!(options.NoModelsAsserted && options.Assemblies.Count > 0),
			Resources.IDS_PERMISSION_CONFIG_CONTRADICTS_MODELS);

		Check.Ensure(
			!(options.ExplicitSource != null && options.Rules != null),
			Resources.IDS_PERMISSION_CONFIG_CONTRADICTS_SOURCE);
	}

	/// <summary>
	/// 把一份配置落到容器：累积来源与程序集，重建模型注册表，并注册引擎自身的服务。
	/// </summary>
	/// <remarks>
	/// 重建只发生在本调用内（累积状态由 <see cref="PermissionModelSetup"/> 保存），
	/// 因此配置错误在注册处抛出而不是等到容器构建或首次判定。
	/// </remarks>
	private static void Apply(IServiceCollection services, PermissionModelSetup setup, PermissionOptions options, IPermissionCodeSource explicitSource = null)
	{
		// 来源三选一：显式传入或 Source() 指定 > 配置/回调产出的规则 > 无码断言
		IPermissionCodeSource source = explicitSource ?? options.ExplicitSource;
		if (source == null && options.Rules != null)
		{
			source = options.Rules.Build();
		}

		if (source == null && options.NoCodesAsserted)
		{
			source = EmptyCodeSource.Instance;
		}

		var nextSource = source ?? setup.CodeSource;
		var assemblies = setup.Assemblies.Concat(options.Assemblies).Distinct().ToArray();

		if (options.NoModelsAsserted)
		{
			setup.NoModelsAsserted = true;
		}

		if (source != null)
		{
			setup.AddSource(source);
		}

		setup.AddAssemblies([.. options.Assemblies]);

		if (options.Operations.Count > 0)
		{
			setup.AddOperations(options.Operations);
		}

		RegisterEngine(services);

		// 输入没变时复用上一次的注册表：全量构建要重扫程序集并逐条编译校验，重复注册应当是空操作
		ScopeModelRegistry registry;
		if (setup.SameAsLastBuild(nextSource, assemblies))
		{
			registry = setup.LastRegistry;
		}
		else
		{
			registry = ScopeModelRegistry.Create(setup.CodeSource, [.. setup.Assemblies]);
			setup.LastRegistry = registry;
		}

		services.RemoveAll<ScopeModelRegistry>();
		services.AddSingleton(registry);

		services.RemoveAll<IPermissionCodeSource>();
		services.AddSingleton(setup.CodeSource);

		// 两个契约由引擎实现自己那一半（TryAdd：宿主可换成自己的实现）；契约在 Core，无需适配包。
		// 工厂委托延迟解析：注册表在容器里只保留最新一份，契约解析时取到的必然是它。
		services.TryAddSingleton<IObjectScopeAuthorizer>(provider => new ObjectScopeAuthorizer(
			provider.GetRequiredService<ScopeModelRegistry>(),
			provider.GetRequiredService<IPermissionCodeSource>()));
		services.TryAddSingleton<IScopeKeyResolver>(provider => new ObjectScopeKeyResolver(
			provider.GetRequiredService<ScopeModelRegistry>(),
			provider.GetRequiredService<IPermissionCodeSource>(),
			provider.GetService<IObjectOperationResolver>()));

		services.RemoveAll<PermissionSetup>();
		services.AddSingleton(new PermissionSetup(setup.HasDeclarations(registry)));
	}

	/// <summary>
	/// 注册引擎自身的判定与守卫服务（幂等：只在首次注册时执行）。
	/// </summary>
	private static void RegisterEngine(IServiceCollection services)
	{
		services.TryAddScoped<IPermissionChecker, SubjectPermissionChecker>();
		services.TryAddScoped<IScopeGuard>(provider =>
		{
			// 首次解析守卫时执行启动期校验：解析器、判定主体、扫描范围的缺漏
			// 在此刻暴露，而不是等到首次权限判定甚至静默失效。
			// 手动调用 ValidatePermissionSetup 的步骤由此取消。
			ValidateSetup(provider);

			return new ScopeGuard(
							provider.GetRequiredService<UserPrincipal>(),
							provider.GetRequiredService<ScopeModelRegistry>(),
							provider.GetService<IScopeSubjectResolver>(),
							provider.GetService<IScopeKeyResolver>());
		});
	}

	/// <summary>
	/// 校验权限体系的依赖是否齐备；缺失即抛出，使配置错误在首次使用权限体系时暴露。
	/// 未经 <c>AddPermission</c> 启用权限体系时不做任何检查。
	/// </summary>
	/// <param name="provider">已构建的服务提供程序。</param>
	/// <exception cref="InvalidOperationException">
	/// 声明了权限模型或使用了 <see cref="PermissionAttribute"/>，却未注册 <see cref="IScopeSubjectResolver"/>
	/// 或 <see cref="UserPrincipal"/> 时抛出；<c>AddPermission</c> 未指定任何程序集、
	/// 也未用 <c>AssertNoPermissionModels()</c> 显式断言时同样抛出。
	/// </exception>
	private static void ValidateSetup(IServiceProvider provider)
	{
		var setup = provider.GetService<PermissionSetup>();

		if (setup == null)
		{
			return;
		}

		// 零程序集扫描必须显式断言（与 EmptyCodeSource 同一条规则：空输入不是默认值）。
		// 这一条刻意排在 RequiresSubjectResolver 短路之前——零程序集时它恒为 false，
		// 放在后面就永远检查不到，行级权限会静默失效。
		var modelSetup = provider.GetService<PermissionModelSetup>();

		Check.Ensure(
				modelSetup == null || modelSetup.NoModelsAsserted || modelSetup.Assemblies.Count > 0,
				Resources.IDS_PERMISSION_NO_ASSEMBLY_SCANNED);

		if (setup.RequiresSubjectResolver != true)
		{
			return;
		}

		Check.Ensure(
				provider.GetService<IScopeSubjectResolver>() != null,
				Resources.IDS_PERMISSION_SUBJECT_RESOLVER_NOT_REGISTERED,
				nameof(IScopeSubjectResolver));

		// 缺用户主体不算「声明了却没接数据源」，但同样值得在守卫解析时说清：
		// 否则表现为「所有人被拒」，极易被误判成策略写错。
		Check.Ensure(
				provider.GetService<UserPrincipal>() != null,
				Resources.IDS_PERMISSION_USER_PRINCIPAL_NOT_REGISTERED,
				nameof(UserPrincipal));
	}

	/// <summary>本次调用可见的程序集：已累积的 ∪ 本次传入；供配置里的类型名解析使用。</summary>
	private static Assembly[] KnownAssemblies(IServiceCollection services, Assembly[] assemblies)
	{
		var known = assemblies ?? [];

		return TryGetSetup(services) is { } setup
				? [.. setup.Assemblies.Concat(known).Distinct()]
				: known;
	}

	/// <summary>只读取已累积的状态；尚未注册时返回 <see langword="null"/>。</summary>
	private static PermissionModelSetup TryGetSetup(IServiceCollection services)
	{
		return services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(PermissionModelSetup))
					   ?.ImplementationInstance as PermissionModelSetup;
	}

	/// <summary>
	/// 取得累积状态；首次调用时创建并登记。
	/// </summary>
	private static PermissionModelSetup GetOrCreateSetup(IServiceCollection services)
	{
		if (TryGetSetup(services) is { } existing)
		{
			return existing;
		}

		var setup = new PermissionModelSetup();
		services.AddSingleton(setup);
		return setup;
	}

	/// <summary>
	/// 判断给定程序集中是否存在权限声明（类型级 <see cref="PermissionAttribute"/> 或可解析的权限码）。
	/// </summary>
	internal static bool HasPermissionDeclarations(IEnumerable<Assembly> assemblies, IPermissionCodeSource codeSource)
	{
		foreach (var type in AssemblyHelper.LoadTypes(assemblies))
		{
			if (!type.IsClass || type.IsAbstract)
			{
				continue;
			}

			if (type.GetCustomAttributes<PermissionAttribute>(true).Any())
			{
				return true;
			}

			foreach (var operation in codeSource.AllOperations)
			{
				if (codeSource.CodesFor(type, operation).Count > 0)
				{
					return true;
				}
			}
		}

		return false;
	}

	/// <summary>
	/// 追加要扫描的程序集（数据权限模型与权限声明），不改变权限码来源。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <remarks>
	/// 用于「模型分散在多个程序集、权限码来源只有一处」的布局。
	/// 本方法不提供来源：若尚无任何来源，方法级 <see cref="PermissionAttribute"/> 不参与判定，
	/// 按权限码声明的策略会被注册期死策略校验拒绝——两者都不会静默放行。
	/// </remarks>
	public static IServiceCollection AddPermissionModels(this IServiceCollection services, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);

		var setup = GetOrCreateSetup(services);
		setup.AddAssemblies(assemblies ?? []);

		RegisterEngine(services);

		var registry = ScopeModelRegistry.Create(setup.CodeSource, [.. setup.Assemblies]);
		services.RemoveAll<ScopeModelRegistry>();
		services.AddSingleton(registry);

		services.RemoveAll<IPermissionCodeSource>();
		services.AddSingleton(setup.CodeSource);

		services.RemoveAll<PermissionSetup>();
		services.AddSingleton(new PermissionSetup(setup.HasDeclarations(registry)));

		return services;
	}

	/// <summary>
	/// 断言本应用没有任何权限模型与权限声明；仅用于没有任何扫描范围的场景。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <remarks>
	/// <c>AddPermission</c> 全程一个程序集都没给时，扫描范围为空会让行级数据权限静默失效，
	/// 启动期校验也会因「没有声明」一并短路。调用本方法即等于说「这是有意的」，校验据此放行。
	/// </remarks>
	public static IServiceCollection AssertNoPermissionModels(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		GetOrCreateSetup(services).NoModelsAsserted = true;
		return services;
	}
}
