using System.Linq.Expressions;
using System.Reflection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// <see cref="ScopeModelRegistry"/> 的构造器：从程序集扫描权限模型并完成注册期校验。
/// </summary>
/// <remarks>
/// 使用方经由 <c>ScopeModelRegistry.Create</c> 触达本类型，不直接使用。
/// </remarks>
internal sealed class ScopeModelRegistryBuilder
{
	private readonly List<(string Name, Func<IScopeModel> Factory)> _entries = [];

	/// <summary>注册一个模型实例。</summary>
	/// <param name="model">权限模型。</param>
	/// <returns>当前构造器，便于链式声明。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="model"/> 为 <see langword="null"/> 时抛出。</exception>
	public ScopeModelRegistryBuilder Add(IScopeModel model)
	{
		ArgumentNullException.ThrowIfNull(model);

		var captured = model;
		_entries.Add((model.GetType().Name, () => captured));

		return this;
	}

	/// <summary>注册一个模型类型，要求公共无参构造。</summary>
	/// <typeparam name="TModel">权限模型类型。</typeparam>
	/// <returns>当前构造器，便于链式声明。</returns>
	public ScopeModelRegistryBuilder Add<TModel>()
		where TModel : IScopeModel, new()
	{
		_entries.Add((typeof(TModel).Name, static () => new TModel()));

		return this;
	}

	/// <summary>扫描给定程序集并注册其中发现的全部权限模型。</summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>当前构造器，便于链式声明。</returns>
	public ScopeModelRegistryBuilder AddFrom(params Assembly[] assemblies)
	{
		foreach (var modelType in GetModelTypes(assemblies))
		{
			_entries.Add((modelType.Name, () => (IScopeModel)Activator.CreateInstance(modelType)!));
		}

		return this;
	}

	/// <summary>完成注册期校验并构建注册表。</summary>
	/// <returns>构建好的注册表；没有任何模型时返回 <see cref="ScopeModelRegistry.Empty"/>。</returns>
	/// <exception cref="ScopeModelValidationException">存在配置问题时抛出，携带全部诊断。</exception>
	public ScopeModelRegistry Build()
	{
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
				problems.Add(new ScopeModelDiagnostic(name, string.Format(Resources.IDS_MODEL_INSTANTIATE_FAILED, exception.Message)));
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
					string.Format(Resources.IDS_SCOPE_MULTIPLE_MODELS_FOR_TYPE, descriptor.ResourceType.FullName, existing.Model.GetType().Name)));
				continue;
			}

			var registration = new ScopeModelRegistration(descriptor, model);

			List<ScopeModelDiagnostic> modelProblems;

			try
			{
				modelProblems = ValidateModel(name, registration);
			}
			catch (Exception exception)
			{
				// 模型在 Declare 里自报冲突（标识或授予键被两条声明抢占）时是「抛出」而不是「返回诊断」的，
				// 而 Declare 正是首次读取模型声明时被调用的。捕获后并入诊断：
				// 否则整个 Build 会在第一个冲突处中断，用户得改一处跑一次。
				problems.Add(new ScopeModelDiagnostic(name, exception.Message));
				continue;
			}

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
			: ScopeModelRegistry.Create(registrations);
	}

	private static List<ScopeModelDiagnostic> ValidateModel(string name, ScopeModelRegistration registration)
	{
		var problems = new List<ScopeModelDiagnostic>();

		var policy = registration.Model.PolicyObject;

		if (policy == null)
		{
			problems.Add(new ScopeModelDiagnostic(name, Resources.IDS_SCOPE_POLICY_NOT_PROVIDED));
		}
		else
		{
			Collect(problems, name, TryValidatePolicy(name, registration.Descriptor, policy, ScopeKeys.Default));
		}

		// 每条声明各自编译校验：引用了未映射的维度、或恒不允许配置，
		// 都在注册期报出来，而不是等到某次请求踩上去。
		// 键在这里直接取声明值（而不是经 TryResolve 反查）：遍历的本来就是标识，不存在寻址问题；
		// 「标识与键互相抢占」已在 Declare 处拒绝，到不了这里。
		foreach (var (identifier, scopedPolicy) in registration.Model.DeclaredPolicies)
		{
			Collect(problems, name, TryValidatePolicy(name, registration.Descriptor, scopedPolicy, registration.Model.DeclaredKeys[identifier]));
		}

		return problems;

		static void Collect(List<ScopeModelDiagnostic> target, string name, string problem)
		{
			if (problem != null)
			{
				target.Add(new ScopeModelDiagnostic(name, problem));
			}
		}
	}

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

		var allow = (LambdaExpression)compiled.GetType().GetProperty(nameof(CompiledScopePolicy<object>.Allow))!.GetValue(compiled)!;

		if (allow.Body is ConstantExpression { Value: false })
		{
			return Resources.IDS_SCOPE_POLICY_NEVER_ALLOW;
		}

		return null;
	}

	private static IEnumerable<Type> GetModelTypes(IEnumerable<Assembly> assemblies)
	{
		return AssemblyHelper.LoadTypes(assemblies)
						   .Where(type => type.IsClass && !type.IsAbstract && type.IsAssignableTo(typeof(IScopeModel)));
	}
}
