using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Endpoint;
using Nerosoft.Euonia.Endpoint.Grpc;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 为 <see cref="IEndpointRouteBuilder"/> 提供 gRPC 端点映射扩展。
/// </summary>
public static class EndpointRouteBuilderExtensions
{
	/// <summary>
	/// 映射生成器注册的 gRPC 服务端点。
	/// </summary>
	/// <param name="endpoints">端点构建器。</param>
	/// <returns>原始端点构建器，以便链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="endpoints"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当端点构建器没有服务提供程序时抛出。</exception>
	public static IEndpointRouteBuilder MapDynamicGrpcEndpoint(this IEndpointRouteBuilder endpoints)
	{
		ArgumentNullException.ThrowIfNull(endpoints);
		var provider = endpoints.ServiceProvider ?? throw new InvalidOperationException("Missing service provider.");
		var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("Nerosoft.Euonia.Endpoint.Grpc");
		var options = provider.GetService<IOptions<GrpcExposureOptions>>()?.Value ?? new GrpcExposureOptions();
		if (!options.EnableExposeApplicationServiceAsGrpc)
		{
			logger?.LogInformation("gRPC endpoint exposure is disabled.");
			return endpoints;
		}

		var registry = provider.GetService<GrpcAssemblyRegistry>();
		var assemblies = ServiceCollectionExtensions.ResolveAssemblies(registry?.Assemblies.ToArray() ?? Array.Empty<Assembly>(), options, logger);
		provider.GetService<GrpcMetadataRegistry>()?.Build(assemblies);
		foreach (var assembly in assemblies.Distinct())
		{
			foreach (var attribute in assembly.GetCustomAttributes<GrpcGeneratedMappingAttribute>())
			{
				var method = attribute.MappingType.GetMethod("MapGenerated", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (method == null)
				{
					logger?.LogWarning("Generated gRPC mapping type '{Type}' has no MapGenerated method.", attribute.MappingType.FullName);
					continue;
				}

				try
				{
					method.Invoke(null, [endpoints]);
				}
				catch (TargetInvocationException exception) when (exception.InnerException != null)
				{
					ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
				}
			}
		}

		foreach (var assembly in assemblies.Distinct())
		{
			if (HasGeneratedMapping(assembly) || !HasExposeGrpc(assembly))
			{
				continue;
			}

			logger?.LogWarning(
				"Assembly '{Assembly}' contains [ExposeGrpc] methods but no generated gRPC mapping. gRPC does not support runtime fallback; reference Euonia.Endpoint.SourceGenerator in that assembly.",
				assembly.GetName().Name);
		}

		if (options.EnableReflection) endpoints.MapGrpcReflectionService();
		return endpoints;
	}

	private static bool HasGeneratedMapping(Assembly assembly)
	{
		return assembly.GetCustomAttributes<GrpcGeneratedMappingAttribute>().Any();
	}

	private static bool HasExposeGrpc(Assembly assembly)
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

		return types.Where(type => type.IsInterface)
					.SelectMany(type => type.GetMethods())
					.Any(method => method.GetCustomAttributes<ExposeGrpcAttribute>(inherit: false).Any());
	}
}
