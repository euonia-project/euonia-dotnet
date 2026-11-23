using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 针对 <see cref="RequestInfoBehavior{TMessage,TResponse}"/> 请求元数据写入行为的测试。
/// </summary>
public class RequestInfoBehaviorTests
{
	/// <summary>
	/// 请求上下文存在时应写入追踪标识、IP、User-Agent、Referer 与客户端信息。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithRequestContext_ShouldWriteRequestMetadata()
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				TraceIdentifier = "trace-1",
				RemoteIpAddress = IPAddress.Parse("10.0.0.1"),
				Headers = new Dictionary<string, string>
				{
					["X-Forwarded-For"] = "203.0.113.1",
					["User-Agent"] = "test-agent",
					["Referer"] = "https://example.com",
					["x-client-name"] = "web",
					["x-client-version"] = "1.2.3"
				}
			}
		};
		var behavior = CreateBehavior(accessor);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("trace-1", context.Metadata["RequestId"]);
		Assert.Equal("203.0.113.1", context.Metadata["IpAddress"]);
		Assert.Equal("test-agent", context.Metadata["UserAgent"]);
		Assert.Equal("https://example.com", context.Metadata["Referer"]);
		Assert.Equal("web", context.Metadata["AppName"]);
		Assert.Equal("1.2.3", context.Metadata["AppVersion"]);
	}

	/// <summary>
	/// 没有代理头时应回退到请求上下文的远程 IP。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithoutForwardedHeader_ShouldUseRemoteIpAddress()
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				RemoteIpAddress = IPAddress.Parse("10.0.0.2"),
				Headers = new Dictionary<string, string>()
			}
		};
		var behavior = CreateBehavior(accessor);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("10.0.0.2", context.Metadata["IpAddress"]);
	}

	/// <summary>
	/// 请求上下文不存在时不写入元数据，并继续执行下一个管道委托。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithoutRequestContext_ShouldSkipMetadataAndInvokeNext()
	{
		var behavior = CreateBehavior(new StubRequestContextAccessor { Context = null });
		var context = new RoutedMessage<string>("payload", "channel");
		var invoked = false;

		await behavior.HandleAsync(context, _ =>
		{
			invoked = true;
			return Task.FromResult(true);
		});

		Assert.True(invoked);
		Assert.False(context.Metadata.ContainsKey("RequestId"));
		Assert.False(context.Metadata.ContainsKey("IpAddress"));
		Assert.False(context.Metadata.ContainsKey("UserAgent"));
	}

	/// <summary>
	/// 请求头缺失时不应写入空值，避免下游把空字符串当作有效请求信息。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithoutHeaders_ShouldNotWriteEmptyValues()
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext { Headers = new Dictionary<string, string>() }
		};
		var behavior = CreateBehavior(accessor);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.False(context.Metadata.ContainsKey("UserAgent"));
		Assert.False(context.Metadata.ContainsKey("Referer"));
		Assert.False(context.Metadata.ContainsKey("AppName"));
		Assert.False(context.Metadata.ContainsKey("AppVersion"));
	}

	private static RequestInfoBehavior<RoutedMessage<string>, bool> CreateBehavior(IRequestContextAccessor? accessor)
	{
		var services = new ServiceCollection();

		if (accessor is not null)
		{
			services.AddSingleton(accessor);
		}

		return new RequestInfoBehavior<RoutedMessage<string>, bool>(services.BuildServiceProvider());
	}
}
