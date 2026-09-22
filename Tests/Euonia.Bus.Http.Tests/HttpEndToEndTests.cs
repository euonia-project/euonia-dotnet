using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Http;
using Nerosoft.Euonia.Bus.Http.Tests;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 端到端测试：通过真实 HTTP 服务器（<c>MapBusEndpoint</c>）+ 完整 <see cref="IBus"/> 调用栈验证 CallAsync 远程调用。
/// </summary>
public class HttpEndToEndTests
{
	[Fact]
	public async Task CallAsync_OverHttpEndpoint_ReturnsResult()
	{
		var port = GetFreePort();
		var builder = WebApplication.CreateBuilder();
		builder.Logging.AddConsole();
		builder.WebHost.UseUrls($"http://localhost:{port}");

		ConfigureServer(builder.Services);

		await using var app = builder.Build();
		var configurator = app.Services.GetRequiredService<IConfigurator>();
		configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());

		// 端点映射会提前构造 IHandlerContext 并订阅渠道注册事件，
		// 因此映射之后（如模块初始化阶段）的 RegisterChannel 注册必定生效。
		app.MapBusEndpoint();

		configurator.RegisterChannel<CountRequest, int>("count", (request, _) => Task.FromResult(request.Start + 1));

		try
		{
			await app.StartAsync(TestContext.Current.CancellationToken);

			await using var clientProvider = BuildClientProvider($"http://localhost:{port}");

			var bus = clientProvider.GetRequiredService<IBus>();
			var result = await bus.CallAsync<CountRequest, int>(
				new CountRequest { Start = 41 },
				new CallOptions { Channel = "count", CorrelationId = "e2e-corr-1", RequestTraceId = "e2e-trace-1" },
				null,
				TestContext.Current.CancellationToken);

			Assert.Equal(42, result);
		}
		finally
		{
			await app.StopAsync(TestContext.Current.CancellationToken);
		}
	}

	[Fact]
	public async Task CallAsync_OverHttpEndpoint_PropagatesHandlerError()
	{
		var port = GetFreePort();
		var builder = WebApplication.CreateBuilder();
		builder.Logging.AddConsole();
		builder.WebHost.UseUrls($"http://localhost:{port}");

		ConfigureServer(builder.Services);

		await using var app = builder.Build();
		var configurator = app.Services.GetRequiredService<IConfigurator>();
		configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());

		// 端点映射会提前构造 IHandlerContext 并订阅渠道注册事件，
		// 因此映射之后（如模块初始化阶段）的 RegisterChannel 注册必定生效。
		app.MapBusEndpoint();

		configurator.RegisterChannel<CountRequest, int>("count", (_, _) => throw new InvalidOperationException("boom"));

		try
		{
			await app.StartAsync(TestContext.Current.CancellationToken);

			await using var clientProvider = BuildClientProvider($"http://localhost:{port}");

			var bus = clientProvider.GetRequiredService<IBus>();
			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => bus.CallAsync<CountRequest, int>(
				new CountRequest { Start = 1 },
				new CallOptions { Channel = "count" },
				null,
				TestContext.Current.CancellationToken));

			Assert.Equal("boom", exception.Message);
		}
		finally
		{
			await app.StopAsync(TestContext.Current.CancellationToken);
		}
	}

	private static void ConfigureServer(IServiceCollection services)
	{
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "http");
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.AddHttpBus("http");
	}

	private static ServiceProvider BuildClientProvider(string endpoint)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "http");
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.AddHttpBus("http", options =>
		{
			options.Endpoint = endpoint;
		});

		var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());
		return provider;
	}

	private static int GetFreePort()
	{
		var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
		listener.Start();
		var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
		listener.Stop();
		return port;
	}
}