using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计写入队列：单读者后台消费 <see cref="IAuditingStore"/>，失败开放。
/// </summary>
/// <remarks>
/// 写入不进业务事务、不阻塞业务线程；队列满时丢弃当前记录并计入 `dropped` 指标（日志限流）。
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
	private readonly AuditingDelivery _successDelivery;
	private readonly SemaphoreSlim _lifecycle = new(1, 1);
	private Channel<PendingRecord> _channel;
	private readonly object _sync = new();
	private TaskCompletionSource _drained = CreateCompletedSource();
	private Task? _loop;
	private CancellationTokenSource? _stopping;
	private int _pending;
	private int _accepting = 1;
	private int _started;
	private int _stopped;
	private int _restartBlocked;
	private long _lastDropLogTimestamp;

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
		_successDelivery = _options.SuccessDelivery;

		_channel = CreateChannel();
	}

	private Channel<PendingRecord> CreateChannel()
	{
		return _options.QueueCapacity > 0
			       ? Channel.CreateBounded<PendingRecord>(new BoundedChannelOptions(_options.QueueCapacity)
			       {
				       // Wait 模式下 TryWrite 在队列满时返回 false，可准确回收入队计数；
				       // DropOldest / DropWrite 会静默丢弃并返回 true，导致 _pending 永不归零、FlushAsync 挂死。
				       FullMode = BoundedChannelFullMode.Wait,
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
		await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			if (Volatile.Read(ref _restartBlocked) == 1)
			{
				throw new InvalidOperationException("Auditing writer cannot restart because the previous stop timed out.");
			}

			if (Interlocked.Exchange(ref _started, 1) == 1)
			{
				return;
			}

			if (Interlocked.Exchange(ref _stopped, 0) == 1)
			{
				// 支持 Start→Stop→Start：上一代通道已关闭，重建并复位计数。
				lock (_sync)
				{
					_channel = CreateChannel();
					_pending = 0;
					_drained = CreateCompletedSource();
				}
			}

			Interlocked.Exchange(ref _accepting, 1);

			// 先启动消费循环，再重放 spool：有界队列由消费者持续排空，避免重放被容量截断。
			_stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_loop = Task.Run(() => ProcessAsync(_stopping.Token, _channel), CancellationToken.None);

			if (_spool != null)
			{
				try
				{
					foreach (var pending in await _spool.LoadPendingAsync(cancellationToken).ConfigureAwait(false))
					{
						await EnqueueReplayAsync(pending.Value, pending.Key, cancellationToken).ConfigureAwait(false);
					}
				}
				catch (Exception exception) when (exception is not OperationCanceledException)
				{
					_logger?.LogError(exception, "Failed to replay auditing records from the durable spool.");
				}
			}
		}
		finally
		{
			_lifecycle.Release();
		}
	}

	/// <inheritdoc />
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		await _lifecycle.WaitAsync(CancellationToken.None).ConfigureAwait(false);
		try
		{
			if (Interlocked.Exchange(ref _stopped, 1) == 1)
			{
				return;
			}

			Interlocked.Exchange(ref _started, 0);
			Interlocked.Exchange(ref _accepting, 0);
			_channel.Writer.TryComplete();

			var completed = _loop == null;

			if (_loop != null)
			{
				// 不使用宿主停止令牌等待循环：令牌取消会让 WaitAsync 立即抛出，而循环可能仍在刷写。
				try
				{
					await _loop.WaitAsync(TimeSpan.FromSeconds(30), CancellationToken.None).ConfigureAwait(false);
					completed = true;
				}
				catch (TimeoutException)
				{
					// 存储挂起：取消内部令牌并再等一次，避免 StopAsync 永久阻塞；仍未结束则跳过释放。
					_stopping?.Cancel();
					try
					{
						await _loop.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None).ConfigureAwait(false);
						completed = true;
					}
					catch (TimeoutException)
					{
						// 旧循环仍在运行：禁止重启，避免两个消费者共享/重建通道。
						Interlocked.Exchange(ref _restartBlocked, 1);
						_stopping?.Dispose();
						_stopping = null;
						_loop = null;
						_logger?.LogWarning("Auditing writer did not stop within the timeout; restart is disabled for this instance.");
					}
				}
			}

			if (completed)
			{
				_stopping?.Dispose();
				_stopping = null;
				_loop = null;
			}
		}
		finally
		{
			_lifecycle.Release();
		}
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		await StopAsync(CancellationToken.None).ConfigureAwait(false);
	}

	private async ValueTask EnqueueReplayAsync(AuditingRecord record, string? spoolKey, CancellationToken cancellationToken)
	{
		var pending = new PendingRecord(record, spoolKey);
		lock (_sync)
		{
			if (_pending++ == 0)
			{
				_drained = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			}
		}

		try
		{
			// 重放使用等待容量，而不是 TryWrite 丢弃。
			await _channel.Writer.WriteAsync(pending, cancellationToken).ConfigureAwait(false);
		}
		catch
		{
			MarkProcessed();
			throw;
		}
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
			if (Volatile.Read(ref _accepting) == 0)
			{
				// 写入器已停止：与“队列满”区分，便于告警判断。
				AuditingMetrics.RecordRejected();
				_logger?.LogDebug("Auditing writer is stopping; record {ScopeId} was rejected.", record.ScopeId);
			}
			else
			{
				AuditingMetrics.RecordDropped();
				LogDrop(record);
			}

			MarkProcessed();
		}
	}

	private void LogDrop(AuditingRecord record)
	{
		// 持续溢出时限制日志频率，避免在业务线程上形成日志风暴。
		var now = Environment.TickCount64;
		var last = Interlocked.Read(ref _lastDropLogTimestamp);
		if (now - last >= 5000 && Interlocked.CompareExchange(ref _lastDropLogTimestamp, now, last) == last)
		{
			_logger?.LogWarning(
				"Auditing queue is full (capacity {Capacity}); record {ScopeId} was dropped.",
				_options.QueueCapacity,
				record.ScopeId);
			return;
		}

		_logger?.LogDebug("Auditing queue is full; record {ScopeId} was dropped.", record.ScopeId);
	}

	private async Task ProcessAsync(CancellationToken cancellationToken, Channel<PendingRecord> channel)
	{
		var buffer = new List<PendingRecord>();

		try
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				if (buffer.Count == 0)
				{
					if (!await channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
					{
						break;
					}
				}

				while (buffer.Count < _batchSize && channel.Reader.TryRead(out var item))
				{
					buffer.Add(item);
				}

				if (buffer.Count >= _batchSize)
				{
					await FlushBufferAsync(buffer, cancellationToken).ConfigureAwait(false);
					continue;
				}

				// 批窗口内继续等待新记录；窗口到期才刷写已到达的部分批次。
				using var delayCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				var waitTask = channel.Reader.WaitToReadAsync(cancellationToken).AsTask();
				var completed = await Task.WhenAny(waitTask, Task.Delay(_batchInterval, delayCancellation.Token)).ConfigureAwait(false);

				if (completed == waitTask)
				{
					delayCancellation.Cancel();
					if (!await waitTask.ConfigureAwait(false))
					{
						// 通道已关闭：刷写剩余记录后退出。
						if (buffer.Count > 0)
						{
							await FlushBufferAsync(buffer, cancellationToken).ConfigureAwait(false);
						}

						break;
					}
				}
				else
				{
					// 批窗口到期。
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
		var succeeded = false;
		var persisted = new HashSet<PendingRecord>();

		try
		{
			// 每批创建一个作用域：内置存储是单例，但宿主注册的 scoped 存储也能正确解析并释放。
			// 作用域创建放在 try 内：ValidateScopes 等异常按批失败处理，不会终止消费循环。
			using var flushScope = _services.GetService(typeof(IServiceScopeFactory)) is IServiceScopeFactory scopeFactory
				                       ? scopeFactory.CreateScope()
				                       : null;
			var services = flushScope?.ServiceProvider ?? _services;

			var router = services.GetService(typeof(IAuditingStoreRouter)) as IAuditingStoreRouter;
			var defaultStore = services.GetService(typeof(IAuditingStore)) as IAuditingStore;

			if (router == null && defaultStore == null)
			{
				_logger?.LogWarning("Auditing is enabled but no IAuditingStore is registered; {Count} record(s) were dropped.", buffer.Count);
				return;
			}

			var allPersisted = true;

			// 先逐条解析存储：单条路由异常或空结果只影响该条记录，不能让 GroupBy 键选择器把整批一起丢掉。
			var resolved = new List<(PendingRecord Pending, IAuditingStore Store)>(buffer.Count);
			var resolutionFailures = 0;
			Exception? firstResolutionException = null;
			foreach (var pending in buffer)
			{
				IAuditingStore? store;
				try
				{
					store = router != null ? router.GetStore(pending.Record) : defaultStore;
				}
				catch (Exception exception)
				{
					// 审计失败开放：路由异常按该条失败处理，事务性 OCE 也不例外，避免消费循环被单条存储异常终止。
					resolutionFailures++;
					firstResolutionException ??= exception;
					continue;
				}

				if (store == null)
				{
					resolutionFailures++;
					continue;
				}

				resolved.Add((pending, store));
			}

			if (resolutionFailures > 0)
			{
				allPersisted = false;

				// 按批聚合日志：持续失败的租户不应让每条记录各刷一条日志。
				if (firstResolutionException != null)
				{
					_logger?.LogError(firstResolutionException, "Failed to resolve auditing store for {Count} record(s); they remain in the durable spool if configured.", resolutionFailures);
				}
				else
				{
					_logger?.LogWarning("No auditing store could be resolved for {Count} record(s); they were dropped.", resolutionFailures);
				}
			}

			// 每租户审计库：按成功解析的存储分组，同一存储的记录合并为一次批量写入。
			foreach (var group in resolved.GroupBy(item => item.Store))
			{
				var store = group.Key;
				var items = group.Select(item => item.Pending).ToArray();

				var immediate = items
				                .Where(item => item.Record.IsSuccess == false
				                               ? _failureDelivery == AuditingDelivery.Immediate
				                               : _successDelivery == AuditingDelivery.Immediate)
				                .ToArray();

				foreach (var pending in immediate)
				{
					try
					{
						await SaveAsync(store, pending.Record, cancellationToken).ConfigureAwait(false);
						persisted.Add(pending);
						await DeleteSpoolSafeAsync(pending).ConfigureAwait(false);
					}
					catch (Exception exception)
					{
						allPersisted = false;
						_logger?.LogError(exception, "Failed to persist auditing record {ScopeId}.", pending.Record.ScopeId);
					}
				}

				var immediateSet = immediate.ToHashSet();
				var batched = items.Where(item => !immediateSet.Contains(item)).ToArray();
				if (batched.Length == 0)
				{
					continue;
				}

				try
				{
					await store.SaveManyAsync(batched.Select(item => item.Record).ToArray(), cancellationToken).ConfigureAwait(false);

					foreach (var pending in batched)
					{
						persisted.Add(pending);
						await DeleteSpoolSafeAsync(pending).ConfigureAwait(false);
					}
				}
				catch (Exception exception)
				{
					// 任何批量失败都退化为逐条写入，避免单条分组失败中止后续存储组。
					_logger?.LogWarning(exception, "Batch auditing write failed; falling back to per-record writes.");
					foreach (var pending in batched)
					{
						try
						{
							await SaveAsync(store, pending.Record, cancellationToken).ConfigureAwait(false);
							persisted.Add(pending);
							await DeleteSpoolSafeAsync(pending).ConfigureAwait(false);
						}
						catch (Exception recordException)
						{
							// 单条失败不阻断后续记录；spool 键保留，便于重启重放。
							allPersisted = false;
							_logger?.LogError(recordException, "Failed to persist auditing record {ScopeId} during fallback.", pending.Record.ScopeId);
						}
					}
				}
			}

			stopwatch.Stop();
			succeeded = allPersisted;
		}
		catch (Exception exception)
		{
			_logger?.LogError(exception, "Failed to persist auditing records; they remain in the durable spool if configured.");
		}
		finally
		{
			stopwatch.Stop();
			var count = Math.Max(1, buffer.Count);
			var elapsedSeconds = stopwatch.Elapsed.TotalSeconds / count;

			foreach (var pending in buffer)
			{
				var persistedRecord = persisted.Contains(pending);
				if (!persistedRecord && pending.SpoolKey == null)
				{
					AuditingMetrics.RecordDropped();
				}

				AuditingMetrics.RecordWrite(persistedRecord, elapsedSeconds);
				MarkProcessed();
			}

			buffer.Clear();
		}
	}

	private async Task DeleteSpoolSafeAsync(PendingRecord pending)
	{
		try
		{
			await DeleteSpoolAsync(pending).ConfigureAwait(false);
		}
		catch (Exception exception)
		{
			// 已落库；删除失败只保留 spool 供下次重放（写入幂等）。
			_logger?.LogWarning(exception, "Failed to delete auditing spool entry {SpoolKey}.", pending.SpoolKey);
		}
	}

	private async Task DeleteSpoolAsync(PendingRecord pending)
	{
		if (_spool != null && pending.SpoolKey != null)
		{
			await _spool.DeleteAsync(pending.SpoolKey, CancellationToken.None).ConfigureAwait(false);
		}
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
			catch (Exception exception) when (exception is not OperationCanceledException && attempt < attempts)
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
