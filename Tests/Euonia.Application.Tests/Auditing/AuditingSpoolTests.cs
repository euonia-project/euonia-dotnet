using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Auditing;

namespace Nerosoft.Euonia.Application.Tests.Auditing;

/// <summary>
/// 审计 durable spool：持久化、重放与落库后清理。
/// </summary>
public sealed class AuditingSpoolTests : IDisposable
{
	private readonly string _directory = Path.Combine(Path.GetTempPath(), "euonia-audit-spool-" + Guid.NewGuid().ToString("N"));

	/// <summary>
	/// 暂存可写入、读取并删除。
	/// </summary>
	[Fact]
	public async Task FileSpool_ShouldPersistLoadAndDelete()
	{
		var spool = new FileAuditingSpool(_directory);
		var record = NewRecord("t1:req:1");

		var key = await spool.PersistAsync(record, TestContext.Current.CancellationToken);
		var pending = await spool.LoadPendingAsync(TestContext.Current.CancellationToken);

		var item = Assert.Single(pending);
		Assert.Equal(key, item.Key);
		Assert.Equal(record.ScopeId, item.Value.ScopeId);

		await spool.DeleteAsync(key, TestContext.Current.CancellationToken);
		Assert.Empty(await spool.LoadPendingAsync(TestContext.Current.CancellationToken));
	}

	/// <summary>
	/// 写入器启动时重放未落库记录，成功后清理暂存文件。
	/// </summary>
	[Fact]
	public async Task Writer_ShouldReplayPendingRecordsOnStart()
	{
		var spool = new FileAuditingSpool(_directory);
		var record = NewRecord("t1:req:replay");
		await spool.PersistAsync(record, TestContext.Current.CancellationToken);

		var store = new InMemoryAuditingStore();
		var services = new ServiceCollection();
		services.AddSingleton<IAuditingStore>(store);
		await using var provider = services.BuildServiceProvider();

		await using var writer = new AuditingWriter(
			provider,
			Options.Create(new AuditingOptions { Enabled = true }),
			logger: null,
			spool);

		await writer.StartAsync(TestContext.Current.CancellationToken);
		await writer.FlushAsync(TestContext.Current.CancellationToken);

		var saved = Assert.Single(store.Records);
		Assert.Equal("t1:req:replay", saved.ScopeId);
		Assert.Empty(await spool.LoadPendingAsync(TestContext.Current.CancellationToken));

		await writer.StopAsync(TestContext.Current.CancellationToken);
	}

	private static AuditingRecord NewRecord(string scopeId)
	{
		return new AuditingRecord
		{
			Id = Guid.CreateVersion7(),
			ScopeId = scopeId,
			ExecutionTime = DateTime.UtcNow,
			RecordedAt = DateTime.UtcNow,
			IsSuccess = true,
		};
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (Directory.Exists(_directory))
		{
			Directory.Delete(_directory, recursive: true);
		}
	}
}
