using System.Reflection;
using Nerosoft.Euonia.Endpoint;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 扫描程序集或类型中标记了 <see cref="ExposeWebApiAttribute"/> 的接口方法。
/// </summary>
internal sealed class WebApiMethodScanner
{
	/// <summary>
	/// 扫描指定程序集。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	/// <returns>扫描到的待暴露方法。</returns>
	public IReadOnlyList<ExposedWebApiMethod> Scan(IEnumerable<Assembly> assemblies)
	{
		var types = new List<Type>();
		foreach (var assembly in assemblies)
		{
			try
			{
				types.AddRange(assembly.GetTypes());
			}
			catch (ReflectionTypeLoadException exception)
			{
				types.AddRange(exception.Types.OfType<Type>());
			}
		}

		return Scan(types);
	}

	/// <summary>
	/// 扫描指定类型。
	/// </summary>
	/// <param name="types">要扫描的类型。</param>
	/// <returns>扫描到的待暴露方法。</returns>
	public IReadOnlyList<ExposedWebApiMethod> Scan(IEnumerable<Type> types)
	{
		var result = new List<ExposedWebApiMethod>();
		var seen = new HashSet<string>(StringComparer.Ordinal);

		foreach (var type in types)
		{
			if (!type.IsInterface || type.IsGenericTypeDefinition)
			{
				continue;
			}

			foreach (var method in GetInterfaceMethods(type))
			{
				var attributes = method.GetCustomAttributes<ExposeWebApiAttribute>(inherit: false).ToArray();
				if (attributes.Length == 0)
				{
					continue;
				}

				var key = $"{type.FullName}|{GetMethodKey(method)}";
				if (!seen.Add(key))
				{
					continue;
				}

				foreach (var attribute in attributes)
				{
					result.Add(new ExposedWebApiMethod(type, method, attribute));
				}
			}
		}

		return result;
	}

	private static IEnumerable<MethodInfo> GetInterfaceMethods(Type interfaceType)
	{
		var methods = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
		foreach (var type in new[] { interfaceType }.Concat(interfaceType.GetInterfaces()))
		{
			foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
			{
				if (method.IsSpecialName || method.IsStatic || method.IsGenericMethodDefinition)
				{
					continue;
				}

				methods.TryAdd(GetMethodKey(method), method);
			}
		}

		return methods.Values;
	}

	private static string GetMethodKey(MethodInfo method)
	{
		return $"{method.Name}({string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName))})";
	}
}
