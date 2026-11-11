using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 启动期校验多租户配置；失败时快速终止宿主。
/// </summary>
/// <remarks>
/// 只做可静态判定的检查；租户模型扫描由 EF 适配层在首次创建上下文时完成。
/// </remarks>
public sealed class TenantStartupValidator : IHostedService
{
	private readonly IServiceProvider _services;
	private readonly IOptions<MultiTenancyOptions> _options;

	/// <summary>
	/// 初始化 <see cref="TenantStartupValidator"/> 的新实例。
	/// </summary>
	/// <param name="services">服务提供程序。</param>
	/// <param name="options">多租户选项。</param>
	public TenantStartupValidator(IServiceProvider services, IOptions<MultiTenancyOptions> options)
	{
		_services = services;
		_options = options;
	}

	/// <inheritdoc />
	public Task StartAsync(CancellationToken cancellationToken)
	{
		var options = _options.Value;
		if (!options.Enabled)
		{
			return Task.CompletedTask;
		}

		if (_services.GetService<ITenantStore>() == null)
		{
			throw new ConfigurationException(
				"Multi-tenancy is enabled but no ITenantStore is registered. Register a tenant store (for example AddConfigurationTenantStore or AddInMemoryTenantStore) before starting the host.");
		}

		if (options.DefaultIsolation == TenantIsolation.DatabasePerTenant
		    && string.IsNullOrWhiteSpace(options.ConnectionStringTemplate))
		{
			throw new ConfigurationException(
				"DatabasePerTenant isolation requires MultiTenancyOptions.ConnectionStringTemplate or an explicit TenantInfo.ConnectionString for every tenant.");
		}

		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}
