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

	/// <summary>
	/// 中5：HTTP/2 场景下头名全小写（头字典忽略大小写）时，行为仍应按规范大小写读取到元数据。
	/// </summary>
	[Fact]
	public async Task HandleAsync_LowercaseHeaders_ShouldReadRequestMetadata()
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				TraceIdentifier = "trace-h2",
				RemoteIpAddress = IPAddress.Parse("10.0.0.9"),
				Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					["x-forwarded-for"] = "203.0.113.9",
					["user-agent"] = "h2-agent",
					["referer"] = "https://h2.example.com",
					["x-client-name"] = "h2-client",
					["x-client-version"] = "2.0.0"
				}
			}
		};
		var behavior = CreateBehavior(accessor);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("trace-h2", context.Metadata["RequestId"]);
		Assert.Equal("203.0.113.9", context.Metadata["IpAddress"]);
		Assert.Equal("h2-agent", context.Metadata["UserAgent"]);
		Assert.Equal("https://h2.example.com", context.Metadata["Referer"]);
		Assert.Equal("h2-client", context.Metadata["AppName"]);
		Assert.Equal("2.0.0", context.Metadata["AppVersion"]);
	}

	/// <summary>
	/// 中6：X-Forwarded-For 为多跳链时应取第一个（最初客户端）地址，而不是整串存入。
	/// </summary>
	[Fact]
	public async Task HandleAsync_MultiHopForwardedChain_ShouldUseFirstHop()
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				RemoteIpAddress = IPAddress.Parse("10.0.0.1"),
				Headers = new Dictionary<string, string>
				{
					["X-Forwarded-For"] = "203.0.113.1, 198.51.100.2, 10.0.0.9"
				}
			}
		};
		var behavior = CreateBehavior(accessor);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("203.0.113.1", context.Metadata["IpAddress"]);
	}

	/// <summary>
	/// 中6：代理头值非法时应继续尝试下一来源，而不是把非法字符串当作 IP 写入。
	/// </summary>
	[Fact]
	public async Task HandleAsync_InvalidForwardedValue_ShouldTryNextHeader()
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				RemoteIpAddress = IPAddress.Parse("10.0.0.1"),
				Headers = new Dictionary<string, string>
				{
					["X-Forwarded-For"] = "not-an-ip",
					["X-Real-IP"] = "198.51.100.7"
				}
			}
		};
		var behavior = CreateBehavior(accessor);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("198.51.100.7", context.Metadata["IpAddress"]);
	}

	/// <summary>
	/// 中6：所有代理头均非法时回退到 RemoteIpAddress。
	/// </summary>
	[Fact]
	public async Task HandleAsync_AllProxyHeadersInvalid_ShouldFallbackToRemoteIpAddress()
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				RemoteIpAddress = IPAddress.Parse("10.0.0.3"),
				Headers = new Dictionary<string, string>
				{
					["X-Forwarded-For"] = "bogus",
					["x-client-ip"] = "999.999.1.1",
					["X-Real-IP"] = "<script>"
				}
			}
		};
		var behavior = CreateBehavior(accessor);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("10.0.0.3", context.Metadata["IpAddress"]);
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
