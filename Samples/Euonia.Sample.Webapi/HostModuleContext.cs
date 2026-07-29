using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.OpenApi;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Hosting;
using Serilog;
using Nerosoft.Euonia.Sample.Facade;

namespace Nerosoft.Euonia.Sample;

/// <summary>
/// Module context to configure the current application.
/// </summary>
[DependsOn(typeof(FacadeServiceModule))]
[DependsOn(typeof(HostingModule))]
public class HostModuleContext : ModuleContextBase
{
	/// <inheritdoc/>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddControllers();
		context.Services.AddHealthChecks();
		context.Services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new OpenApiInfo { Title = "Euonia Sample", Version = "v1" });
		});
	}

	/// <inheritdoc/>
	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		var app = context.GetApplicationBuilder();
		app.ServerFeatures.Get<IServerAddressesFeature>();

		app.UseSerilogRequestLogging();
		app.UseForwardedHeaders();
		app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()); //.AllowCredentials());
	}
}
