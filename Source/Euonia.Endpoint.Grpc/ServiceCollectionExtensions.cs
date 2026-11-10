using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Endpoint.Grpc;
using ProtoBuf.Grpc.Server;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 为 <see cref="IServiceCollection"/> 提供 gRPC 端点暴露注册扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 从配置节 <see cref="GrpcExposureOptions.SectionName"/> 读取选项并注册 gRPC 端点暴露。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configuration">应用配置。</param>
	/// <param name="assemblies">显式指定的扫描程序集；为空时使用配置中的扫描范围。</param>
	/// <returns>原始服务集合，以便链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="services"/> 或 <paramref name="configuration"/> 为 <c>null</c> 时抛出。</exception>
	public static IServiceCollection AddDynamicGrpcEndpoint(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);
		EndpointProtoModel.Configure();
		var options = configuration.GetSection(GrpcExposureOptions.SectionName).Get<GrpcExposureOptions>() ?? new GrpcExposureOptions();
		services.AddOptions<GrpcExposureOptions>().Bind(configuration.GetSection(GrpcExposureOptions.SectionName));
		services.AddCodeFirstGrpc();
		services.TryAddSingleton<GrpcMetadataRegistry>();
		services.TryAddSingleton(new GrpcAssemblyRegistry(assemblies));
		if (options.EnableExposeApplicationServiceAsGrpc)
		{
			RunGeneratedRegistrations(services, ResolveAssemblies(assemblies, options));
		}
		return services;
	}
	/// <summary>
	/// 使用回调配置 gRPC 端点暴露选项并注册所需服务。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configure">选项配置回调。</param>
	/// <param name="assemblies">显式指定的扫描程序集；为空时使用配置中的扫描范围。</param>
	/// <returns>原始服务集合，以便链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="services"/> 或 <paramref name="configure"/> 为 <c>null</c> 时抛出。</exception>
	public static IServiceCollection AddDynamicGrpcEndpoint(this IServiceCollection services, Action<GrpcExposureOptions> configure, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configure);
		EndpointProtoModel.Configure();
		var options = new GrpcExposureOptions();
		configure(options);
		services.AddOptions<GrpcExposureOptions>().Configure(configure);
		services.AddCodeFirstGrpc();
		services.TryAddSingleton<GrpcMetadataRegistry>();
		services.TryAddSingleton(new GrpcAssemblyRegistry(assemblies));
		if (options.EnableExposeApplicationServiceAsGrpc)
		{
			RunGeneratedRegistrations(services, ResolveAssemblies(assemblies, options));
		}
		return services;
	}

	internal static IReadOnlyList<Assembly> ResolveAssemblies(Assembly[] explicitAssemblies, GrpcExposureOptions options, ILogger? logger = null)
	{
		if (explicitAssemblies.Length > 0)
		{
			return explicitAssemblies.Where(assembly => assembly != null).Distinct().ToArray();
		}

		var assemblies = new List<Assembly>();
		foreach (var name in options.ScanAssemblies ?? Array.Empty<string>())
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				continue;
			}

			var assembly = ResolveAssembly(name.Trim(), logger);
			if (assembly != null)
			{
				assemblies.Add(assembly);
			}
		}

		if (assemblies.Count == 0 && options.ScanEntryAssembly)
		{
			var entry = Assembly.GetEntryAssembly();
			if (entry != null)
			{
				assemblies.Add(entry);
			}
		}

		return assemblies.Distinct().ToArray();
	}

	private static Assembly? ResolveAssembly(string name, ILogger? logger)
	{
		var loaded = AppDomain.CurrentDomain.GetAssemblies()
							   .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase)
														  || string.Equals(assembly.FullName, name, StringComparison.OrdinalIgnoreCase));
		if (loaded != null)
		{
			return loaded;
		}

		try
		{
			return Assembly.Load(new AssemblyName(name));
		}
		catch (Exception exception)
		{
			logger?.LogWarning(exception, "Failed to resolve gRPC scan assembly '{Assembly}'.", name);
			return null;
		}
	}

	private static void RunGeneratedRegistrations(IServiceCollection services, IEnumerable<Assembly> assemblies)
	{
		foreach (var assembly in assemblies.Distinct())
		{
			foreach (var attribute in assembly.GetCustomAttributes<GrpcGeneratedMappingAttribute>())
			{
				var method = attribute.MappingType.GetMethod("AddGenerated", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (method == null)
				{
					continue;
				}

				try
				{
					method.Invoke(null, new object[] { services });
				}
				catch (TargetInvocationException exception) when (exception.InnerException != null)
				{
					ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
				}
			}
		}
	}
}
/// <summary>
/// 保存 gRPC 端点扫描显式指定的程序集。
/// </summary>
internal sealed class GrpcAssemblyRegistry
{
	/// <summary>
	/// 初始化 <see cref="GrpcAssemblyRegistry"/> 的新实例。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集；为 <c>null</c> 时使用空集合。</param>
	public GrpcAssemblyRegistry(IEnumerable<Assembly>? assemblies)
	{
		Assemblies = assemblies?.Where(assembly => assembly != null).Distinct().ToArray() ?? Array.Empty<Assembly>();
	}

	/// <summary>
	/// 获取显式指定的程序集。
	/// </summary>
	public IReadOnlyList<Assembly> Assemblies { get; }
}
