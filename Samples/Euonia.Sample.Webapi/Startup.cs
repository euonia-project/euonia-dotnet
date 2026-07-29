namespace Nerosoft.Euonia.Sample;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    /// <summary>
    /// Configure application services.
    /// </summary>
    /// <param name="services"></param>
    /// <remarks>
    /// This method gets called by the runtime. Use this method to add services to the container.
    /// </remarks>
    public void ConfigureServices(IServiceCollection services)
    {
        /*
        services.AddEntityFrameworkRepository<DataContext>(options =>
        {
            //options.UseNpgsql("Host=localhost;Database=euonia_sample;Username=postgres;Password=nerosoft.8888");
            //options.UseNpgsql("postgres://postgres:nerosoft.8888@localhost:5432/euonia_sample");
            options.UseInMemoryDatabase("Euonia.Sample");
        }); //PageActionEndpointConventionBuilder{ })

        */

        services.AddModularityApplication<HostModuleContext>(Configuration);

    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Euonia.Sample v1"));
        }

        app.InitializeApplication();
		app.UseExceptionHandler(new ExceptionHandlerOptions
		{
			ExceptionHandler = async context =>
			{
				var exceptionHandlerPathFeature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
				var exception = exceptionHandlerPathFeature?.Error;
				context.Response.StatusCode = 500;
				context.Response.ContentType = "application/json";
				var response = new
				{
					Message = "An unexpected error occurred.",
					Details = exception?.Message
				};
				await context.Response.WriteAsJsonAsync(response);
			}
		});
        app.UseHttpsRedirection();

        app.UseAuthentication();

        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            //endpoints.MapGrpcServices();
            endpoints.MapHealthChecks("health");
            if (env.IsDevelopment())
            {
                //endpoints.MapGrpcReflectionService();
            }
        });
    }
}
