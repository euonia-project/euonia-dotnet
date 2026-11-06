using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// RabbitMQ 总线测试宿主的启动配置，负责注册消息总线、RabbitMQ 传输与测试用处理器。
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "<Pending>")]
[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
public class Startup
{
	/// <summary>
	/// 配置测试宿主的应用配置与基础服务，并加载模块化应用。
	/// </summary>
	/// <param name="hostBuilder">由测试框架传入的宿主构建器。</param>
	public void ConfigureHost(IHostBuilder hostBuilder)
	{
		hostBuilder.ConfigureAppConfiguration(builder =>
		           {
			           builder.AddJsonFile("appsettings.json");
		           })
		           .ConfigureServices((context, services) =>
		           {
			           services.TryAddScoped<DefaultRequestContextAccessor>();
			           services.TryAddScoped<DelegateRequestContextAccessor>(_ =>
			           {
				           return () => new RequestContext();
			           });
			           services.AddModularityApplication<HostModule>(context.Configuration);
			           // 在此注册服务。
		           });
	}

	// ConfigureServices(IServiceCollection services)
	// ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
	// ConfigureServices(HostBuilderContext hostBuilderContext, IServiceCollection services)
	/// <summary>
	/// 注册测试程序集中的消息处理器、配置器与 RabbitMQ 传输策略；配置要求跳过测试时直接返回。
	/// </summary>
	/// <param name="services">宿主服务集合。</param>
	/// <param name="hostBuilderContext">宿主构建上下文，提供配置信息。</param>
	public void ConfigureServices(IServiceCollection services, HostBuilderContext hostBuilderContext)
	{
		var preventUnitTest = hostBuilderContext.Configuration.GetValue<bool>("PreventRunTests");
		if (preventUnitTest)
		{
			return;
		}

		// services.AddEuoniaBus(config =>
		// {
		// 	config.RegisterChannel(Assembly.GetExecutingAssembly());
		// 	config.SetConvention(builder =>
		// 	{
		// 		builder.Add<DefaultMessageConvention>();
		// 		builder.Add<AnnotationMessageConvention>();
		// 		builder.EvaluateUnicast((c, t) => c.EndsWith("Command"));
		// 		builder.EvaluateMulticast((c, t) => c.EndsWith("Event"));
		// 		builder.EvaluateRequest((c, t) => c.EndsWith("Request"));
		// 	});
		// 	config.SetStrategy("RabbitMq", builder =>
		// 	{
		// 		builder.EvaluateOutgoing(e => true);
		// 		builder.EvaluateIncoming(e => true);
		// 	});
		// 	// config.UseRabbitMq(options =>
		// 	// {
		// 	// 	options.Connection = "amqp://127.0.0.1";
		// 	// 	options.QueueName = "nerosoft.euonia.test.command";
		// 	// 	options.TopicName = "nerosoft.euonia.test.event";
		// 	// 	options.ExchangeName = $"nerosoft.euonia.test.exchange.{options.ExchangeType}";
		// 	// 	options.RoutingKey = "*";
		// 	// });
		// });
		services.AddMessageHandler(ServiceLifetime.Transient, Assembly.GetExecutingAssembly());
		services.AddEuoniaBus();

		services.AddSingleton<ConfiguratorBuilder>(_ =>
		{
			void Action(IConfigurator config)
			{
				config.RegisterChannel(Assembly.GetExecutingAssembly());
				config.SetConvention(builder =>
				      {
					      builder.Add<DefaultMessageConvention>();
					      builder.Add<AnnotationMessageConvention>();
					      builder.EvaluateUnicast((c, t) => c.EndsWith("Command"));
					      builder.EvaluateMulticast((c, t) => c.EndsWith("Event"));
					      builder.EvaluateRequest((c, t) => c.EndsWith("Request"));
				      })
				      .SetStrategy("RabbitMq", builder =>
				      {
					      builder.Add(new AnnotationTransportStrategy(["RabbitMq"]));
					      builder.EvaluateIncoming((c, t) => c.EndsWith("Command"));
					      builder.EvaluateOutgoing((c, t) => c.EndsWith("Command"));
				      })
					;
			}

			return Action;
		});
	}

	//public void Configure(IServiceProvider applicationServices, IIdGenerator idGenerator)
	//{
	//  InitData();
	//}

	/// <summary>
	/// 宿主启动完成后的回调，当前不执行额外初始化。
	/// </summary>
	/// <param name="applicationServices">宿主构建完成后的服务提供程序。</param>
	public void Configure(IServiceProvider applicationServices)
	{
		//var config = applicationServices.GetService<IConfiguration>();
	}
}