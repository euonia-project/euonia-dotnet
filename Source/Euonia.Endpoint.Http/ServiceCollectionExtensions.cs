using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Endpoint.Http;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// HTTP 端点暴露注册扩展。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>从配置注册 HTTP 端点暴露。</summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configuration">应用配置。</param>
	/// <param name="assemblies">显式指定的扫描程序集；为空时使用配置中的扫描范围。</param>
	/// <returns>原始服务集合，以便链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="services"/> 或 <paramref name="configuration"/> 为 <c>null</c> 时抛出。</exception>
	public static IServiceCollection AddDynamicHttpEndpoint(this IServiceCollection services, IConfiguration configuration, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);

		services.AddOptions<HttpExposureOptions>().Bind(configuration.GetSection(HttpExposureOptions.SectionName));
		services.TryAddSingleton<HttpRegistry>();
		services.TryAddSingleton<HttpMetadataRegistry>();
		services.TryAddSingleton(new HttpAssemblyRegistry(assemblies));
		return services;
	}

	/// <summary>使用回调注册 HTTP 端点暴露。</summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configure">选项配置回调。</param>
	/// <param name="assemblies">显式指定的扫描程序集；为空时使用配置中的扫描范围。</param>
	/// <returns>原始服务集合，以便链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="services"/> 或 <paramref name="configure"/> 为 <c>null</c> 时抛出。</exception>
	public static IServiceCollection AddDynamicHttpEndpoint(this IServiceCollection services, Action<HttpExposureOptions> configure, params Assembly[] assemblies)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configure);

		services.AddOptions<HttpExposureOptions>().Configure(configure);
		services.TryAddSingleton<HttpRegistry>();
		services.TryAddSingleton<HttpMetadataRegistry>();
		services.TryAddSingleton(new HttpAssemblyRegistry(assemblies));
		return services;
	}
}
