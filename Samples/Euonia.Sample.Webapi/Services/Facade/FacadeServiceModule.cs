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
/// Linkyou 应用程序的外观服务模块。
/// </summary>
/// <remarks>
/// <para>
/// 该模块通过继承 <see cref="ModuleContextBase"/>，参与应用程序的模块化初始化与服务
/// 注册管道。
/// </para>
/// 职责：
/// <para>- 作为应用服务的集中注册点。</para>
/// <para>- 提供添加与应用功能相关初始化逻辑的位置。</para>
/// 依赖项：
/// <para>- 依赖 <see cref="ApplicationModule"/> 以使用核心应用服务与基础设施，
///   并依赖 <see cref="PersistServiceModule"/>、<see cref="BusinessServiceModule"/>
///   和 <see cref="DomainServiceModule"/>，以确保本模块初始化前所需服务已完成注册。
/// </para>
/// 扩展指引：
/// <para>- 在此添加额外的服务注册，或在应用功能需要初始化或配置时重写
///   <see cref="ModuleContextBase"/> 的生命周期方法（如启动/关闭钩子）。
/// </para>
/// </remarks>
/// <seealso cref="ModuleContextBase"/>
[DependsOn(typeof(ApplicationModule), typeof(UnitOfWorkModule))]
[DependsOn(typeof(PersistServiceModule), typeof(BusinessServiceModule), typeof(DomainServiceModule))]
[DependsOn(typeof(MessageBusModule), typeof(InMemoryBusModule), typeof(RabbitMqBusModule))]
public class FacadeServiceModule : ModuleContextBase
{
	/// <summary>
	/// 在常规服务注册之前绑定消息总线配置。
	/// </summary>
	/// <param name="context">服务配置上下文，提供配置与服务注册入口。</param>
	public override void AheadConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.AddOptions<MessageBusOptions>()
		       .BindConfiguration("Euonia:Bus")
		       .Validate(_ => true);
	}

	/// <summary>
	/// 注册外观层服务，包括应用服务上下文、消息序列化器、消息处理器以及消息通道约定与传输策略。
	/// </summary>
	/// <param name="context">服务配置上下文，提供用于注册服务的 <see cref="IServiceCollection"/>。</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.Register<FacadeServiceContext>();
		context.Services.AddKeyedSingleton<IMessageSerializer, SystemTextJsonSerializer>("SystemTestJson");
		context.Services.AddKeyedSingleton<IMessageSerializer, NewtonsoftJsonSerializer>("NewtonsoftJson");
		context.Services.AddMessageHandler(ServiceLifetime.Scoped, typeof(FacadeServiceModule).Assembly);
		context.Services.AddConfiguratorBuilder(config =>
		{
			config.RegisterChannel(typeof(FacadeServiceModule).Assembly)
			      .SetConvention(builder =>
			      {
				      builder.Add<DefaultMessageConvention>();
				      builder.Add<AnnotationMessageConvention>();
				      builder.Add<DomainMessageConvention>();
				      builder.EvaluateMulticast((_, type) => type.Name.EndsWith("Eto"));
			      })
			      .SetStrategy("InMemory", builder =>
			      {
				      builder.Add<LocalMessageTransportStrategy>();
				      builder.EvaluateIncoming((_, _) => true);
				      builder.EvaluateOutgoing((_, _) => true);
			      })
			      .SetStrategy("RabbitMq", builder =>
			      {
				      builder.Add<DistributedMessageTransportStrategy>();
				      builder.Add(new AnnotationTransportStrategy(["RabbitMq"]));
				      builder.EvaluateIncoming((_, type) => type.FullName!.EndsWith("Eto"));
				      builder.EvaluateOutgoing((_, type) => type.FullName!.EndsWith("Eto"));
			      });
		});
	}
}