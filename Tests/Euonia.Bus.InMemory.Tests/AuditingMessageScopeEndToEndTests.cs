using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Messaging;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 消息审计参与者：处理器执行后提交记录，作用域以消息标识生成。
/// </summary>
public class AuditingMessageScopeEndToEndTests
{
	/// <summary>
	/// 处理器结束后写入一条审计记录。
	/// </summary>
	[Fact]
	public async Task ExecutionScope_ShouldSubmitAuditingRecord()
	{
		var writer = new CapturingWriter();
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSingleton<IAuditingWriter>(writer);
		services.AddAuditing(options => options.Enabled = true);
		services.AddTransient<IMessageExecutionScopeParticipant, AuditingMessageScopeParticipant>();

		await using var provider = services.BuildServiceProvider();

		var messageId = Guid.NewGuid().ToString("N");
		var message = new MessageContext
		{
			MessageId = messageId,
			Metadata = new MessageMetadata(),
			Transport = "inmemory",
		};

		var environment = new MessageExecutionEnvironment(provider, null);
		var result = await environment.RunWithResultAsync(
			MessageExecutionDescriptor.DistributedInbound,
			message,
			_ => Task.CompletedTask);

		Assert.Equal(MessageExecutionScopeAction.Continue, result.Action);
		await WaitAsync(() => writer.Records.Count == 1);

		var record = Assert.Single(writer.Records);
		Assert.EndsWith($"msg:{messageId}", record.ScopeId!, StringComparison.Ordinal);
		Assert.Empty(record.Actions);
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

	private sealed class CapturingWriter : IAuditingWriter
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
