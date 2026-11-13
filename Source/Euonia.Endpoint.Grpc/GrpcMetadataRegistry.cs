using System.Reflection;
using Microsoft.AspNetCore.Authorization;

namespace Nerosoft.Euonia.Endpoint.Grpc;

/// <summary>
/// 收集 gRPC 服务接口实现类上的认证特性，供生成的端点复用。
/// </summary>
public sealed class GrpcMetadataRegistry
{
	private readonly object _gate = new();
	private readonly Dictionary<Type, List<Type>> _implementations = new();
	private readonly Dictionary<Type, IReadOnlyList<object>> _cache = new();
	private readonly Dictionary<string, IReadOnlyList<object>> _methodCache = new(StringComparer.Ordinal);
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
			_methodCache.Clear();

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
	/// 获取服务接口及其实现类上的认证元数据。
	/// </summary>
	/// <param name="serviceType">服务接口类型。</param>
	/// <returns>去重后的认证元数据。</returns>
	public IReadOnlyList<object> GetServiceMetadata(Type serviceType)
	{
		lock (_gate)
		{
			if (_cache.TryGetValue(serviceType, out var cached))
			{
				return cached;
			}

			var metadata = new List<object>();
			AddMetadata(metadata, serviceType);
			// 多实现时无法确定实际注册的实现，只使用接口元数据，避免 AllowAnonymous 误放开。
			if (_implementations.TryGetValue(serviceType, out var implementations) && implementations.Count == 1)
			{
				AddMetadata(metadata, implementations[0]);
			}

			var result = Deduplicate(metadata);
			_cache[serviceType] = result;
			return result;
		}
	}

	/// <summary>
	/// 获取接口/实现方法上的认证元数据。
	/// </summary>
	/// <param name="serviceType">服务接口类型。</param>
	/// <param name="methodName">方法名称。</param>
	/// <returns>去重后的认证元数据。</returns>
	public IReadOnlyList<object> GetMethodMetadata(Type serviceType, string methodName)
	{
		var key = $"{serviceType.FullName}|{methodName}";
		lock (_gate)
		{
			if (_methodCache.TryGetValue(key, out var cached))
			{
				return cached;
			}

			var metadata = new List<object>();
			AddMetadata(metadata, serviceType);
			var interfaceMethod = serviceType.GetMethods().FirstOrDefault(method => method.Name == methodName);
			if (interfaceMethod != null)
			{
				AddMethodMetadata(metadata, interfaceMethod);
			}

			if (_implementations.TryGetValue(serviceType, out var implementations) && implementations.Count == 1)
			{
				AddMetadata(metadata, implementations[0]);
				if (interfaceMethod != null)
				{
					var implementationMethod = FindImplementationMethod(implementations[0], serviceType, interfaceMethod);
					if (implementationMethod != null)
					{
						AddMethodMetadata(metadata, implementationMethod);
					}
				}
			}

			var result = Deduplicate(metadata);
			_methodCache[key] = result;
			return result;
		}
	}

	private static void AddMetadata(List<object> metadata, Type type)
	{
		metadata.AddRange(type.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
		metadata.AddRange(type.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
	}

	private static void AddMethodMetadata(List<object> metadata, System.Reflection.MethodInfo method)
	{
		metadata.AddRange(method.GetCustomAttributes<AuthorizeAttribute>(inherit: true));
		metadata.AddRange(method.GetCustomAttributes<AllowAnonymousAttribute>(inherit: true));
	}

	private static System.Reflection.MethodInfo? FindImplementationMethod(Type implementation, Type serviceType, System.Reflection.MethodInfo interfaceMethod)
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
			// Fall back to a name/signature match.
		}

		return implementation.GetMethods()
							 .FirstOrDefault(method => method.Name == interfaceMethod.Name
													   && method.GetParameters().Select(parameter => parameter.ParameterType)
																.SequenceEqual(interfaceMethod.GetParameters().Select(parameter => parameter.ParameterType)));
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
}
