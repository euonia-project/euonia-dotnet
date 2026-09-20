using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Application.Tests;

public class CorrelationIdBehaviorTests
{
	[Fact]
	public async Task HandleAsync_WithTraceIdentifier_ShouldSetCorrelationAndTraceMetadata()
	{
		var behavior = CreateBehavior("trace-1", null);
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("trace-1", context.Metadata[MessageHeaders.CorrelationId]);
		Assert.Equal("trace-1", context.Metadata[MessageHeaders.RequestTraceId]);
	}

	[Fact]
	public async Task HandleAsync_WithRequestIdHeader_ShouldUseRequestId()
	{
		var behavior = CreateBehavior(null, "req-id-42");
		var context = new RoutedMessage<string>("payload", "channel");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("req-id-42", context.Metadata[MessageHeaders.CorrelationId]);
	}

	[Fact]
	public async Task HandleAsync_WithoutRequestContext_ShouldPreferExistingEnvelopeCorrelationId()
	{
		var behavior = CreateBehavior(null, null);
		var context = new RoutedMessage<string>("payload", "channel");
		context.CorrelationId = "existing-correlation";

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("existing-correlation", context.Metadata[MessageHeaders.CorrelationId]);
	}

	[Fact]
	public async Task HandleAsync_WithExistingMetadataCorrelationId_ShouldReuseIt()
	{
		var behavior = CreateBehavior(null, null);
		var context = new RoutedMessage<string>("payload", "channel");
		context.Metadata.Set(MessageHeaders.CorrelationId, "meta-correlation");

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("meta-correlation", context.Metadata[MessageHeaders.CorrelationId]);
	}

	[Fact]
	public async Task HandleAsync_WithoutAnyCorrelation_ShouldGenerateNewOne()
	{
		var behavior = CreateBehavior(null, null);
		var context = new RoutedMessage<string>("payload", "channel") { CorrelationId = null };

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		var generated = context.Metadata[MessageHeaders.CorrelationId] as string;
		Assert.False(string.IsNullOrEmpty(generated));
	}

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

	private static CorrelationIdBehavior<RoutedMessage<string>, bool> CreateBehavior(string traceIdentifier, string requestId)
	{
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext
			{
				TraceIdentifier = traceIdentifier,
				Headers = requestId == null
					? new Dictionary<string, string>()
					: new Dictionary<string, string> { ["Request-Id"] = requestId }
			}
		};

		return new CorrelationIdBehavior<RoutedMessage<string>, bool>(accessor);
	}
}