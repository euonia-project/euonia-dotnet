using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Bus.Tests.Commands;
using Nerosoft.Euonia.Bus.Tests.Handlers;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public class Startup
{
	public void ConfigureHost(IHostBuilder hostBuilder)
	{
		hostBuilder.ConfigureAppConfiguration(builder =>
		           {
			           builder.AddJsonFile("appsettings.json");
		           })
		           .ConfigureServices((context, services) =>
		           {
			           services.Configure<MessageBusOptions>(options =>
			           {
				           options.DefaultTransporter = "InMemory";
			           });
			           services.TryAddScoped<DefaultRequestContextAccessor>();
			           services.TryAddScoped<DelegateRequestContextAccessor>(_ =>
			           {
				           return () => new RequestContext();
			           });
			           services.AddModularityApplication<HostModule>(context.Configuration);
			           // Register service here.
		           });
	}

	// ConfigureServices(IServiceCollection services)
	// ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
	// ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
	public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
	{
		services.AddMessageHandler(ServiceLifetime.Transient, Assembly.GetExecutingAssembly());
		//services.AddEuoniaBus();
		// services.AddEuoniaBus(config =>
		// {
		// 	// config.UseInMemory(options =>
		// 	// {
		// 	// 	options.IsDefaultTransport = true;
		// 	// 	options.MultipleSubscriberInstance = false;
		// 	// });
		// });
		services.AddConfiguratorBuilder(config =>
		{
			config.RegisterChannel(Assembly.GetExecutingAssembly());
			config.SetConvention(builder =>
			      {
				      builder.Add<DefaultMessageConvention>();
				      builder.Add<AnnotationMessageConvention>();
				      builder.EvaluateUnicast((c, t) => t.Name.EndsWith("Command"));
				      builder.EvaluateMulticast((c, t) => t.Name.EndsWith("Event"));
				      builder.EvaluateRequest((c, t) => t.Name.EndsWith("Request"));
			      })
			      .SetStrategy("InMemory", builder =>
			      {
				      builder.Add(new AnnotationTransportStrategy(["InMemory"]));
				      builder.EvaluateIncoming((c, t) => true);
				      builder.EvaluateOutgoing((c, t) => true);
			      });
		});
	}

	//public void Configure(IServiceProvider applicationServices, IIdGenerator idGenerator)
	//{
	//  InitData();
	//}

	public void Configure(IServiceProvider applicationServices)
	{
		// var configurator = applicationServices.GetService<IConfigurator>();
		// applicationServices.GetService<ConfiguratorBuilder>()?.Invoke(configurator);
	}
}