using Nerosoft.Euonia.Uow;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于向依赖注入容器注册工作单元相关服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 注册工作单元及其访问器与管理器。
	/// </summary>
	/// <param name="services">要注册服务的 <see cref="IServiceCollection"/>。</param>
	/// <returns>注册完成后的 <paramref name="services"/>，以便链式调用。</returns>
	/// <remarks>
	/// <see cref="IUnitOfWork"/> 以瞬态（Transient）生命周期注册；
	/// <see cref="IUnitOfWorkAccessor"/> 与 <see cref="IUnitOfWorkManager"/> 以单例（Singleton）生命周期注册。
	/// </remarks>
	public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
	{
		services.AddTransient<IUnitOfWork, UnitOfWork>();
		services.AddSingleton<IUnitOfWorkAccessor, UnitOfWorkAccessor>();
		services.AddSingleton<IUnitOfWorkManager, UnitOfWorkManager>();
		return services;
	}
}