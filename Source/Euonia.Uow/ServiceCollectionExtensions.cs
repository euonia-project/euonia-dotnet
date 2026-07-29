using Nerosoft.Euonia.Uow;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for adding repository services to the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Add unit of work.
	/// </summary>
	/// <param name="services"></param>
	/// <returns></returns>
	public static IServiceCollection AddUnitOfWork(this IServiceCollection services)
	{
		services.AddTransient<IUnitOfWork, UnitOfWork>();
		services.AddSingleton<IUnitOfWorkAccessor, UnitOfWorkAccessor>();
		services.AddSingleton<IUnitOfWorkManager, UnitOfWorkManager>();
		return services;
	}
}