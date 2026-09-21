using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于激活消息接收器的后台服务。
/// 在应用程序启动时，通过所有已注册的 <see cref="IRecipientRegistrar"/> 实例启动各传输器的消息接收器。
/// </summary>
public class ServiceActivator : BackgroundService
{
	private readonly IServiceProvider _provider;
	private readonly string _defaultTransporter;
	private readonly IConfigurator _configurator;
	private readonly ILogger<ServiceActivator> _logger;

	/// <summary>
	/// 已解析并启动的接收者注册器，停机时需要释放。
	/// </summary>
	private IReadOnlyList<IRecipientRegistrar> _registrars;

	/// <summary>
	/// 初始化 <see cref="ServiceActivator"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于解析 <see cref="IRecipientRegistrar"/> 实例的服务提供程序。</param>
	/// <param name="configurator">消息总线配置器，提供消息注册信息和默认传输器。</param>
	/// <param name="configuration">应用程序配置，用于读取 "Euonia:Bus:DefaultTransport" 配置项。</param>
	public ServiceActivator(IServiceProvider provider, IConfigurator configurator, IConfiguration configuration)
	{
		_provider = provider;
		_configurator = configurator;
		_defaultTransporter = configuration.GetValue<string>(Constants.DefaultTransporterSection);
		_logger = provider.GetService<ILoggerFactory>()?.CreateLogger<ServiceActivator>();
	}

	/// <summary>
	/// 执行所有消息接收器的注册与启动。
	/// 获取所有已注册的 <see cref="IRecipientRegistrar"/> 实例，并并行调用其 <see cref="IRecipientRegistrar.RegisterAsync"/> 方法，
	/// 根据消息注册信息和默认传输器名称启动所有消息通道的接收器。
	/// </summary>
	/// <remarks>
	/// 注册器实例被保留至 <see cref="StopAsync"/>：它们负责创建传输层接收者（broker 消费者 / 订阅者 / 请求执行器），
	/// 必须在停机时释放，否则其持有的连接、通道与会话将随进程存活而泄漏。
	/// 为此注册器应以单例生命周期注册（见各传输的 <c>AddXxxBus</c> 扩展）。
	/// </remarks>
	/// <param name="stoppingToken">应用程序关闭时触发的取消令牌。</param>
	/// <returns>表示所有接收器注册操作并行执行的任务。</returns>
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var builder = _provider.GetService<ConfiguratorBuilder>();

		builder?.Invoke(_configurator);

		var registrations = _configurator.Registrations;

		_registrars = [.. _provider.GetServices<IRecipientRegistrar>()];

		return Task.WhenAll(_registrars.Select(x => x.RegisterAsync(registrations, _defaultTransporter, stoppingToken)));
	}

	/// <summary>
	/// 停机时释放所有已注册的接收者注册器。
	/// </summary>
	/// <param name="cancellationToken">用于取消停止操作的令牌。</param>
	/// <remarks>
	/// 即使某个注册器释放失败也不会中断其余注册器的释放：停机路径上应尽量完成清理，
	/// 失败仅记录警告。
	/// </remarks>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await base.StopAsync(cancellationToken);

		if (_registrars == null)
		{
			return;
		}

		foreach (var registrar in _registrars)
		{
			try
			{
				await registrar.DisposeAsync();
			}
			catch (Exception exception)
			{
				_logger?.LogWarning(exception, "Failed to dispose recipient registrar {Registrar}.", registrar.GetType().FullName);
			}
		}

		_registrars = null;
	}
}