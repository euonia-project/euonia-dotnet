using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Messaging;
using Nerosoft.Euonia.Telemetry;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 消息处理 Span：名称、类型、消息标识与租户标签。
/// </summary>
[Collection("BusTelemetry")]
public class BusTelemetryTests
{
	/// <summary>
	/// 处理器执行后产生带租户标签的消费 Span。
	/// </summary>
	[Fact]
	public async Task ExecutionScope_ShouldCreateConsumerSpan()
	{
		var stopped = new List<Activity>();
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Nerosoft.Euonia.Bus",
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity =>
			{
				if (activity.OperationName == TelemetryNames.BusProcess)
				{
					lock (stopped)
					{
						stopped.Add(activity);
					}
				}
			},
		};
		ActivitySource.AddActivityListener(listener);

		var services = new ServiceCollection();
		services.AddLogging();
		services.AddEuoniaTelemetry(options => options.Enabled = true);
		services.AddTenancy(options => options.Enabled = true);
		services.AddInMemoryTenantStore([new TenantInfo { Id = "t1", Name = "T1", Status = TenantStatus.Active }]);
		services.AddTransient<IMessageExecutionScopeParticipant, ExecutionContextMessageScopeParticipant>();
		services.AddTransient<IMessageExecutionScopeParticipant, TenantMessageScopeParticipant>();

		await using var provider = services.BuildServiceProvider();
		var environment = new MessageExecutionEnvironment(provider, null);

		var message = new MessageContext
		{
			MessageId = "m1",
			Metadata = new MessageMetadata { [MessageMetadataKeys.TenantId] = "t1" },
			Transport = "inmemory",
		};

		await environment.RunWithResultAsync(
			MessageExecutionDescriptor.DistributedInbound,
			message,
			_ => Task.CompletedTask);

		Activity activity;
		lock (stopped)
		{
			activity = Assert.Single(stopped);
		}

		Assert.Equal(ActivityKind.Consumer, activity.Kind);
		Assert.Equal("t1", activity.GetTagItem(TelemetryNames.TenantId));
		Assert.Equal("m1", activity.GetTagItem(TelemetryNames.MessagingMessageId));
		Assert.Equal("success", activity.GetTagItem(TelemetryNames.Outcome));
	}
}
