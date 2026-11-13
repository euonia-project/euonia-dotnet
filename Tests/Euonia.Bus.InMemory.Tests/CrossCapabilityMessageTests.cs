using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Messaging;
using Nerosoft.Euonia.Telemetry;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 消息链路上的跨能力协作：租户作用域、审计记录与消费 Span 同时生效。
/// </summary>
[Collection("BusTelemetry")]
public class CrossCapabilityMessageTests
{
	/// <summary>
	/// 一条消息同时产生租户上下文、审计记录与消费 Span，且租户一致。
	/// </summary>
	[Fact]
	public async Task Message_ShouldApplyTenantAuditAndTelemetry()
	{
		var activities = new List<Activity>();
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Nerosoft.Euonia.Bus",
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity =>
			{
				if (activity.OperationName == TelemetryNames.BusProcess)
				{
					lock (activities)
					{
						activities.Add(activity);
					}
				}
			},
		};
		ActivitySource.AddActivityListener(listener);

		var writer = new CapturingAuditWriter();
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IAuditingWriter>(writer);
		services.AddAuditing(options => options.Enabled = true);
		services.AddEuoniaTelemetry(options => options.Enabled = true);
		services.AddTenancy(options => options.Enabled = true);
		services.AddInMemoryTenantStore([new TenantInfo { Id = "t1", Name = "T1", Status = TenantStatus.Active }]);

		services.AddTransient<IMessageExecutionScopeParticipant, ExecutionContextMessageScopeParticipant>();
		services.AddTransient<IMessageExecutionScopeParticipant, TenantMessageScopeParticipant>();
		services.AddTransient<IMessageExecutionScopeParticipant, AuditingMessageScopeParticipant>();

		await using var provider = services.BuildServiceProvider();
		var environment = new MessageExecutionEnvironment(provider, null);

		var message = new MessageContext
		{
			MessageId = "m-cross",
			Metadata = new MessageMetadata { [MessageMetadataKeys.TenantId] = "t1" },
			Transport = "inmemory",
		};

		await environment.RunWithResultAsync(
			MessageExecutionDescriptor.DistributedInbound,
			message,
			_ => Task.CompletedTask);

		await WaitAsync(() => writer.Records.Count == 1);
		var record = Assert.Single(writer.Records);
		Assert.Equal("t1", record.TenantId);

		Activity activity;
		lock (activities)
		{
			activity = Assert.Single(activities);
		}

		Assert.Equal("t1", activity.GetTagItem(TelemetryNames.TenantId));
	}

	/// <summary>
	/// 四种传输的消费 Span 都带 messaging.transport 标签。
	/// </summary>
	[Theory]
	[InlineData("http")]
	[InlineData("grpc")]
	[InlineData("rabbitmq")]
	[InlineData("activemq")]
	public async Task Transport_ShouldBeTaggedOnSpan(string transport)
	{
		Activity? captured = null;
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Nerosoft.Euonia.Bus",
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity =>
			{
				if (activity.OperationName == TelemetryNames.BusProcess)
				{
					captured = activity;
				}
			},
		};
		ActivitySource.AddActivityListener(listener);

		var services = new ServiceCollection();
		services.AddLogging();
		services.AddEuoniaTelemetry(options => options.Enabled = true);
		await using var provider = services.BuildServiceProvider();
		var environment = new MessageExecutionEnvironment(provider, null);

		await environment.RunWithResultAsync(
			MessageExecutionDescriptor.DistributedInbound,
			new MessageContext
			{
				MessageId = $"m-{transport}",
				Metadata = new MessageMetadata(),
				Transport = transport,
			},
			_ => Task.CompletedTask);

		await WaitAsync(() => captured != null);
		Assert.Equal(transport, captured!.GetTagItem(TelemetryNames.MessagingTransport));
	}

	/// <summary>
	/// 重投消息的原始 trace 作为 ActivityLink，而不是父级。
	/// </summary>
	[Fact]
	public async Task RedeliveredMessage_ShouldLinkOriginalTrace()
	{
		Activity? captured = null;
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Nerosoft.Euonia.Bus",
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity =>
			{
				if (activity.OperationName == TelemetryNames.BusProcess)
				{
					captured = activity;
				}
			},
		};
		ActivitySource.AddActivityListener(listener);

		var services = new ServiceCollection();
		services.AddLogging();
		services.AddEuoniaTelemetry(options => options.Enabled = true);
		await using var provider = services.BuildServiceProvider();
		var environment = new MessageExecutionEnvironment(provider, null);

		const string linkTraceId = "0af7651916cd43dd8448eb211c80319c";
		var message = new MessageContext
		{
			MessageId = "m-redelivery",
			Metadata = new MessageMetadata
			{
				[MessageMetadataKeys.LinkTraceParent] = $"00-{linkTraceId}-b7ad6b7169203331-01",
			},
			Transport = "inmemory",
		};

		await environment.RunWithResultAsync(
			MessageExecutionDescriptor.DistributedInbound,
			message,
			_ => Task.CompletedTask);

		await WaitAsync(() => captured != null);
		var link = Assert.Single(captured!.Links);
		Assert.Equal(linkTraceId, link.Context.TraceId.ToHexString());
	}

	private static async Task WaitAsync(Func<bool> condition)
	{
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!condition() && DateTime.UtcNow < deadline)
		{
			await Task.Delay(10, TestContext.Current.CancellationToken);
		}

		Assert.True(condition());
	}

	private sealed class CapturingAuditWriter : IAuditingWriter
	{
		public List<AuditingRecord> Records { get; } = [];

		public ValueTask WriteAsync(AuditingRecord record, CancellationToken cancellationToken = default)
		{
			lock (Records)
			{
				Records.Add(record);
			}

			return ValueTask.CompletedTask;
		}

		public Task FlushAsync(CancellationToken cancellationToken = default)
		{
			return Task.CompletedTask;
		}
	}
}
