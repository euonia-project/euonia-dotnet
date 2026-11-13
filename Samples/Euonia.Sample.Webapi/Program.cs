using Nerosoft.Euonia.Hosting;
using Serilog;
using Serilog.Events;

namespace Nerosoft.Euonia.Sample;

/// <summary>
/// 示例应用的入口类型，负责配置日志与宿主构建选项后启动服务。
/// </summary>
public class Program
{
	/// <summary>
	/// 配置 Serilog 日志输出与宿主选项，并启动示例应用。
	/// </summary>
	/// <param name="args">命令行参数。</param>
	public static void Main(string[] args)
	{
		static void HostBuilderOptionsAction(HostBuilderOptions options)
		{
			options.EnableHttp2 = true;
			//options.ConfigureWebHostBuilder = WebHostBuilderAction;
		options.ConfigureHostBuilder = builder =>
		{
			builder.UseSerilog((context, configuration) =>
			{
				configuration.ReadFrom.Configuration(context.Configuration)
					             .Enrich.FromLogContext()
					             .Enrich.WithProperty("ApplicationName", context.HostingEnvironment.ApplicationName)
					             .Enrich.WithProperty("Environment", context.HostingEnvironment);

					configuration.WriteTo.Logger(config => config.Filter.ByIncludingOnly(@event => @event.Level == LogEventLevel.Debug)
					                                             .WriteTo.File("Logs/Debug/logs.log", rollingInterval: RollingInterval.Day))
					             .WriteTo.Logger(config => config.Filter.ByIncludingOnly(@event => @event.Level == LogEventLevel.Warning)
					                                             .WriteTo.File("Logs/Warning/logs.log", rollingInterval: RollingInterval.Day))
					             .WriteTo.Logger(config => config.Filter.ByIncludingOnly(@event => @event.Level == LogEventLevel.Error)
					                                             .WriteTo.File("Logs/Error/logs.log", rollingInterval: RollingInterval.Day))
					             .WriteTo.Logger(config => config.Filter.ByIncludingOnly(@event => @event.Level == LogEventLevel.Information)
					                                             .WriteTo.File("Logs/Info/logs.log", rollingInterval: RollingInterval.Day))
					             .WriteTo.Logger(config => config.Filter.ByIncludingOnly(@event => @event.Level == LogEventLevel.Fatal)
					                                             .WriteTo.File("Logs/Fatal/logs.log", rollingInterval: RollingInterval.Day));
				});
			};
		}

		// static void WebHostBuilderAction(IWebHostBuilder builder)
		// {
		// 	builder.ConfigureKestrel(kes =>
		// 	{
		// 	    kes.ListenLocalhost(4567, cfg => cfg.Protocols = HttpProtocols.Http2);
		// 	    kes.ListenLocalhost(5678, cfg => cfg.Protocols = HttpProtocols.Http1);
		// 	});
		// }

		HostUtility.Run<Startup>(args, HostBuilderOptionsAction);
	}

	//public static void Main(string[] args)
	//{
	//    var builder = WebApplication.CreateBuilder(args);

	//    // Add services to the container.

	//    builder.Services.AddControllers();
	//    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
	//    builder.Services.AddEndpointsApiExplorer();
	//    builder.Services.AddSwaggerGen();

	//    var app = builder.Build();

	//    // Configure the HTTP request pipeline.
	//    if (app.Environment.IsDevelopment())
	//    {
	//        app.UseSwagger();
	//        app.UseSwaggerUI();
	//    }

	//    app.UseHttpsRedirection();

	//    app.UseAuthorization();


	//    app.MapControllers();

	//    app.Run();
	//}
}