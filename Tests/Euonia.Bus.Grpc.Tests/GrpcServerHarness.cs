using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Bus.Grpc;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Bus.Grpc.Tests;

/// <summary>
/// 请求-响应调用的消息类型。
/// </summary>
public sealed class CountRequest : IRequest<int>
{
	public int Start { get; set; }
}

/// <summary>
/// 用于测试抛错路径的请求消息。
/// </summary>
public sealed class FailingRequest : IRequest<object>
{
	public string Text { get; set; }
}

/// <summary>
/// 启动真实 gRPC 服务端的测试宿主。
/// </summary>
internal sealed class GrpcServerHarness : IAsyncDisposable
{
	public string Endpoint { get; }

	private readonly WebApplication _app;

	private GrpcServerHarness(WebApplication app, string endpoint)
	{
		_app = app;
		Endpoint = endpoint;
	}

	public static async Task<GrpcServerHarness> StartAsync(Action<IConfigurator> registerChannel)
	{
		var builder = WebApplication.CreateBuilder();
		builder.Logging.AddConsole();
		builder.WebHost.UseKestrel(kestrel =>
		{
			kestrel.Listen(IPAddress.Loopback, 0, endpoint => endpoint.Protocols = HttpProtocols.Http2);
		});

		var services = builder.Services;
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "grpc");
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.AddGrpc();
		services.AddGrpcBusServer();

		var app = builder.Build();

		var configurator = app.Services.GetRequiredService<IConfigurator>();
		configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());

		// 端点映射会提前构造 IHandlerContext 并订阅渠道注册事件，
		// 因此映射之后（如模块初始化阶段）的 RegisterChannel 注册必定生效。
		app.MapGrpcBusService();

		registerChannel(configurator);

		await app.StartAsync();

		return new GrpcServerHarness(app, app.Urls.First());
	}

	public async ValueTask DisposeAsync()
	{
		await _app.StopAsync();
		await _app.DisposeAsync();
	}

	internal static ServiceProvider BuildClientProvider(string endpoint)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddSingleton<DefaultRequestContextAccessor>();
		services.AddSingleton<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddSingleton<IServiceAccessor, ServiceAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "grpc");
		services.TryAddSingleton<IRequestContextAccessor, RequestContextAccessor>();
		services.AddGrpcBus("grpc", options =>
		{
			options.Endpoint = endpoint;
		});

		var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());
		return provider;
	}

	internal static GrpcTransporter BuildTransporter(string endpoint)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddGrpcBus("grpc", options =>
		{
			options.Endpoint = endpoint;
		});
		return services.BuildServiceProvider().GetRequiredService<GrpcTransporter>();
	}
}