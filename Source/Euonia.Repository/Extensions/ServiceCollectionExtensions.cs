using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Repository;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于向依赖注入容器注册仓储相关服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 注册默认的上下文提供程序及其默认上下文工厂。
	/// </summary>
	/// <param name="services">要注册服务的 <see cref="IServiceCollection"/>。</param>
	/// <returns>注册完成后的 <paramref name="services"/>，以便链式调用。</returns>
	/// <remarks>
	/// <see cref="IContextProvider"/> 以作用域（Scoped）生命周期注册，
	/// 而 <see cref="IContextFactory"/> 以瞬态（Transient）生命周期注册。
	/// </remarks>
	public static IServiceCollection AddContextProvider(this IServiceCollection services)
	{
		services.TryAddScoped<IContextProvider, DefaultContextProvider>();
		services.AddTransient<IContextFactory, DefaultContextFactory>();
		return services;
	}
}