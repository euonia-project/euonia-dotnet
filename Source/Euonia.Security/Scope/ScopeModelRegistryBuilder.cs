using System.Linq.Expressions;
using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// <see cref="ScopeModelRegistry"/> 的构造器：程序化地声明要注册哪些权限模型。
/// </summary>
public sealed class ScopeModelRegistryBuilder
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
	/// <param name="codeSource">权限码来源，用于校验策略键解析。</param>
	/// <returns>构建好的注册表；没有任何模型时返回 <see cref="ScopeModelRegistry.Empty"/>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="codeSource"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ScopeModelValidationException">存在配置问题时抛出，携带全部诊断。</exception>
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

	private static string ValidateKeyResolution(string name, ScopeModelRegistration registration, IPermissionCodeSource codeSource)
	{
		var resolved = new HashSet<string>(StringComparer.Ordinal);

		foreach (var operation in codeSource.AllOperations)
		{
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
			if (ScopeKeys.IsReserved(code) || resolved.Contains(code))
			{
				continue;
			}

			return $"为权限码 '{code}' 声明了行级策略，但没有任何操作会解析到该码"
			       + $"（请核对方法上 [Permission] 的码与 Declare 里的码是否一致）。已解析到的码：{(resolved.Count == 0 ? "（无）" : string.Join(", ", resolved))}。";
		}

		return null;
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
