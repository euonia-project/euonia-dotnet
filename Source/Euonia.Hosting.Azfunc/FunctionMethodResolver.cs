using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;

namespace Nerosoft.Euonia.Hosting.Azfunc;

/// <summary>
/// 按 <see cref="FunctionDefinition.EntryPoint"/> 解析函数目标方法。
/// </summary>
internal static class FunctionMethodResolver
{
	private static readonly ConcurrentDictionary<string, MethodInfo> Cache = new(StringComparer.Ordinal);

	/// <summary>
	/// 解析当前函数的目标方法；无法解析时返回 <c>null</c>。
	/// </summary>
	/// <param name="context">函数执行上下文。</param>
	/// <returns>目标方法；入口点为空或无法解析时为 <c>null</c>。</returns>
	public static MethodInfo? Resolve(FunctionContext context)
	{
		var entryPoint = context.FunctionDefinition.EntryPoint;

		if (string.IsNullOrWhiteSpace(entryPoint))
		{
			return null;
		}

		var key = context.FunctionDefinition.PathToAssembly + "|" + entryPoint;

		if (Cache.TryGetValue(key, out var cached))
		{
			return cached;
		}

		var method = ResolveCore(context.FunctionDefinition.PathToAssembly, entryPoint);

		if (method is not null)
		{
			Cache.TryAdd(key, method);
		}

		return method;
	}

	internal static MethodInfo? Resolve(string? pathToAssembly, string entryPoint)
	{
		return ResolveCore(pathToAssembly, entryPoint);
	}

	private static MethodInfo? ResolveCore(string? pathToAssembly, string entryPoint)
	{
		var separator = entryPoint.LastIndexOf('.');

		if (separator <= 0 || separator == entryPoint.Length - 1)
		{
			return null;
		}

		var typeName = entryPoint[..separator];
		var methodName = entryPoint[(separator + 1)..];

		var type = ResolveType(pathToAssembly, typeName);

		return type?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
		            .FirstOrDefault(method => method.Name == methodName);
	}

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
