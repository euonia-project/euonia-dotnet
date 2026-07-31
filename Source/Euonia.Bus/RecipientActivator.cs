using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The background service to activate the recipients.
/// </summary>
public class RecipientActivator : BackgroundService
{
	private readonly IServiceProvider _provider;
	private readonly string _defaultTransport;
	private readonly IConfigurator _configurator;

	/// <summary>
	/// Initializes a new instance of the <see cref="RecipientActivator"/> class.
	/// </summary>
	/// <param name="provider"></param>
	/// <param name="configurator"></param>
	/// <param name="configuration"></param>
	public RecipientActivator(IServiceProvider provider, IConfigurator configurator, IConfiguration configuration)
	{
		_provider = provider;
		_configurator = configurator;
		_defaultTransport = string.Collapse(configuration.GetValue<string>("Euonia:Bus:DefaultTransport"), configurator.DefaultTransporter);
	}

	/// <inheritdoc/>
	protected override Task ExecuteAsync(CancellationToken stoppingToken)
	{
		var registrations = _configurator.Registrations;

		var registrars = _provider.GetServices<IRecipientRegistrar>();

		return Task.WhenAll(registrars.Select(x => x.RegisterAsync(registrations, _defaultTransport, stoppingToken)));
	}
}