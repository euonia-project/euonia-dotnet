using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Auditing;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 消息审计参与者必须遵守“在 Install 中安装环境”的契约。
/// </summary>
public class AuditingMessageScopeParticipantTests
{
	/// <summary>
	/// EnterAsync 不安装作用域；Install 才安装，且消息结束后只产生一条记录。
	/// </summary>
	[Fact]
	public async Task EnterAsync_ShouldInstallScopeOnlyInInstall()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddAuditing(options => options.Enabled = true);
		services.AddInMemoryAuditingStore();

		await using var provider = services.BuildServiceProvider();
		var writer = provider.GetRequiredService<AuditingWriter>();
		await writer.StartAsync(TestContext.Current.CancellationToken);

		var recorder = provider.GetRequiredService<IAuditingRecorder>();
		var participant = new AuditingMessageScopeParticipant();
		var message = new MessageContext { MessageId = "m-audit-1" };

		var entry = await participant.EnterAsync(message, provider, TestContext.Current.CancellationToken);
		Assert.Null(recorder.CurrentScope);

		var lease = entry.Install!();
		Assert.NotNull(lease);
		Assert.NotNull(recorder.CurrentScope);

		lease!.Dispose();
		await recorder.FlushAsync(TestContext.Current.CancellationToken);

		var record = Assert.Single(provider.GetRequiredService<InMemoryAuditingStore>().Records);
		Assert.Contains("msg:m-audit-1", record.ScopeId);
		Assert.Empty(record.Actions);
		Assert.Null(recorder.CurrentScope);

		await writer.StopAsync(TestContext.Current.CancellationToken);
	}

	/// <summary>
	/// 已有外层（HTTP）作用域时，消息参与者创建自有作用域，不提交也不分离外层。
	/// </summary>
	[Fact]
	public async Task Install_ShouldNotHijackAmbientScope()
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddAuditing(options => options.Enabled = true);
		services.AddInMemoryAuditingStore();

		await using var provider = services.BuildServiceProvider();
		var writer = provider.GetRequiredService<AuditingWriter>();
		await writer.StartAsync(TestContext.Current.CancellationToken);
		var recorder = provider.GetRequiredService<IAuditingRecorder>();

		var httpScope = recorder.EnsureScope(new AuditingScopeOptions
		{
			OperationKey = "req:http",
			AutoComplete = false,
		});

		var participant = new AuditingMessageScopeParticipant();
		var entry = await participant.EnterAsync(
			new MessageContext { MessageId = "m-hijack" },
			provider,
			TestContext.Current.CancellationToken);

		var lease = entry.Install!();
		Assert.NotSame(httpScope, recorder.CurrentScope);

		lease!.Dispose();
		await recorder.FlushAsync(TestContext.Current.CancellationToken);

		var store = provider.GetRequiredService<InMemoryAuditingStore>();
		Assert.DoesNotContain(store.Records, record => record.ScopeId!.Contains("req:http"));
		Assert.Same(httpScope, recorder.CurrentScope);

		recorder.Detach(httpScope);
		await writer.StopAsync(TestContext.Current.CancellationToken);
	}
}
