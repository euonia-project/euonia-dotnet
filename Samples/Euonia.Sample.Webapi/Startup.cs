namespace Nerosoft.Euonia.Sample;

using Nerosoft.Euonia.Sample.Filters;

/// <summary>
/// 示例应用的启动配置，负责注册服务并构建 HTTP 请求管道。
/// </summary>
public class Startup
{
    /// <summary>
    /// 使用应用配置初始化 <see cref="Startup"/>。
    /// </summary>
    /// <param name="configuration">应用配置。</param>
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// 获取应用配置。
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// 配置应用服务。
    /// </summary>
    /// <param name="services">用于注册应用程序服务的服务集合。</param>
    /// <remarks>
    /// 此方法由运行时调用。使用此方法向容器添加服务。
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

    // 此方法由运行时调用。使用此方法配置 HTTP 请求管道。
    /// <summary>
    /// 配置 HTTP 请求管道，依次启用模块初始化、统一异常处理、开发环境文档、认证授权与端点映射。
    /// </summary>
    /// <param name="app">用于构建请求管道的应用构建器。</param>
    /// <param name="env">提供当前宿主环境信息的运行环境。</param>
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
