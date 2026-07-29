using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.InMemory;
using Nerosoft.Euonia.Bus.RabbitMq;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Sample.Domain;
using Nerosoft.Euonia.Sample.Persist;
using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Sample.Facade;

/// <summary>
/// Facade service module for the Linkyou application.
/// </summary>
/// <remarks>
/// <para>
/// This module participates in the application's modular initialization and service
/// registration pipeline by inheriting from <see cref="ModuleContextBase"/>.
/// </para>
/// Responsibilities:
/// <para>- Act as a central registration point for application services.</para>
/// <para>- Provide a place to add initialization logic related to application functionality.</para>
/// Dependencies:
/// <para>- Depends on <see cref="ApplicationModule"/> to leverage core application services and infrastructure,
///   <see cref="PersistServiceModule"/>, <see cref="BusinessServiceModule"/>,
///   and <see cref="DomainServiceModule"/> to ensure required services are registered
///   before this module initializes.
/// </para>
/// Extension guidance:
/// <para>- Add additional service registrations here or override lifecycle methods from
///   <see cref="ModuleContextBase"/> (such as startup/shutdown hooks) when application functionality requires initialization or configuration.
/// </para>
/// </remarks>
/// <seealso cref="ModuleContextBase"/>
[DependsOn(typeof(ApplicationModule), typeof(UnitOfWorkModule))]
[DependsOn(typeof(PersistServiceModule), typeof(BusinessServiceModule), typeof(DomainServiceModule))]
[DependsOn(typeof(InMemoryBusModule), typeof(RabbitMqBusModule))]
public class FacadeServiceModule : ModuleContextBase
{
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddOptions<MessageBusOptions>()
		       .BindConfiguration("Euonia:Bus")
		       .Validate(_ => true);
	}

	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.Register<FacadeServiceContext>();

		context.Services.AddEuoniaBus(config =>
		{
			config.RegisterHandlers([typeof(FacadeServiceModule).Assembly])
			      .SetConventions(builder =>
			      {
				      builder.Add<DefaultMessageConvention>();
				      builder.Add<AttributeMessageConvention>();
				      builder.Add<DomainMessageConvention>();
			      })
			      .SetStrategy("InMemory", builder =>
			      {
				      builder.Add<LocalMessageTransportStrategy>();
				      builder.EvaluateIncoming(_ => true);
				      builder.EvaluateOutgoing(_ => true);
			      });
		});
	}
}