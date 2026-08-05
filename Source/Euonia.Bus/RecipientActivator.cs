using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于激活消息接收器的后台服务。
/// 在应用程序启动时，通过所有已注册的 <see cref="IRecipientRegistrar"/> 实例启动各传输器的消息接收器。
/// </summary>
public class RecipientActivator : BackgroundService
{
	private readonly IServiceProvider _provider;
	private readonly string _defaultTransporter;
	private readonly IConfigurator _configurator;

	/// <summary>
	/// 初始化 <see cref="RecipientActivator"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析 <see cref="IRecipientRegistrar"/> 实例的服务提供程序。</param>
	/// <param name="configurator">消息总线配置器，提供消息注册信息和默认传输器。</param>
	/// <param name="configuration">应用程序配置，用于读取 "Euonia:Bus:DefaultTransport" 配置项。</param>
	public RecipientActivator(IServiceProvider provider, IConfigurator configurator, IConfiguration configuration)
	{
		_provider = provider;
		_configurator = configurator;
		_defaultTransporter = configuration.GetValue<string>(Constants.DefaultTransporterSection);
	}

	/// <summary>
	/// 执行所有消息接收器的注册与启动。
	/// 获取所有已注册的 <see cref="IRecipientRegistrar"/> 实例，并并行调用其 <see cref="IRecipientRegistrar.RegisterAsync"/> 方法，
	/// 根据消息注册信息和默认传输器名称启动所有消息通道的接收器。
	/// </summary>
	/// <param name="stoppingToken">应用程序关闭时触发的取消令牌。</param>
	/// <returns>表示所有接收器注册操作并行执行的任务。</returns>
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var registrations = _configurator.Registrations;

		var registrars = _provider.GetServices<IRecipientRegistrar>();

		return Task.WhenAll(registrars.Select(x => x.RegisterAsync(registrations, _defaultTransporter, stoppingToken)));
	}
}