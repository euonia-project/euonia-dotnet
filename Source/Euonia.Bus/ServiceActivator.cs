using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于激活消息接收器的后台服务。
/// 在应用程序启动时，通过所有已注册的 <see cref="IRecipientRegistrar"/> 实例启动各传输器的消息接收器。
/// </summary>
public class ServiceActivator : BackgroundService
{
	private readonly IServiceProvider _provider;
	private readonly MessageBusOptions _options;
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
	/// <param name="options">消息总线配置选项。</param>
	/// <remarks>
	/// 默认传输器取自 <see cref="MessageBusOptions.DefaultTransporter"/>，与分发时所用的来源一致。
	/// 此前此处直接读取原始配置节 <c>Euonia:Bus:DefaultTransporter</c>，
	/// 与选项绑定各读一份，配置键写法一旦不一致（例如写成 <c>DefaultTransport</c>）就会静默失效。
	/// </remarks>
	public ServiceActivator(IServiceProvider provider, IConfigurator configurator, IOptions<MessageBusOptions> options)
	{
		_provider = provider;
		_configurator = configurator;
		_options = options?.Value ?? new MessageBusOptions();
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
		// 自动装配须在用户配置委托之前：这样同一处理器被显式再次注册时会被幂等去重，
		// 而不是产生两份注册（多播场景下会被执行两次）。
		AutoRegisterHandlers();

		var builder = _provider.GetService<ConfiguratorBuilder>();

		builder?.Invoke(_configurator);

		var registrations = _configurator.Registrations;

		_registrars = [.. _provider.GetServices<IRecipientRegistrar>()];

		return Task.WhenAll(_registrars.Select(x => x.RegisterAsync(registrations, _options.DefaultTransporter, stoppingToken)));
	}

	/// <summary>
	/// 按 <see cref="MessageBusOptions.AutoLoadAssemblies"/> 配置扫描并注册处理器。
	/// </summary>
	/// <remarks>
	/// 配置为空时不执行任何操作。程序集名称无法解析时**显式失败**：
	/// 配置写错属于部署期错误，静默跳过会让处理器"注册了却没生效"难以排查。
	/// </remarks>
	/// <exception cref="InvalidOperationException">当配置的程序集无法加载时抛出。</exception>
	private void AutoRegisterHandlers()
	{
		var assemblyNames = _options.AutoLoadAssemblies;
		if (assemblyNames == null || assemblyNames.Length == 0)
		{
			return;
		}

		var types = new List<Type>();

		foreach (var assemblyName in assemblyNames)
		{
			if (string.IsNullOrWhiteSpace(assemblyName))
			{
				continue;
			}

			Assembly assembly;
			try
			{
				assembly = Assembly.Load(new AssemblyName(assemblyName));
			}
			catch (Exception exception)
			{
				throw new InvalidOperationException($"Failed to auto-load the assembly '{assemblyName}' configured in '{Constants.ConfigurationSection}:AutoLoadAssemblies'.", exception);
			}

			types.AddRange(assembly.DefinedTypes);
		}

		if (types.Count == 0)
		{
			return;
		}

		_logger?.LogInformation("Auto-registering message handlers from {Count} type(s) in {AssemblyCount} configured assembly/assemblies.", types.Count, assemblyNames.Length);

		_configurator.RegisterChannel(types);
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
