using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
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
						   options.EnablePipelineBehaviors = true;
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
		services.AddEuoniaBus(config =>
		{
			config.RegisterHandlers(Assembly.GetExecutingAssembly());
			config.SetConventions(builder =>
				  {
					  builder.Add<DefaultMessageConvention>();
					  builder.Add<AttributeMessageConvention>();
					  builder.EvaluateUnicast(t => t.Name.EndsWith("Command"));
					  builder.EvaluateMulticast(t => t.Name.EndsWith("Event"));
					  builder.EvaluateRequest(t => t.Name.EndsWith("Request"));
				  })
				  .SetStrategy("InMemory", builder =>
				  {
					  builder.Add(new AttributeTransportStrategy(["InMemory"]));
					  builder.EvaluateIncoming(type => type.Name.EndsWith("Command"));
					  builder.EvaluateOutgoing(type => type.Name.EndsWith("Command"));
				  });
			// config.UseInMemory(options =>
			// {
			// 	options.IsDefaultTransport = true;
			// 	options.MultipleSubscriberInstance = false;
			// });
		});
	}

	//public void Configure(IServiceProvider applicationServices, IIdGenerator idGenerator)
	//{
	//  InitData();
	//}

	public void Configure(IServiceProvider applicationServices)
	{
		//var config = applicationServices.GetService<IConfiguration>();
	}
}