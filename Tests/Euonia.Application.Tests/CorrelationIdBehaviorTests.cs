using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// <see cref="CorrelationIdBehavior{TMessage,TResponse}"/> 的测试，验证关联 ID 与请求跟踪 ID 在
/// 请求上下文、消息信封和消息头之间的取值优先级，以及管道后续调用的执行。
/// </summary>
public class CorrelationIdBehaviorTests
{
	/// <summary>
	/// 请求上下文存在跟踪标识时，应将其同时写入关联 ID 与请求跟踪 ID 元数据。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithTraceIdentifier_ShouldSetCorrelationAndTraceMetadata()
	{
		var behavior = CreateBehavior("trace-1", null);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("trace-1", context.Metadata[MessageHeaders.CorrelationId]);
		Assert.Equal("trace-1", context.Metadata[MessageHeaders.RequestTraceId]);
	}

	/// <summary>
	/// 没有跟踪标识但存在 <c>Request-Id</c> 头时，应使用该头的值作为关联 ID。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithRequestIdHeader_ShouldUseRequestId()
	{
		var behavior = CreateBehavior(null, "req-id-42");
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("req-id-42", context.Metadata[MessageHeaders.CorrelationId]);
	}

	/// <summary>
	/// 请求上下文不含跟踪标识与请求头时，应优先沿用消息信封上已有的关联 ID。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithoutRequestContext_ShouldPreferExistingEnvelopeCorrelationId()
	{
		var behavior = CreateBehavior(null, null);
		var context = new RoutedMessage<string>("payload", "channel");
		context.CorrelationId = "existing-correlation";

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("existing-correlation", context.Metadata[MessageHeaders.CorrelationId]);
	}

	/// <summary>
	/// 消息元数据中已存在关联 ID 时直接复用，不会被覆盖或重新生成。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithExistingMetadataCorrelationId_ShouldReuseIt()
	{
		var behavior = CreateBehavior(null, null);
		var context = new RoutedMessage<string>("payload", "channel");
		context.Metadata.Set(MessageHeaders.CorrelationId, "meta-correlation");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("meta-correlation", context.Metadata[MessageHeaders.CorrelationId]);
	}

	/// <summary>
	/// 没有任何关联 ID 来源时应生成一个非空的新关联 ID。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithoutAnyCorrelation_ShouldGenerateNewOne()
	{
		var behavior = CreateBehavior(null, null);
		var context = new RoutedMessage<string>("payload", "channel") { CorrelationId = null! };

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		var generated = context.Metadata[MessageHeaders.CorrelationId] as string;
		Assert.False(string.IsNullOrEmpty(generated));
	}

	/// <summary>
	/// 仅存在 <c>X-Correlation-ID</c> 头时使用该头的值作为关联 ID，且不写入请求跟踪 ID。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithCorrelationHeader_ShouldUseXCorrelationHeader()
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				Headers = new Dictionary<string, string> { ["X-Correlation-ID"] = "corr-web" }
			}
		};
		var behavior = new CorrelationIdBehavior<RoutedMessage<string>, bool>(accessor);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("corr-web", context.Metadata[MessageHeaders.CorrelationId]);
		Assert.Empty(context.Metadata[MessageHeaders.RequestTraceId] as string ?? string.Empty);
	}

	/// <summary>
	/// 跟踪标识与 <c>X-Correlation-ID</c> 头同时存在时，跟踪标识优先作为关联 ID 与请求跟踪 ID。
	/// </summary>
	[Fact]
	public async Task HandleAsync_TraceIdentifier_ShouldWinOverCorrelationHeader()
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				TraceIdentifier = "trace-1",
				Headers = new Dictionary<string, string> { ["X-Correlation-ID"] = "corr-web" }
			}
		};
		var behavior = new CorrelationIdBehavior<RoutedMessage<string>, bool>(accessor);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("trace-1", context.Metadata[MessageHeaders.CorrelationId]);
		Assert.Equal("trace-1", context.Metadata[MessageHeaders.RequestTraceId]);
	}

	/// <summary>
	/// 处理完成后应继续调用管道的下一个委托，保证消息不会被吞掉。
	/// </summary>
	[Fact]
	public async Task HandleAsync_ShouldInvokeNext()
	{
		var behavior = CreateBehavior("trace-1", null);
		var context = new RoutedMessage<string>("payload", "channel");
		var invoked = false;

		await behavior.HandleAsync(context, _ =>
		{
			invoked = true;
			return Task.FromResult(true);
		});

		Assert.True(invoked);
	}

	private static CorrelationIdBehavior<RoutedMessage<string>, bool> CreateBehavior(string? traceIdentifier, string? requestId)
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				TraceIdentifier = traceIdentifier!,
				Headers = requestId == null
					? new Dictionary<string, string>()
					: new Dictionary<string, string> { ["Request-Id"] = requestId }
			}
		};

		return new CorrelationIdBehavior<RoutedMessage<string>, bool>(accessor);
	}
}