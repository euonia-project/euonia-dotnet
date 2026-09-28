using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Reflection;
using Nerosoft.Euonia.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 权限体系的注册入口：<see cref="AddPermission(IServiceCollection, IPermissionCodeSource, Assembly[])"/> 声明「哪个方法对应哪个业务操作」的规则并给出扫描范围，
/// <see cref="AddPermissionModels"/> 只追加扫描范围。
/// </summary>
/// <remarks>
/// 规则的<b>载体</b>有三种，产出的是同一套规则、走的是同一份注册期校验；
/// 差别只在「规则写在哪里、随什么发布」与表达力（配置不承载自定义谓词与按特性名推导的候选名）：
/// <list type="number">
/// <item><description><b>回调</b>（<see cref="AddPermission(IServiceCollection, Action{OperationCodeSourceBuilder}, Assembly[])"/>）：
/// 推荐用法——规则与代码同源、可导航、编译期可见，模块可以各自贡献自己的规则并自动合并。</description></item>
/// <item><description><b>配置节</b>（<see cref="AddPermission(IServiceCollection, IConfigurationSection, Assembly[])"/>）：
/// 适合「部署期换命名约定」，代价是规则脱离代码（见 README §3.4）。</description></item>
/// <item><description><b>自定义来源</b>（<see cref="AddPermission(IServiceCollection, IPermissionCodeSource, Assembly[])"/>）：
/// 两种载体都表达不了时的扩展点，例如规则来自数据库。</description></item>
/// </list>
/// 无论走哪种载体，都必须在注册处<b>显式</b>给出规则——引擎不猜。确实没有方法级权限码时，
/// 用 <c>EmptyCodeSource.Instance</c> 做断言（<see cref="AddPermission(IServiceCollection, IPermissionCodeSource, Assembly[])"/>，
/// 见 README §3.1）。
/// </remarks>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 注册权限策略引擎的服务，并用回调声明操作入口规则。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="configure">规则声明回调，例如 <c>o =&gt; o.OnAttributeOrName(BusinessOperation.Read, "Order", typeof(FetchAttribute))</c>。</param>
	/// <param name="assemblies">要扫描数据权限模型与权限声明的程序集。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <exception cref="InvalidOperationException">回调没有声明任何规则时抛出（此时引擎无从收集方法级权限码）。</exception>
	/// <remarks>
	/// <para>
	/// 这是声明规则的<b>推荐形态</b>：规则写在代码里，随代码评审与发布，且可以按模块分散声明——
	/// 每次调用贡献一组规则，多次调用按并集合并（见 README §3.2）。
	/// </para>
	/// <para>
	/// 只声明了规则、没有可扫描的模型或权限声明时，引擎照常工作；规则的编译与校验发生在<b>注册处</b>，
	/// 因此写错的规则不会等到运行期才暴露。
	/// </para>
	/// </remarks>
	public static IServiceCollection AddPermission(this IServiceCollection services, Action<OperationCodeSourceBuilder> configure, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configure);

		// 先构造规则再动容器：规则不合法时不应留下半套注册（Build 会拒绝空规则）
		var builder = OperationCodeSource.Create();
		configure(builder);

		var source = builder.Build();
		var setup = GetOrCreateSetup(services);

		setup.Add(source, assemblies);
		Rebuild(services, setup);

		return services;
	}

	/// <summary>
	/// 注册权限策略引擎的服务，并从配置节读取操作入口规则。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="configuration">规则所在的配置节，例如 <c>configuration.GetSection("Permission")</c>。</param>
	/// <param name="assemblies">要扫描数据权限模型与权限声明的程序集；配置里的类型名也在其中解析。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <exception cref="InvalidOperationException">
	/// 配置缺少 <c>Operations</c> 节点、操作下有未知节点、某个操作没有声明任何规则、
	/// 写成了标量而不是数组、含空项，或入口特性类型名无法解析 / 有歧义 / 不是特性时抛出。
	/// 以上全部发生在<b>注册处</b>。
	/// </exception>
	/// <remarks>
	/// <para>
	/// 配置键：<c>Operations</c> 下每个子节点是一个操作，可以给出 <c>Attributes</c>（入口特性类型名数组）
	/// 与 <c>Names</c>（入口方法名数组），二者并存时为「命中其一即为入口」的或语义：
	/// </para>
	/// <code>
	/// {
	///   "Permission": {
	///     "Operations": {
	///       "read":    { "Attributes": ["MyApp.Web.OrderFetchAttribute"], "Names": ["Fetch", "Get"] },
	///       "approve": { "Names": ["Approve", "ApproveAsync"] }
	///     }
	///   }
	/// }
	/// </code>
	/// <para>
	/// <b>与 <see cref="OperationCodeSourceBuilder.OnAttributeOrName"/> 的差别</b>：后者还会按特性名
	/// 推导候选方法名（<c>OrderFetchAttribute</c> → <c>Fetch</c> / <c>FetchAsync</c> / …），配置不推导——
	/// 用配置表达同一条规则要显式写出候选名，否则「按命名」那一半会静默消失。
	/// </para>
	/// <para>
	/// 配置只在注册期读取一次：<b>不订阅变更</b>，改配置不会改变门禁，需重启（或重新注册）。
	/// </para>
	/// <para>
	/// <b>注意</b>：配置驱动的规则意味着「改配置即改鉴权口径」。特性类型与自定义谓词天然属于代码，
	/// 配置只适合承载方法名 / 类型名这类纯数据；请让配置文件与代码走同一套评审与发布流程，
	/// 不要把它当成运维侧的可调开关（见 README §3.4）。
	/// </para>
	/// </remarks>
	public static IServiceCollection AddPermission(this IServiceCollection services, IConfigurationSection configuration, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		// 先读配置再动容器：规则不合法时不应留下半套注册。
		// 类型名在「本次调用已知的扫描范围」里解析——包括此前已累积的程序集，
		// 因此 AddPermissionModels(Order.Assembly) 之后再注册配置节是可以解析到该程序集的类型的。
		var builder = OperationCodeSource.Create();
		ConfigurationRuleBinder.Bind(configuration, builder, KnownAssemblies(services, assemblies));

		var source = builder.Build();
		var setup = GetOrCreateSetup(services);

		setup.Add(source, assemblies);
		Rebuild(services, setup);

		return services;
	}

	/// <summary>本次调用可见的程序集：此前累积的 ∪ 本次传入。</summary>
	/// <remarks>
	/// 只读取累积状态，不创建它——规则不合法时不应留下半套注册。
	/// 代价是「程序集只在之后的调用里传入」这种情况解析不到：配置里的类型名必须出现在**本次或更早**的调用中。
	/// </remarks>
	private static Assembly[] KnownAssemblies(IServiceCollection services, Assembly[] assemblies)
	{
		var known = assemblies ?? [];

		return TryGetSetup(services) is { } setup
			? [.. setup.Assemblies.Concat(known).Distinct()]
			: known;
	}

	/// <summary>
	/// 注册权限策略引擎的服务。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="codeSource">权限码来源；由使用方实现，回答「某类型在某操作上声明了哪些权限码」。</param>
	/// <param name="assemblies">要扫描数据权限模型与权限声明的程序集。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <remarks>
	/// <para>
	/// <paramref name="codeSource"/> <b>是必填项</b>，没有默认实现：「哪个方法对应哪个业务操作」
	/// 取决于使用方的约定，引擎无从推断。确实不使用方法级权限码时，请显式传入
	/// <see cref="EmptyCodeSource.Instance"/>——那是一个断言，而不是默认值（见 README §3.1）。
	/// </para>
	/// <para>
	/// 权限模型在<b>注册期</b>完成校验，配置错误在启动时失败而非运行期（校验项清单见 README §5.6）。
	/// 本方法可多次调用，每次的贡献按并集合并（见 README §3.2）。
	/// </para>
	/// <para>
	/// <see cref="IScopeSubjectResolver"/> 与 <see cref="IScopeKeyResolver"/> 均允许缺席：前者只
	/// 在真正发生判定时才被要求，解析器可在本方法<b>之后</b>注册，故这里只记录是否需要它
	/// （见 <see cref="PermissionSetup"/>）；后者缺席时未显式指定权限码的判定回落到
	/// <see cref="ScopeKeys.Default"/>。判定主体取宿主注册的 <see cref="UserPrincipal"/>，
	/// 它与解析器都由容器构建后的 <c>provider.ValidatePermissionSetup()</c> 检查。
	/// </para>
	/// </remarks>
	public static IServiceCollection AddPermission(this IServiceCollection services, IPermissionCodeSource codeSource, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(codeSource);

		var setup = GetOrCreateSetup(services);

		setup.Add(codeSource, assemblies);
		Rebuild(services, setup);

		return services;
	}

	/// <summary>
	/// 追加要扫描的程序集（数据权限模型与权限声明），不改变权限码来源。
	/// </summary>
	/// <param name="services">要注册权限服务的 <see cref="IServiceCollection"/>。</param>
	/// <param name="assemblies">要扫描数据权限模型与权限声明的程序集。</param>
	/// <returns>原 <paramref name="services"/>，便于链式调用。</returns>
	/// <remarks>
	/// <para>
	/// 用于「模型分散在多个程序集、权限码来源只有一处」的布局：来源照旧由一次
	/// <see cref="AddPermission(IServiceCollection, IPermissionCodeSource, Assembly[])"/> 给出，其余程序集各自用本方法追加（见 README §3.2）。
	/// 程序集按幂等处理，重复传入只扫一次。
	/// </para>
	/// <para>
	/// <b>本方法不提供权限码来源</b>，因此它不是「本应用没有方法级权限码」的断言
	/// （那个断言只能用 <see cref="AddPermission(IServiceCollection, IPermissionCodeSource, Assembly[])"/> 传 <see cref="EmptyCodeSource.Instance"/> 做出，见 README §3.1）。
	/// 若此前从未注册过来源：扫描到的方法级 <see cref="PermissionAttribute"/> 不会参与判定，
	/// 而按权限码声明的策略会被注册期的<b>死策略校验</b>拒绝——两者都不会静默放行。
	/// </para>
	/// </remarks>
	public static IServiceCollection AddPermissionModels(this IServiceCollection services, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);

		var setup = GetOrCreateSetup(services);

		setup.AddAssemblies(assemblies);
		Rebuild(services, setup);

		return services;
	}

	/// <summary>只读取本次注册已累积的状态；尚未注册时返回 <see langword="null"/>。</summary>
	private static PermissionModelSetup TryGetSetup(IServiceCollection services)
	{
		return services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(PermissionModelSetup))
		               ?.ImplementationInstance as PermissionModelSetup;
	}

	/// <summary>
	/// 取得本次注册累积的状态；首次调用时一并注册引擎自身的服务。
	/// </summary>
	private static PermissionModelSetup GetOrCreateSetup(IServiceCollection services)
	{
		if (TryGetSetup(services) is { } existing)
		{
			return existing;
		}

		var setup = new PermissionModelSetup();

		services.AddSingleton(setup);

		services.TryAddScoped<IPermissionChecker, SubjectPermissionChecker>();

		services.TryAddScoped<IScopeGuard>(provider => new ScopeGuard(
			provider.GetRequiredService<UserPrincipal>(),
			provider.GetRequiredService<ScopeModelRegistry>(),
			provider.GetService<IScopeSubjectResolver>(),
			provider.GetService<IScopeKeyResolver>()));

		return setup;
	}

	/// <summary>
	/// 按累积后的来源与程序集重建注册表，并<b>替换</b>（而非追加）三者在本容器中的注册。
	/// </summary>
	/// <remarks>
	/// 重建是逐次调用进行的，因此配置错误在<b>注册处</b>抛出，而不是等到容器构建或首次判定。
	/// 用替换而非追加：多次注册后每种类型只保留一条描述符，容器里不留失效的中间注册表。
	/// </remarks>
	private static void Rebuild(IServiceCollection services, PermissionModelSetup setup)
	{
		var registry = ScopeModelRegistry.Create(setup.CodeSource, [.. setup.Assemblies]);

		services.RemoveAll<ScopeModelRegistry>();
		services.AddSingleton(registry);

		services.RemoveAll<IPermissionCodeSource>();
		services.AddSingleton(setup.CodeSource);

		// 「要求从哪来」也由引擎回答：注册期校验、工厂边界的闸门、宿主框架都问这一个服务，
		// 不必各自写一份形状相同的转换（见 CodeSourceRequirementProvider）
		services.RemoveAll<IPermissionRequirementProvider>();
		services.AddSingleton<IPermissionRequirementProvider>(new CodeSourceRequirementProvider(setup.CodeSource));

		services.RemoveAll<PermissionSetup>();
		services.AddSingleton(new PermissionSetup(setup.HasDeclarations(registry)));
	}

	/// <summary>
	/// 判断给定程序集中是否存在 <see cref="PermissionAttribute"/> 声明或可解析的权限码。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <param name="codeSource">权限码来源。</param>
	/// <returns>存在声明则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
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

}
