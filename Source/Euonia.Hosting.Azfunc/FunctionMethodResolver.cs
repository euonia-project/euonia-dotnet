using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// 按 <see cref="FunctionDefinition.EntryPoint"/> 解析函数目标方法。
/// </summary>
internal static class FunctionMethodResolver
{
	private static readonly ConcurrentDictionary<string, ResolutionResult> Cache = new(StringComparer.Ordinal);

	/// <summary>
	/// 解析当前函数的目标方法；无法解析时返回 <c>null</c>。
	/// </summary>
	/// <param name="context">函数执行上下文。</param>
	/// <returns>目标方法；入口点为空或无法解析时为 <c>null</c>。</returns>
	/// <exception cref="InvalidOperationException">入口点匹配到多个同名方法，无法安全判定授权特性。</exception>
	public static MethodInfo? Resolve(FunctionContext context)
	{
		var entryPoint = context.FunctionDefinition.EntryPoint;

		if (string.IsNullOrWhiteSpace(entryPoint))
		{
			return null;
		}

		return ResolveCached(context.FunctionDefinition.PathToAssembly, entryPoint);
	}

	internal static MethodInfo? Resolve(string? pathToAssembly, string entryPoint)
	{
		return ResolveCached(pathToAssembly, entryPoint);
	}

	private static MethodInfo? ResolveCached(string? pathToAssembly, string entryPoint)
	{
		var key = (pathToAssembly ?? string.Empty) + "|" + entryPoint;

		// 缓存包含失败结果，避免无法解析的入口点每请求都扫描全部程序集。
		var result = Cache.GetOrAdd(key, _ => ResolveCore(pathToAssembly, entryPoint));

		if (result.ErrorMessage is not null)
		{
			throw new InvalidOperationException(result.ErrorMessage);
		}

		return result.Method;
	}

	private static ResolutionResult ResolveCore(string? pathToAssembly, string entryPoint)
	{
		var separator = entryPoint.LastIndexOf('.');

		if (separator <= 0 || separator == entryPoint.Length - 1)
		{
			return new ResolutionResult(null, null);
		}

		var typeName = entryPoint[..separator];
		var methodName = entryPoint[(separator + 1)..];

		var type = ResolveType(pathToAssembly, typeName);

		if (type is null)
		{
			return new ResolutionResult(null, null);
		}

		var matches = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
		                  .Where(method => method.Name == methodName)
		                  .ToArray();

		if (matches.Length == 0)
		{
			return new ResolutionResult(null, null);
		}

		if (matches.Length > 1)
		{
			// fail-closed：同名重载无法确定授权特性归属，宁可拒绝执行也不猜测。
			return new ResolutionResult(null, $"函数入口点 {entryPoint} 匹配到 {matches.Length} 个同名方法，无法确定授权特性，已拒绝执行。");
		}

		return new ResolutionResult(matches[0], null);
	}

	private sealed record ResolutionResult(MethodInfo? Method, string? ErrorMessage);

	private static Type? ResolveType(string? pathToAssembly, string typeName)
	{
		if (!string.IsNullOrWhiteSpace(pathToAssembly) && File.Exists(pathToAssembly))
		{
			try
			{
				var assembly = AppDomain.CurrentDomain.GetAssemblies()
				                         .FirstOrDefault(item => string.Equals(item.Location, pathToAssembly, StringComparison.OrdinalIgnoreCase))
				               ?? Assembly.LoadFrom(pathToAssembly);

				var type = assembly.GetType(typeName);
				if (type is not null)
				{
					return type;
				}
			}
			catch (Exception exception) when (exception is FileLoadException or BadImageFormatException or IOException)
			{
				// 回退到已加载程序集扫描。
			}
		}

		return AppDomain.CurrentDomain.GetAssemblies()
		                .Select(assembly => assembly.GetType(typeName))
		                .FirstOrDefault(type => type is not null);
	}
}
