using Nerosoft.Euonia.Mapping;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Sample.Domain.Mappers;
using Nerosoft.Euonia.Validation;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// Registers domain-level services for the bounded context.
/// </summary>
/// <remarks>
/// This module is responsible for composing and registering domain services, validators,
/// and mapping profiles required by the domains. It declares dependencies on
/// <see cref="AutomapperModule"/> and <see cref="ValidationModule"/>, ensuring that
/// mapping and validation infrastructure is available when this module is initialized.
/// </remarks>
/// <seealso cref="ModuleContextBase"/>
[DependsOn(typeof(AutomapperModule), typeof(ValidationModule))]
public class DomainServiceModule : ModuleContextBase
{
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddTransient(typeof(ShortUniqueIdResolver<,>));
		Configure<AutomapperOptions>(options =>
		{
			options.AddProfile<UserMapperProfile>();
		});
	}
}
