using System.Threading.Channels;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计写入队列：单读者后台消费 <see cref="IAuditingStore"/>，失败开放。
/// </summary>
/// <remarks>
/// 写入不进业务事务、不阻塞业务线程；队列积压时按容量丢弃最旧记录并记录日志。
/// 注册 <see cref="IAuditingSpool"/> 后先落暂存再入队，落库成功后清理，进程崩溃可重放。
/// 未注册 <see cref="IAuditingStore"/> 时本类型退化为丢弃并告警，保证可选依赖不阻断宿主启动。
/// </remarks>
public sealed class AuditingWriter : IAuditingWriter, IHostedService, IAsyncDisposable
{
	private readonly IServiceProvider _services;
	private readonly AuditingOptions _options;
	private readonly ILogger<AuditingWriter>? _logger;
	private readonly IAuditingSpool? _spool;
	private readonly int _batchSize;
	private readonly TimeSpan _batchInterval;
	private readonly AuditingDelivery _failureDelivery;
	private readonly Channel<PendingRecord> _channel;
	private readonly object _sync = new();
	private TaskCompletionSource _drained = CreateCompletedSource();
	private Task? _loop;
	private CancellationTokenSource? _stopping;
	private int _pending;

	/// <summary>
	/// 初始化 <see cref="AuditingWriter"/> 的新实例。
	/// </summary>
	/// <param name="services">服务提供程序，用于解析 <see cref="IAuditingStore"/>。</param>
	/// <param name="options">审计选项。</param>
	/// <param name="logger">日志记录器。</param>
	/// <param name="spool">可选的持久化暂存。</param>
	public AuditingWriter(
		IServiceProvider services,
		IOptions<AuditingOptions> options,
		ILogger<AuditingWriter>? logger = null,
		IAuditingSpool? spool = null)
	{
		_services = services;
		_options = options.Value;
		_logger = logger;
		_spool = spool;
		_batchSize = Math.Max(1, _options.BatchSize);
		_batchInterval = _options.BatchInterval > TimeSpan.Zero ? _options.BatchInterval : TimeSpan.FromSeconds(1);
		_failureDelivery = _options.FailureDelivery;

		_channel = _options.QueueCapacity > 0
			           ? Channel.CreateBounded<PendingRecord>(new BoundedChannelOptions(_options.QueueCapacity)
			           {
				           FullMode = BoundedChannelFullMode.DropOldest,
				           SingleReader = true,
			           })
			           : Channel.CreateUnbounded<PendingRecord>(new UnboundedChannelOptions
			           {
				           SingleReader = true,
			           });
	}

	/// <inheritdoc />
	public async ValueTask WriteAsync(AuditingRecord record, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(record);

		string? spoolKey = null;
		if (_spool != null)
		{
			try
			{
				spoolKey = await _spool.PersistAsync(record, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception exception)
			{
				_logger?.LogError(exception, "Failed to persist auditing record {ScopeId} to the durable spool.", record.ScopeId);
			}
		}

		Enqueue(record, spoolKey);
	}

	/// <inheritdoc />
	public async Task FlushAsync(CancellationToken cancellationToken = default)
	{
		while (true)
		{
			Task drained;
			lock (_sync)
			{
				if (_pending == 0)
				{
					return;
				}

				drained = _drained.Task;
			}

			await drained.WaitAsync(cancellationToken).ConfigureAwait(false);
		}
	}

	/// <inheritdoc />
	public async Task StartAsync(CancellationToken cancellationToken)
	{
		if (_spool != null)
		{
			try
			{
				foreach (var pending in await _spool.LoadPendingAsync(cancellationToken).ConfigureAwait(false))
				{
					Enqueue(pending.Value, pending.Key);
				}
			}
			catch (Exception exception)
			{
				_logger?.LogError(exception, "Failed to replay auditing records from the durable spool.");
			}
		}

		_stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		_loop = Task.Run(() => ProcessAsync(_stopping.Token), CancellationToken.None);
	}

	/// <inheritdoc />
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		_channel.Writer.TryComplete();

		if (_loop != null)
		{
			await _loop.WaitAsync(cancellationToken).ConfigureAwait(false);
		}

		_stopping?.Dispose();
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await StopAsync(CancellationToken.None).ConfigureAwait(false);
	}

	private void Enqueue(AuditingRecord record, string? spoolKey)
	{
		lock (_sync)
		{
			if (_pending++ == 0)
			{
				_drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			}
		}

		if (!_channel.Writer.TryWrite(new PendingRecord(record, spoolKey)))
		{
			MarkProcessed();
		}
	}

	private async Task ProcessAsync(CancellationToken cancellationToken)
	{
		var buffer = new List<PendingRecord>();

		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				var waitTask = _channel.Reader.WaitToReadAsync(cancellationToken).AsTask();
				var completed = await Task.WhenAny(waitTask, Task.Delay(_batchInterval, cancellationToken)).ConfigureAwait(false);

				if (completed == waitTask)
				{
					if (!await waitTask.ConfigureAwait(false))
					{
						// 通道已关闭：结束循环并刷写剩余记录。
						break;
					}

					while (buffer.Count < _batchSize && _channel.Reader.TryRead(out var item))
					{
						buffer.Add(item);
					}

					// 缓冲已满则按批刷写；未满说明当前没有更多可读记录，同样立即刷写（低延迟）。
					if (buffer.Count > 0)
					{
						await FlushBufferAsync(buffer, cancellationToken).ConfigureAwait(false);
					}
				}
				else if (buffer.Count > 0)
				{
					// 批窗口到期，刷写已缓存的记录。
					await FlushBufferAsync(buffer, cancellationToken).ConfigureAwait(false);
				}
			}
		}
		catch (OperationCanceledException)
		{
			// 宿主停止：在 finally 中尽力刷写。
		}
		finally
		{
			if (buffer.Count > 0)
			{
				await FlushBufferAsync(buffer, CancellationToken.None).ConfigureAwait(false);
			}
		}
	}

	private async Task FlushBufferAsync(List<PendingRecord> buffer, CancellationToken cancellationToken)
	{
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		try
		{
			var router = _services.GetService(typeof(IAuditingStoreRouter)) as IAuditingStoreRouter;
			var defaultStore = _services.GetService(typeof(IAuditingStore)) as IAuditingStore;

			if (router == null && defaultStore == null)
			{
				_logger?.LogWarning("Auditing is enabled but no IAuditingStore is registered; {Count} record(s) were dropped.", buffer.Count);
				return;
			}

			// 每租户审计库：按路由把记录分组，同一存储的记录合并为一次批量写入。
			foreach (var group in buffer.GroupBy(item => router?.GetStore(item.Record) ?? defaultStore))
			{
				var store = group.Key;
				var items = group.ToArray();

				if (store == null)
				{
					_logger?.LogWarning("No auditing store could be resolved for {Count} record(s); they were dropped.", items.Length);
					continue;
				}

				var immediate = items
				                .Where(item => _failureDelivery == AuditingDelivery.Immediate && item.Record.IsSuccess == false)
				                .ToArray();

				foreach (var pending in immediate)
				{
					await SaveAsync(store, pending.Record, cancellationToken).ConfigureAwait(false);
					await DeleteSpoolAsync(pending).ConfigureAwait(false);
				}

				var batched = items.Where(item => !immediate.Contains(item)).ToArray();
				if (batched.Length == 0)
				{
					continue;
				}

				try
				{
					await store.SaveManyAsync(batched.Select(item => item.Record).ToArray(), cancellationToken).ConfigureAwait(false);

					foreach (var pending in batched)
					{
						await DeleteSpoolAsync(pending).ConfigureAwait(false);
					}
				}
				catch (Exception exception) when (batched.Length > 1)
				{
					// 批量失败退化为逐条写入，避免一条坏记录拖垮整批。
					_logger?.LogWarning(exception, "Batch auditing write failed; falling back to per-record writes.");
					foreach (var pending in batched)
					{
						await SaveAsync(store, pending.Record, cancellationToken).ConfigureAwait(false);
						await DeleteSpoolAsync(pending).ConfigureAwait(false);
					}
				}
			}

			stopwatch.Stop();
			foreach (var _ in buffer)
			{
				AuditingMetrics.RecordWrite(true, stopwatch.Elapsed.TotalSeconds / buffer.Count);
			}
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Failed to persist auditing records; they remain in the durable spool if configured.");
		}
		finally
		{
			foreach (var _ in buffer)
			{
				MarkProcessed();
			}

			buffer.Clear();
		}
	}

	private async Task DeleteSpoolAsync(PendingRecord pending)
	{
		if (_spool != null && pending.SpoolKey != null)
		{
			await _spool.DeleteAsync(pending.SpoolKey, CancellationToken.None).ConfigureAwait(false);
		}
	}

	private async Task SaveAsync(AuditingRecord record, CancellationToken cancellationToken)
	{
		var store = _services.GetService(typeof(IAuditingStore)) as IAuditingStore;
		if (store == null)
		{
			_logger?.LogWarning("Auditing is enabled but no IAuditingStore is registered; the record {ScopeId} was dropped.", record.ScopeId);
			return;
		}

		await SaveAsync(store, record, cancellationToken).ConfigureAwait(false);
	}

	private async Task SaveAsync(IAuditingStore store, AuditingRecord record, CancellationToken cancellationToken)
	{
		var attempts = Math.Max(0, _options.WriteRetryCount) + 1;
		for (var attempt = 1; ; attempt++)
		{
			try
			{
				await store.SaveAsync(record, cancellationToken).ConfigureAwait(false);
				return;
			}
			catch (Exception) when (attempt < attempts)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken).ConfigureAwait(false);
			}
		}
	}

	private void MarkProcessed()
	{
		lock (_sync)
		{
			if (_pending <= 0)
			{
				return;
			}

			if (--_pending == 0)
			{
				_drained.TrySetResult();
			}
		}
	}

	private static TaskCompletionSource CreateCompletedSource()
	{
		var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		source.TrySetResult();
		return source;
	}

	private sealed record PendingRecord(AuditingRecord Record, string? SpoolKey);
}
