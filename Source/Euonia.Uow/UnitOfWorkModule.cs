using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// Module responsible for registering unit-of-work related services into the DI container.
/// </summary>
/// <remarks>
/// This module registers the <see cref="UnitOfWorkInterceptor"/> as an <see cref="IInterceptor"/>
/// with a transient lifetime so it can be used by proxying mechanisms that require interception
/// of unit-of-work behavior.
/// </remarks>
public class UnitOfWorkModule : ModuleContextBase
{
	/// <summary>
	/// Called during service configuration to register required services for the module.
	/// </summary>
	/// <param name="context">The <see cref="ServiceConfigurationContext"/> that provides access to the service collection.</param>
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		// Register the UnitOfWorkInterceptor as a transient IInterceptor so each injection gets a new instance.
		context.Services.AddTransient<IInterceptor, UnitOfWorkInterceptor>();
		context.Services.Configure<UnitOfWorkOptions>(Configuration.GetSection("Euonia:Uow"));
	}

	/// <summary>
	/// Called during service configuration to register required services for the module.
	/// </summary>
	/// <param name="context">The <see cref="ServiceConfigurationContext"/> that provides access to the service collection.</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddUnitOfWork();
	}
}