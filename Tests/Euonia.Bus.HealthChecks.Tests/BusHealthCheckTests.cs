using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus.HealthChecks;

namespace Nerosoft.Euonia.Bus.HealthChecks.Tests;

/// <summary>
/// 针对 <see cref="BusHealthCheck"/> 的测试。
/// </summary>
public class BusHealthCheckTests
{
	[Fact]
	public async Task CheckHealthAsync_WithNoBacklog_ReturnsHealthy()
	{
		await using var provider = BuildProvider(configure: null);
		var result = await RunAsync(provider);

		Assert.Equal(HealthStatus.Healthy, result.Status);
		Assert.Equal(0, result.Data["transports"]);
	}

	[Fact]
	public async Task CheckHealthAsync_WhenDeadLettersExceedThreshold_ReturnsUnhealthy()
	{
		var store = new InMemoryDeadLetterStore();
		store.Add(new DeadLetterEntry
		{
			MessageId = "dead-1",
			Channel = "test.events",
			Source = DeadLetterSource.Outbox,
			Target = "test",
		});

		await using var provider = BuildProvider(configure: null, deadLetterStore: store);
		var result = await RunAsync(provider);

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
		Assert.Contains("Dead letter backlog", result.Description);
	}

	[Fact]
	public async Task CheckHealthAsync_WhenDeadLettersWithinThreshold_ReturnsHealthy()
	{
		var store = new InMemoryDeadLetterStore();
		store.Add(new DeadLetterEntry { MessageId = "dead-1", Channel = "test.events", Source = DeadLetterSource.Inbox, Target = "handler" });

		await using var provider = BuildProvider(options => options.MaxDeadLetters = 5, store);
		var result = await RunAsync(provider);

		Assert.Equal(HealthStatus.Healthy, result.Status);
		Assert.Equal(1, result.Data["deadLetters"]);
	}

	/// <summary>
	/// 未注册死信存储时该项不参与判定，也不应抛异常。
	/// </summary>
	[Fact]
	public async Task CheckHealthAsync_WithoutDeadLetterStore_OmitsDeadLetterData()
	{
		await using var provider = BuildProvider(configure: null);
		var result = await RunAsync(provider);

		Assert.Equal(HealthStatus.Healthy, result.Status);
		Assert.False(result.Data.ContainsKey("deadLetters"));
	}

	[Fact]
	public async Task CheckHealthAsync_WhenTransporterRequiredButMissing_ReturnsUnhealthy()
	{
		await using var provider = BuildProvider(options => options.RequireTransporter = true);
		var result = await RunAsync(provider);

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
		Assert.Contains("No transporter is configured", result.Description);
	}

	[Fact]
	public async Task CheckHealthAsync_WhenBusNotRegistered_ReturnsUnhealthy()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddSingleton<IHealthCheck, BusHealthCheck>();

		await using var provider = services.BuildServiceProvider();
		var result = await RunAsync(provider);

		Assert.Equal(HealthStatus.Unhealthy, result.Status);
		Assert.Contains("IConfigurator", result.Description);
	}

	private static Task<HealthCheckResult> RunAsync(IServiceProvider provider)
	{
		var check = new BusHealthCheck(provider, provider.GetRequiredService<IOptions<BusHealthCheckOptions>>());
		return check.CheckHealthAsync(new HealthCheckContext(), TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// 构造一个注册了总线核心服务的最小容器；传输器数量为 0，因为没有配置任何传输策略。
	/// </summary>
	private static ServiceProvider BuildProvider(Action<BusHealthCheckOptions> configure, IDeadLetterStore deadLetterStore = null)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddEuoniaBus();

		if (configure != null)
		{
			services.Configure(configure);
		}

		if (deadLetterStore != null)
		{
			services.AddSingleton(deadLetterStore);
		}

		return services.BuildServiceProvider();
	}
}
