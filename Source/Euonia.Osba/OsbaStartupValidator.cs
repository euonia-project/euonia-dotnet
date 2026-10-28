using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 启动期配置诊断。
/// </summary>
/// <param name="Severity">严重级别。</param>
/// <param name="Code">稳定诊断码，便于宿主忽略/过滤。</param>
/// <param name="Message">可读消息。</param>
/// <param name="Type">相关类型。</param>
public sealed record OsbaDiagnostic(OsbaDiagnosticSeverity Severity, string Code, string Message, Type Type = null);

/// <summary>
/// 诊断严重级别。
/// </summary>
public enum OsbaDiagnosticSeverity
{
	/// <summary>信息。</summary>
	Information,

	/// <summary>警告；不阻止启动。</summary>
	Warning,

	/// <summary>错误；启动期校验开启时阻止启动。</summary>
	Error
}

/// <summary>
/// Osba 启动期校验失败。
/// </summary>
public sealed class OsbaConfigurationException : Exception
{
	/// <summary>
	/// 初始化异常。
	/// </summary>
	/// <param name="diagnostics">错误诊断。</param>
	public OsbaConfigurationException(IReadOnlyList<OsbaDiagnostic> diagnostics)
		: base(BuildMessage(diagnostics))
	{
		Diagnostics = diagnostics;
	}

	/// <summary>
	/// 获取错误诊断集合。
	/// </summary>
	public IReadOnlyList<OsbaDiagnostic> Diagnostics { get; }

	private static string BuildMessage(IReadOnlyList<OsbaDiagnostic> diagnostics)
	{
		var builder = new StringBuilder("Euonia.Osba startup validation failed:");
		foreach (var diagnostic in diagnostics)
		{
			builder.AppendLine();
			builder.Append("  [").Append(diagnostic.Severity).Append("] ")
			       .Append(diagnostic.Code).Append(": ").Append(diagnostic.Message);
			if (diagnostic.Type != null)
			{
				builder.Append(" (").Append(diagnostic.Type.FullName).Append(')');
			}
		}

		return builder.ToString();
	}
}

/// <summary>
/// 启动期校验器：只做反射检查，不构建服务提供程序。
/// </summary>
/// <remarks>
/// 校验是<b>显式选项</b>（<see cref="OsbaOptions.ValidateOnStartup"/>）：默认关闭，
/// 因为测试/设计期程序集里常有故意不完整的业务对象。生产宿主开启后，结构性问题
/// （不支持的方法签名、无法解析的 <c>[Inject]</c>、属性注册失败）会在启动时暴露，
/// 而不是等到第一次请求。
/// </remarks>
public static class OsbaStartupValidator
{
	/// <summary>
	/// 校验指定程序集中的业务对象。
	/// </summary>
	/// <param name="assemblies">程序集。</param>
	/// <returns>诊断集合；无问题时为空。</returns>
	public static IReadOnlyList<OsbaDiagnostic> Validate(IEnumerable<Assembly> assemblies)
	{
		var results = new List<OsbaDiagnostic>();
		var assemblyArray = assemblies?.Where(assembly => assembly != null).ToArray() ?? [];

		foreach (var type in BusinessObjectTypeScanner.GetBusinessObjectTypes(assemblyArray))
		{
			ValidateFactoryMethods(type, results);
			ValidateInjectProperties(type, results);
			ValidatePropertyMetadata(type, results);
			ValidatePersistenceMethods(type, results);
		}

		return results;
	}

	/// <summary>
	/// 存在 Error 级诊断时抛出 <see cref="OsbaConfigurationException"/>。
	/// </summary>
	/// <param name="diagnostics">诊断集合。</param>
	/// <exception cref="OsbaConfigurationException">存在 Error 级诊断时抛出。</exception>
	public static void ThrowIfErrors(IReadOnlyList<OsbaDiagnostic> diagnostics)
	{
		var errors = diagnostics?.Where(diagnostic => diagnostic.Severity == OsbaDiagnosticSeverity.Error).ToArray() ?? [];
		if (errors.Length > 0)
		{
			throw new OsbaConfigurationException(errors);
		}
	}

	/// <summary>
	/// 校验工厂方法签名。
	/// </summary>
	private static void ValidateFactoryMethods(Type type, List<OsbaDiagnostic> diagnostics)
	{
		var methods = type.GetRuntimeMethods()
		                  .Where(method => method.GetCustomAttributes<FactoryMethodAttribute>(true).Any())
		                  .ToArray();

		foreach (var method in methods)
		{
			if (method.IsGenericMethodDefinition)
			{
				diagnostics.Add(Error("OSBA-F001", $"Factory method '{method.Name}' must not be generic.", type));
			}

			if (method.ReturnType.IsGenericType &&
			    method.ReturnType.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>))
			{
				diagnostics.Add(Error("OSBA-F002", $"Factory method '{method.Name}' returns IAsyncEnumerable, which is not supported.", type));
			}

			foreach (var parameter in method.GetParameters())
			{
				if (parameter.ParameterType.IsByRef)
				{
					diagnostics.Add(Error("OSBA-F003", $"Factory method '{method.Name}' has a by-ref parameter '{parameter.Name}'.", type));
				}
			}

			var attributes = method.GetCustomAttributes<FactoryMethodAttribute>(true).ToArray();
			if (attributes.Length > 1)
			{
				diagnostics.Add(Warning("OSBA-F004", $"Factory method '{method.Name}' declares multiple factory attributes; resolution is ambiguous.", type));
			}

			if (method.IsAbstract)
			{
				diagnostics.Add(Warning("OSBA-F005", $"Factory method '{method.Name}' is abstract and cannot be invoked.", type));
			}
		}
	}

	/// <summary>
	/// 校验 <c>[Inject]</c> 属性可解析。
	/// </summary>
	private static void ValidateInjectProperties(Type type, List<OsbaDiagnostic> diagnostics)
	{
		try
		{
			_ = ObjectReflector.GetAutoInjectProperties(type);
		}
		catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException or ArgumentException)
		{
			diagnostics.Add(Error("OSBA-I001", $"[Inject] property is not resolvable: {exception.Message}", type));
		}
	}

	/// <summary>
	/// 校验属性元数据可构建（静态注册完整）。
	/// </summary>
	private static void ValidatePropertyMetadata(Type type, List<OsbaDiagnostic> diagnostics)
	{
		try
		{
			_ = new FieldDataManager(type);
		}
		catch (Exception exception)
		{
			diagnostics.Add(Error("OSBA-P001", $"Property metadata cannot be built: {exception.Message}", type));
		}
	}

	/// <summary>
	/// 对可编辑对象提示缺失的持久化方法（不阻止启动）。
	/// </summary>
	private static void ValidatePersistenceMethods(Type type, List<OsbaDiagnostic> diagnostics)
	{
		if (!typeof(IEditableObject).IsAssignableFrom(type) || type.IsAbstract)
		{
			return;
		}

		var hasAny = new[]
		            {
			            typeof(FactoryInsertAttribute),
			            typeof(FactoryUpdateAttribute),
			            typeof(FactoryDeleteAttribute)
		            }
		            .Select(attribute => ObjectReflector.GetFactoryMethods(type, attribute)
		                                                 .Any(method => !IsBaseNoOp(method)))
		            .Any(found => found);

		if (!hasAny)
		{
			diagnostics.Add(Warning("OSBA-P002",
				"Editable object does not implement Insert/Update/Delete; saving it will fail with StrictFactoryMethods enabled.",
				type));
		}
	}

	/// <summary>
	/// 判断方法是否为 <see cref="EditableObject{T}"/> / <see cref="ServiceObject{T}"/> 的基类空实现。
	/// </summary>
	private static bool IsBaseNoOp(MethodInfo method)
	{
		var declaringType = method.DeclaringType;
		if (declaringType == null || !declaringType.IsGenericType)
		{
			return false;
		}

		var definition = declaringType.GetGenericTypeDefinition();
		return definition == typeof(EditableObject<>) || definition == typeof(ServiceObject<>);
	}

	private static OsbaDiagnostic Error(string code, string message, Type type)
	{
		return new OsbaDiagnostic(OsbaDiagnosticSeverity.Error, code, message, type);
	}

	private static OsbaDiagnostic Warning(string code, string message, Type type)
	{
		return new OsbaDiagnostic(OsbaDiagnosticSeverity.Warning, code, message, type);
	}
}
