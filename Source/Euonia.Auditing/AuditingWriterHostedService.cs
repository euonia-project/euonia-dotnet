using Microsoft.Extensions.Hosting;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 统一启动宿主注册的审计写入器：只有内置队列写入器需要生命周期管理。
/// </summary>
internal sealed class AuditingWriterHostedService : IHostedService
{
	private readonly IAuditingWriter _writer;

	public AuditingWriterHostedService(IAuditingWriter writer)
	{
		_writer = writer;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		return _writer is AuditingWriter writer ? writer.StartAsync(cancellationToken) : Task.CompletedTask;
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return _writer is AuditingWriter writer ? writer.StopAsync(cancellationToken) : Task.CompletedTask;
	}
}
