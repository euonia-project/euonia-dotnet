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
	/// <summary>
	/// 起始值，服务端处理器对其加一后返回。
	/// </summary>
	public int Start { get; set; }
}

/// <summary>
/// 用于测试抛错路径的请求消息。
/// </summary>
public sealed class FailingRequest : IRequest<object>
{
	/// <summary>
	/// 处理器抛出异常时使用的消息文本。
	/// </summary>
	public string Text { get; set; } = string.Empty;
}

/// <summary>
/// 启动真实 gRPC 服务端的测试宿主。
/// </summary>
internal sealed class GrpcServerHarness : IAsyncDisposable
{
	/// <summary>
	/// 获取服务端实际监听的地址（Kestrel 随机端口），客户端据此建立连接。
	/// </summary>
	public string Endpoint { get; }

	private readonly WebApplication _app;

	private GrpcServerHarness(WebApplication app, string endpoint)
	{
		_app = app;
		Endpoint = endpoint;
	}

	/// <summary>
	/// 启动真实 gRPC 服务端并应用测试所需的渠道注册。
	/// </summary>
	/// <param name="registerChannel">在端点映射完成后执行的渠道注册回调。</param>
	/// <returns>已启动的测试宿主，调用方负责释放。</returns>
	/// <remarks>端点映射会提前构造处理器上下文，因此回调中注册的渠道必定能生效。</remarks>
	public static async Task<GrpcServerHarness> StartAsync(Action<IConfigurator> registerChannel)
	{
		var builder = WebApplication.CreateBuilder();
		builder.Logging.AddConsole();
		builder.WebHost.UseKestrel(kestrel =>
		{
			kestrel.Listen(IPAddress.Loopback, 0, endpoint => endpoint.Protocols = HttpProtocols.Http2);
		});

		var services = builder.Services;
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "grpc");
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.Configure<GrpcBusOptions>(options => options.RequireAuthorization = false);
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

	/// <summary>
	/// 停止并释放服务端宿主。
	/// </summary>
	/// <returns>表示释放过程的任务。</returns>
	public async ValueTask DisposeAsync()
	{
		await _app.StopAsync();
		await _app.DisposeAsync();
	}

	/// <summary>
	/// 构建指向指定地址的客户端服务提供程序，使用默认的 gRPC 总线选项。
	/// </summary>
	/// <param name="endpoint">服务端地址。</param>
	/// <returns>已注册 gRPC 总线客户端组件的服务提供程序。</returns>
	internal static ServiceProvider BuildClientProvider(string endpoint)
	{
		return BuildClientProvider(endpoint, null);
	}

	/// <summary>
	/// 构建指向指定地址的客户端服务提供程序，并允许覆写 gRPC 总线选项。
	/// </summary>
	/// <param name="endpoint">服务端地址。</param>
	/// <param name="configure">可选的选项覆写回调。</param>
	/// <returns>已注册 gRPC 总线客户端组件的服务提供程序。</returns>
	internal static ServiceProvider BuildClientProvider(string endpoint, Action<GrpcBusOptions>? configure)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddScoped<DefaultRequestContextAccessor>();
		services.AddScoped<DelegateRequestContextAccessor>(_ => () => new RequestContext());
		services.AddScoped<IServiceAccessor, ServiceAccessor>();
		services.Configure<MessageBusOptions>(options => options.DefaultTransporter = "grpc");
		services.TryAddScoped<IRequestContextAccessor, RequestContextAccessor>();
		services.AddGrpcBus("grpc", options =>
		{
			options.Endpoint = endpoint;
			configure?.Invoke(options);
		});

		var provider = services.BuildServiceProvider();
		var configurator = provider.GetRequiredService<IConfigurator>();
		configurator.SetConvention(convention => convention.Add<DefaultMessageConvention>());
		return provider;
	}

	/// <summary>
	/// 构建指向指定地址的 gRPC 传输器，使用默认选项。
	/// </summary>
	/// <param name="endpoint">服务端地址。</param>
	/// <returns>gRPC 传输器。</returns>
	internal static GrpcTransporter BuildTransporter(string endpoint)
	{
		return BuildTransporter(endpoint, null);
	}

	/// <summary>
	/// 构建指向指定地址的 gRPC 传输器，并允许覆写总线选项。
	/// </summary>
	/// <param name="endpoint">服务端地址。</param>
	/// <param name="configure">可选的选项覆写回调。</param>
	/// <returns>gRPC 传输器。</returns>
	internal static GrpcTransporter BuildTransporter(string endpoint, Action<GrpcBusOptions>? configure)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddOptions();
		services.AddGrpcBus("grpc", options =>
		{
			options.Endpoint = endpoint;
			configure?.Invoke(options);
		});
		return services.BuildServiceProvider().GetRequiredService<GrpcTransporter>();
	}
}