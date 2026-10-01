namespace Nerosoft.Euonia.Sample;

using Nerosoft.Euonia.Sample.Filters;

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
        // 统一异常映射必须位于最外层：其余中间件（包括 Swagger 的文档页）都在其内侧，
        // 不启用开发者异常页（否则它会先兜住异常并输出 HTML，吞掉 { code, error, details } 的 JSON）。
        // ApiExceptionMiddleware 在 Development 下自带堆栈与详情。
        app.InitializeApplication();
        app.UseMiddleware<ApiExceptionMiddleware>();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Euonia.Sample v1"));
        }
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
