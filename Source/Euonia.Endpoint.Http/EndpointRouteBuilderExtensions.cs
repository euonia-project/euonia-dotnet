using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Endpoint.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// HTTP 端点映射扩展。
/// </summary>
public static class EndpointRouteBuilderExtensions
{
	/// <summary>
	/// 映射生成端点与运行时兜底端点。
	/// </summary>
	public static IEndpointRouteBuilder MapDynamicHttpEndpoint(this IEndpointRouteBuilder endpoints)
	{
		ArgumentNullException.ThrowIfNull(endpoints);

		var provider = endpoints.ServiceProvider ?? throw new InvalidOperationException("Missing service provider.");
		var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("Nerosoft.Euonia.Endpoint.Http");
		var options = provider.GetService<IOptions<HttpExposureOptions>>()?.Value ?? new HttpExposureOptions();
		if (!options.EnableExposeApplicationServiceAsWebApi)
		{
			logger?.LogInformation("HTTP endpoint exposure is disabled.");
			return endpoints;
		}

		var assemblies = ResolveAssemblies(provider, options, logger);
		var metadataRegistry = provider.GetService<HttpMetadataRegistry>();
		metadataRegistry?.Build(assemblies);
		RunGeneratedMappings(assemblies, endpoints, logger);
		MapRuntimeEndpoints(assemblies, endpoints, provider, options, metadataRegistry, logger);
		return endpoints;
	}

	private static void RunGeneratedMappings(IEnumerable<Assembly> assemblies, IEndpointRouteBuilder endpoints, ILogger? logger)
	{
		foreach (var assembly in assemblies.Distinct())
		{
			foreach (var attribute in assembly.GetCustomAttributes<HttpGeneratedMappingAttribute>())
			{
				var method = attribute.MappingType.GetMethod("MapGenerated", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				if (method == null)
				{
					logger?.LogWarning("Generated HTTP mapping type '{Type}' has no MapGenerated method.", attribute.MappingType.FullName);
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
	}

	private static void MapRuntimeEndpoints(IEnumerable<Assembly> assemblies, IEndpointRouteBuilder endpoints, IServiceProvider provider, HttpExposureOptions options, HttpMetadataRegistry? metadataRegistry, ILogger? logger)
	{
		var scanner = new WebApiMethodScanner();
		var types = FilterTypes(LoadTypes(assemblies), options).ToArray();
		var exposedMethods = scanner.Scan(types);
		if (exposedMethods.Count == 0)
		{
			logger?.LogInformation("No methods marked with ExposeWebApiAttribute were found in the configured scan scope.");
			return;
		}

		var descriptorBuilder = new WebApiActionDescriptorBuilder(options);
		var registry = provider.GetService<HttpRegistry>() ?? throw new InvalidOperationException("HttpRegistry is not registered. Call AddDynamicHttpEndpoint first.");
		var isService = provider.GetService<IServiceProviderIsService>();
		var counter = 0;

		foreach (var exposed in exposedMethods)
		{
			WebApiActionDescriptor descriptor;
			try
			{
				var extraMetadata = metadataRegistry?.GetMetadata(exposed.ServiceType, exposed.Method);
				descriptor = descriptorBuilder.Build(exposed, extraMetadata);
			}
			catch (Exception exception) when (exception is InvalidOperationException or NotSupportedException)
			{
				logger?.LogWarning(exception, "Skipped generated-incompatible HTTP endpoint {Service}.{Method}.", exposed.ServiceType.FullName, exposed.Method.Name);
				continue;
			}

			if (isService?.IsService(exposed.ServiceType) == false)
			{
				var message = $"Service '{exposed.ServiceType.FullName}' is marked with ExposeWebApiAttribute but is not registered.";
				if (options.ThrowOnUnregisteredService) throw new InvalidOperationException(message);
				logger?.LogWarning("{Message}", message);
				continue;
			}

			var reservation = registry.TryReserve(descriptor.RoutePattern, descriptor.HttpMethods, generated: false, out var availableMethods);
			if (reservation == HttpReservation.AlreadyGenerated)
			{
				logger?.LogDebug("HTTP endpoint '{Route}' was already mapped by generated code.", descriptor.RoutePattern);
				continue;
			}

			if (reservation == HttpReservation.Conflict)
			{
				var message = $"Duplicate HTTP endpoint '{descriptor.RoutePattern}' for '{exposed.ServiceType.FullName}.{exposed.Method.Name}'.";
				if (options.ThrowOnRouteConflict) throw new InvalidOperationException(message);
				logger?.LogWarning("{Message}", message);
				continue;
			}

			var handler = WebApiEndpointFactory.Create(descriptor, provider);
			var builder = endpoints.MapMethods(descriptor.RoutePattern, availableMethods, handler);
			builder.WithDisplayName($"{exposed.ServiceType.Name}.{exposed.Method.Name}");
			builder.WithName($"{exposed.ServiceType.FullName ?? exposed.ServiceType.Name}.{exposed.Method.Name}.{string.Join("_", availableMethods)}.runtime.{counter++}");
			builder.WithTags(exposed.ServiceType.Name);
			if (descriptor.Metadata.Count > 0)
			{
				builder.WithMetadata(descriptor.Metadata.ToArray());
			}

			if (options.RequireAuthorization)
			{
				builder.RequireAuthorization();
			}
		}
	}

	private static IReadOnlyList<Assembly> ResolveAssemblies(IServiceProvider provider, HttpExposureOptions options, ILogger? logger)
	{
		var registry = provider.GetService<HttpAssemblyRegistry>();
		if (registry is { Assemblies.Count: > 0 })
		{
			return registry.Assemblies;
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
			logger?.LogWarning(exception, "Could not load scan assembly '{Assembly}'.", name);
			return null;
		}
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

	private static IEnumerable<Type> FilterTypes(IEnumerable<Type> types, HttpExposureOptions options)
	{
		foreach (var type in types)
		{
			if (HttpExposureScope.IsIncluded(type, options))
			{
				yield return type;
			}
		}
	}
}
