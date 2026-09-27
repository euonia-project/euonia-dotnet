using System.Linq.Expressions;
using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// <see cref="ScopeModelRegistry"/> 的构造器：程序化地声明要注册哪些权限模型。
/// </summary>
/// <remarks>
/// <para>
/// 程序集扫描是<b>默认</b>路径（<see cref="AddFrom(Assembly[])"/>），
/// 但不是唯一路径。模型也可以由<b>实例</b>提供——适合模型需要构造参数、
/// 或需要按配置动态生成的宿主。
/// </para>
/// <para>
/// 两条路径走<b>同一套校验</b>：<see cref="Build"/> 是唯一的校验入口，
/// 因此「扫描进来的模型」与「手动注册的模型」不存在校验宽严差异。
/// </para>
/// </remarks>
public sealed class ScopeModelRegistryBuilder
{
	/// <summary>待注册的模型：名字（仅用于诊断）与工厂。</summary>
	private readonly List<(string Name, Func<IScopeModel> Factory)> _entries = [];

	/// <summary>
	/// 注册一个模型实例。
	/// </summary>
	/// <param name="model">权限模型。</param>
	/// <returns>当前构造器，便于链式声明。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="model"/> 为 <see langword="null"/> 时抛出。</exception>
	public ScopeModelRegistryBuilder Add(IScopeModel model)
	{
		ArgumentNullException.ThrowIfNull(model);

		var captured = model;
		_entries.Add((model.GetType().Name, () => captured));

		return this;
	}

	/// <summary>
	/// 注册一个模型类型（要求公共无参构造）。
	/// </summary>
	/// <typeparam name="TModel">权限模型类型。</typeparam>
	/// <returns>当前构造器，便于链式声明。</returns>
	public ScopeModelRegistryBuilder Add<TModel>()
		where TModel : IScopeModel, new()
	{
		_entries.Add((typeof(TModel).Name, static () => new TModel()));

		return this;
	}

	/// <summary>
	/// 扫描给定程序集并注册其中发现的全部权限模型。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>当前构造器，便于链式声明。</returns>
	/// <remarks>
	/// 重复注册不会互相覆盖：同一资源类型出现两个模型是<b>配置错误</b>，
	/// 会在 <see cref="Build"/> 阶段作为诊断报出，而不是「后者胜出」。
	/// </remarks>
	public ScopeModelRegistryBuilder AddFrom(params Assembly[] assemblies)
	{
		foreach (var modelType in GetModelTypes(assemblies))
		{
			_entries.Add((modelType.Name, () => (IScopeModel)Activator.CreateInstance(modelType)!));
		}

		return this;
	}

	/// <summary>
	/// 完成注册期校验并构建注册表。
	/// </summary>
	/// <param name="codeSource">权限码来源，用于校验策略键解析。</param>
	/// <returns>构建好的注册表；没有任何模型时返回 <see cref="ScopeModelRegistry.Empty"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="codeSource"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ScopeModelValidationException">存在任何配置问题时抛出，携带<b>全部</b>诊断。</exception>
	/// <remarks>
	/// 校验项（全部在启动期暴露，避免运行期出现「看似启用了数据权限、实际没有生效」）：
	/// <list type="number">
	/// <item><description>同一资源类型不得有多个权限模型。</description></item>
	/// <item><description>模型必须可实例化（公共无参构造）。</description></item>
	/// <item><description>模型必须至少声明一个维度。</description></item>
	/// <item><description>模型必须提供策略（策略与模型写在同一类型里，结构上不可能缺一个）。</description></item>
	/// <item><description>策略引用的每个维度都必须在模型中映射过——这是「策略写了却没映射 ⇒ 静默放行」的根治点。
	/// 做法是用探针主体集试编译一次策略，未映射的维度会在此报错。</description></item>
	/// <item><description>策略不得结构性恒不放行（<c>Any</c> 之下全是拒绝条件）。</description></item>
	/// <item><description>同一操作解析出的「声明了行级策略的权限码」至多一个，不允许靠猜生效哪一个。</description></item>
	/// <item><description>不得有死策略：声明了行级策略却没有任何操作解析到该码。</description></item>
	/// </list>
	/// </remarks>
	public ScopeModelRegistry Build(IPermissionCodeSource codeSource)
	{
		ArgumentNullException.ThrowIfNull(codeSource);

		var problems = new List<ScopeModelDiagnostic>();
		var registrations = new Dictionary<Type, ScopeModelRegistration>();

		foreach (var (name, factory) in _entries)
		{
			IScopeModel model;

			try
			{
				model = factory();
			}
			catch (Exception exception)
			{
				problems.Add(new ScopeModelDiagnostic(name, $"无法实例化权限模型：{exception.Message}"));
				continue;
			}

			ScopeModelDescriptor descriptor;

			try
			{
				descriptor = ScopeModelDescriptor.Create(model);
			}
			catch (Exception exception)
			{
				problems.Add(new ScopeModelDiagnostic(name, exception.Message));
				continue;
			}

			if (registrations.TryGetValue(descriptor.ResourceType, out var existing))
			{
				problems.Add(new ScopeModelDiagnostic(
					name,
					$"资源类型 '{descriptor.ResourceType.FullName}' 存在多个权限模型（另有 '{existing.Model.GetType().Name}'）。请确保每个资源类型只声明一个权限模型。"));
				continue;
			}

			var registration = new ScopeModelRegistration(descriptor, model);
			var modelProblems = ValidateModel(name, registration, codeSource);

			if (modelProblems.Count > 0)
			{
				problems.AddRange(modelProblems);
				continue;
			}

			registrations[descriptor.ResourceType] = registration;
		}

		if (problems.Count > 0)
		{
			throw new ScopeModelValidationException(problems);
		}

		return registrations.Count == 0
			? ScopeModelRegistry.Empty
			: ScopeModelRegistry.Create(registrations, codeSource);
	}

	/// <summary>
	/// 校验单个模型，返回该模型的全部问题（不抛异常，以便聚合）。
	/// </summary>
	private static List<ScopeModelDiagnostic> ValidateModel(string name, ScopeModelRegistration registration, IPermissionCodeSource codeSource)
	{
		var problems = new List<ScopeModelDiagnostic>();

		var policy = registration.Model.PolicyObject;

		if (policy == null)
		{
			problems.Add(new ScopeModelDiagnostic(
				name,
				"未提供策略。策略与模型必须写在同一个声明类型里，缺少任何一个都无法通过校验。"));
		}
		else
		{
			Collect(problems, name, TryValidatePolicy(name, registration.Descriptor, policy, ScopeKeys.Default));
		}

		// 逐码校验行级策略：未映射维度、恒不放行等问题都要在启动期暴露
		foreach (var code in registration.DeclaredCodes)
		{
			var scopedPolicy = registration.Model.PolicyFor(code);

			if (scopedPolicy == null)
			{
				problems.Add(new ScopeModelDiagnostic(name, $"声明了权限码 '{code}' 但未提供策略。"));
				continue;
			}

			Collect(problems, name, TryValidatePolicy(name, registration.Descriptor, scopedPolicy, code));
		}

		Collect(problems, name, ValidateKeyResolution(name, registration, codeSource));

		return problems;

		static void Collect(List<ScopeModelDiagnostic> target, string name, string problem)
		{
			if (problem != null)
			{
				target.Add(new ScopeModelDiagnostic(name, problem));
			}
		}
	}

	/// <summary>
	/// 校验每个操作都能解析出唯一的策略键，并检出死策略。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 同一操作若解析出多个「声明了行级策略」的权限码，属配置歧义——不允许「实际生效的是哪一个」靠猜。
	/// </para>
	/// <para>
	/// 同时检出「死策略」：声明了策略却没有任何操作会解析到该码。多半是 <c>Declare</c> 里的码与方法上
	/// <c>[Permission]</c> 的码对不上——那样行级策略会静默失效并回落到默认策略，很可能比作者本意更宽松。
	/// </para>
	/// </remarks>
	private static string ValidateKeyResolution(string name, ScopeModelRegistration registration, IPermissionCodeSource codeSource)
	{
		var resolved = new HashSet<string>(StringComparer.Ordinal);

		foreach (var operation in codeSource.AllOperations)
		{
			// 解析歧义会抛 InvalidOperationException，消息里带类型、操作与冲突的码
			try
			{
				resolved.Add(ScopeKeyResolver.Resolve(registration, registration.Descriptor.ResourceType, operation, codeSource));
			}
			catch (InvalidOperationException exception)
			{
				return exception.Message;
			}
		}

		foreach (var code in registration.DeclaredCodes)
		{
			// 框架保留键（@read/@create/…）由操作直接解析，不需要声明方匹配
			if (ScopeKeys.IsReserved(code) || resolved.Contains(code))
			{
				continue;
			}

			return $"为权限码 '{code}' 声明了行级策略，但没有任何操作会解析到该码"
			       + $"（请核对方法上 [Permission] 的码与 Declare 里的码是否一致）。已解析到的码：{(resolved.Count == 0 ? "（无）" : string.Join(", ", resolved))}。";
		}

		return null;
	}

	/// <summary>
	/// 用探针主体集试编译策略，借此暴露「策略引用了未映射维度」等配置错误。
	/// </summary>
	/// <remarks>
	/// 刻意<b>不用空主体集</b>：空集合会让任何基于 Grant 的策略都退化成恒假，
	/// 从而把「本维度未被授予」误判成「策略结构性恒不放行」。
	/// 这里给每个已声明维度都填一个哨兵值，使策略结构被真实地走一遍。
	/// </remarks>
	/// <returns>问题描述；无问题返回 <see langword="null"/>。</returns>
	private static string TryValidatePolicy(string name, ScopeModelDescriptor descriptor, object policy, string scopeKey)
	{
		var probe = ScopeSubjectSet.CreateBuilder();
		foreach (var dimension in descriptor.Dimensions)
		{
			probe.Add(dimension, $"__scope_probe__{dimension}");
		}

		var compile = typeof(ScopePolicyCompiler)
		              .GetMethod(nameof(ScopePolicyCompiler.Compile))!
		              .MakeGenericMethod(descriptor.ResourceType);

		object compiled;

		try
		{
			compiled = compile.Invoke(null, [policy, descriptor, probe.Build(), scopeKey])!;
		}
		catch (TargetInvocationException exception) when (exception.InnerException != null)
		{
			return exception.InnerException.Message;
		}
		catch (Exception exception)
		{
			return exception.Message;
		}

		// 注意：只拒绝「恒不放行」的策略。只有拒绝条件的策略（All(Deny(...))）是合法的
		// 拒绝清单语义——其 Allow 恒真，不应被误报。
		var allow = (LambdaExpression)compiled.GetType().GetProperty(nameof(CompiledScopePolicy<object>.Allow))!.GetValue(compiled)!;

		if (allow.Body is ConstantExpression { Value: false })
		{
			return "策略结构性恒不放行（Allow 恒假），通常意味着 Any 之下全是拒绝条件。请确认策略构成。";
		}

		return null;
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
}
