using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 收集服务接口实现类/方法上的认证特性，供生成的 HTTP 端点复用。
/// </summary>
internal sealed class HttpMetadataRegistry
{
	private readonly object _gate = new();
	private readonly Dictionary<Type, List<Type>> _implementations = new();
	private readonly Dictionary<string, IReadOnlyList<object>> _cache = new(StringComparer.Ordinal);
	private IReadOnlyList<Assembly> _assemblies = Array.Empty<Assembly>();

	/// <summary>
	/// 根据扫描范围重建实现类型索引。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集。</param>
	public void Build(IEnumerable<Assembly> assemblies)
	{
		var selected = assemblies.Distinct().ToArray();
		lock (_gate)
		{
			if (_assemblies.SequenceEqual(selected))
			{
				return;
			}

			_assemblies = selected;
			_implementations.Clear();
			_cache.Clear();

			foreach (var type in LoadTypes(selected))
			{
				if (!type.IsClass || type.IsAbstract)
				{
					continue;
				}

				foreach (var serviceType in type.GetInterfaces())
				{
					if (!_implementations.TryGetValue(serviceType, out var list))
					{
						list = new List<Type>();
						_implementations[serviceType] = list;
					}

					if (!list.Contains(type))
					{
						list.Add(type);
					}
				}
			}
		}
	}

	/// <summary>
	/// 获取接口方法及其实现类/方法上的认证元数据。
	/// </summary>
	/// <param name="serviceType">服务接口类型。</param>
	/// <param name="interfaceMethod">接口方法。</param>
	/// <returns>去重后的认证元数据。</returns>
	public IReadOnlyList<object> GetMetadata(Type serviceType, MethodInfo interfaceMethod)
	{
		var key = BuildKey(serviceType, interfaceMethod);
		lock (_gate)
		{
			if (_cache.TryGetValue(key, out var cached))
			{
				return cached;
			}

			var metadata = new List<object>();

			// 多实现时无法确定 DI 实际解析哪个实现；合并全部实现的授权特性会产生
			// “任一实现 AllowAnonymous 即完全放开”或“多角色 AND”的风险，因此只取接口元数据。
			if (_implementations.TryGetValue(serviceType, out var implementations) && implementations.Count == 1)
			{
				var implementation = implementations[0];
				AddMetadata(metadata, implementation, null);
				var targetMethod = FindImplementationMethod(implementation, serviceType, interfaceMethod);
				if (targetMethod != null)
				{
					AddMetadata(metadata, null, targetMethod);
				}
			}

			var result = Deduplicate(metadata);
			_cache[key] = result;
			return result;
		}
	}

	private static void AddMetadata(List<object> metadata, Type? type, MethodInfo? method)
	{
		if (type != null)
		{
			metadata.AddRange(type.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
			metadata.AddRange(type.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
		}

		if (method != null)
		{
			metadata.AddRange(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
			metadata.AddRange(method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
		}
	}

	private static IReadOnlyList<object> Deduplicate(List<object> metadata)
	{
		var result = new List<object>();
		var keys = new HashSet<string>(StringComparer.Ordinal);
		foreach (var item in metadata)
		{
			var key = item switch
			{
				AuthorizeAttribute authorize => $"authorize|{authorize.Policy}|{authorize.Roles}|{authorize.AuthenticationSchemes}",
				AllowAnonymousAttribute => "allow-anonymous",
				_ => item.GetType().FullName ?? item.GetType().Name
			};

			if (keys.Add(key))
			{
				result.Add(item);
			}
		}

		return result;
	}

	private static MethodInfo? FindImplementationMethod(Type implementation, Type serviceType, MethodInfo interfaceMethod)
	{
		try
		{
			var map = implementation.GetInterfaceMap(serviceType);
			var index = Array.IndexOf(map.InterfaceMethods, interfaceMethod);
			if (index >= 0)
			{
				return map.TargetMethods[index];
			}
		}
		catch (ArgumentException)
		{
			// Ignore and fall back to a signature match.
		}

		return implementation.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
							 .FirstOrDefault(method => method.Name == interfaceMethod.Name
							                        && method.GetParameters().Select(parameter => parameter.ParameterType)
							                                 .SequenceEqual(interfaceMethod.GetParameters().Select(parameter => parameter.ParameterType)));
	}

	private static IEnumerable<Type> LoadTypes(IEnumerable<Assembly> assemblies)
	{
		foreach (var assembly in assemblies)
		{
			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException exception)
			{
				types = exception.Types.OfType<Type>().ToArray();
			}

			foreach (var type in types)
			{
				yield return type;
			}
		}
	}

	private static string BuildKey(Type serviceType, MethodInfo method)
	{
		var parameters = string.Join(",", method.GetParameters().Select(parameter => parameter.ParameterType.FullName));
		return $"{serviceType.FullName}|{method.Name}({parameters})";
	}
}
