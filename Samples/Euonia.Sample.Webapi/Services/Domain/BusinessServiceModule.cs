using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Registers business-layer services for the business module.
/// </summary>
/// <remarks>
/// This module registers business objects by scanning the assembly that contains
/// the `BusinessServiceModule` type and invoking the `AddBusinessObject` extension
/// on the service collection. It derives from <see cref="ModuleContextBase"/> and
/// participates in the application's modular startup.
/// </remarks>
/// <seealso cref="ModuleContextBase"/>
/// <seealso cref="ServiceConfigurationContext"/>
public class BusinessServiceModule : ModuleContextBase
{
	/// <summary>
	/// Configures services for the business module.
	/// </summary>
	/// <param name="context">
	/// The <see cref="ServiceConfigurationContext"/> provided by the modular framework.
	/// Contains the <see cref="IServiceCollection"/> to which business objects will be registered.
	/// </param>
	/// <remarks>
	/// This implementation registers all business objects located in the current assembly
	/// by calling <c>context.Services.AddBusinessObject(typeof(BusinessServiceModule).Assembly)</c>.
	/// </remarks>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddBusinessObject(typeof(BusinessServiceModule).Assembly);
	}
}
