using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Uow;

/// <summary>
/// <see cref="IUnitOfWork"/> 的具体实现，负责管理作用域内的上下文、提交/回滚生命周期以及完成处理器。
/// </summary>
public sealed class UnitOfWork : UnitOfWorkBase, IUnitOfWork
{
	#region Events

	/// <inheritdoc />
	public event EventHandler<UnitOfWorkEventArgs> Completed;

	/// <inheritdoc />
	public event EventHandler<UnitOfWorkFailedEventArgs> Failed;

	#endregion

	#region Fields

	/// <summary>
	/// 来自选项监视器的默认选项，用于补全调用方传入的选项。
	/// </summary>
	private readonly UnitOfWorkOptions _defaultOptions;

	/// <summary>
	/// 按名称键存储的上下文集合，使用并发字典保证线程安全。
	/// </summary>
	private readonly ConcurrentDictionary<string, IUnitOfWorkContext> _contexts = new();

	/// <summary>
	/// 指示当前是否正在执行完成操作，用于防止重入。
	/// </summary>
	private bool _isCompleting;

	#endregion

	#region Ctors

	/// <summary>
	/// 初始化 <see cref="UnitOfWork"/> 类的新实例。
	/// </summary>
	/// <param name="provider">限定于该工作单元作用域的服务提供程序。</param>
	/// <param name="options">提供默认工作单元选项的选项监视器。</param>
	public UnitOfWork(IServiceProvider provider, IOptionsMonitor<UnitOfWorkOptions> options)
	{
		ServiceProvider = provider;
		_defaultOptions = options.CurrentValue;
	}

	#endregion

	#region Properties of IUnitOfWork

	/// <inheritdoc />
	public Guid Id { get; } = Guid.NewGuid();

	/// <inheritdoc />
	public Dictionary<string, object> Items { get; } = new();

	/// <inheritdoc />
	/// <remarks>该只读视图直接基于内部的并发字典，内容随上下文注册而实时变化。</remarks>
	public IReadOnlyDictionary<string, IUnitOfWorkContext> Contexts => _contexts;

	/// <inheritdoc />
	public override IServiceProvider ServiceProvider { get; }

	/// <inheritdoc />
	public IUnitOfWork Outer { get; private set; }

	/// <inheritdoc />
	public bool IsReserved { get; private set; }

	/// <inheritdoc />
	public string ReservationName { get; private set; }

	/// <inheritdoc />
	public bool IsDisposed { get; private set; }

	/// <inheritdoc />
	public bool IsCompleted { get; private set; }

	#endregion

	#region Properties of self

	/// <summary>
	/// 工作单元成功完成后需要依次调用的处理器集合。
	/// </summary>
	private List<Func<Task>> CompletedHandlers { get; } = new();

	/// <inheritdoc />
	public IUnitOfWorkOptions Options { get; private set; }

	/// <summary>
	/// 完成过程中发生失败时捕获的异常。
	/// </summary>
	private Exception Exception { get; set; }

	/// <summary>
	/// 指示该工作单元是否已回滚。
	/// </summary>
	private bool RolledBack { get; set; }

	#endregion

	#region Methods of IUnitOfWork

	/// <inheritdoc />
	/// <remarks>处理器将在工作单元成功完成并保存变更之后依次被调用。</remarks>
	public void OnCompleted(Func<Task> handler)
	{
		CompletedHandlers.Add(handler);
	}

	/// <summary>
	/// 使用指定选项初始化该工作单元，并使用选项监视器中的默认值进行补全。
	/// </summary>
	/// <param name="options">用于配置该工作单元的选项。</param>
	/// <exception cref="ArgumentNullException"><paramref name="options"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="Exception">该工作单元已被初始化过时抛出。</exception>
	/// <remarks>初始化同时会将 <see cref="IsReserved"/> 重置为 <c>false</c>。</remarks>
	public void Initialize(UnitOfWorkOptions options)
	{
		ArgumentNullException.ThrowIfNull(options);

		if (Options != null)
		{
			throw new Exception("This unit of work is already initialized before!");
		}

		Options = _defaultOptions.Normalize(options);

		IsReserved = false;
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentNullException"><paramref name="reservationName"/> 为 <c>null</c> 时抛出。</exception>
	public void Reserve(string reservationName)
	{
		Check.EnsureNotNull(reservationName, nameof(reservationName));

		ReservationName = reservationName;
		IsReserved = true;
	}

	/// <inheritdoc />
	public IUnitOfWorkContext FindContext(string key)
	{
		return _contexts.GetOrDefault(key);
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException"><paramref name="key"/> 为 <c>null</c> 或空字符串时抛出。</exception>
	/// <exception cref="ArgumentNullException"><paramref name="context"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">已存在相同键的上下文，或上下文添加失败时抛出。</exception>
	public void AddContext(string key, IUnitOfWorkContext context)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentNullException.ThrowIfNull(context);

		if (_contexts.ContainsKey(key))
		{
			throw new InvalidOperationException("This unit of work already already contains a context with the key: " + key);
		}

		if (!_contexts.TryAdd(key, context))
		{
			throw new InvalidOperationException("Failed to add context with the key: " + key);
		}
	}

	/// <inheritdoc />
	/// <exception cref="ArgumentException"><paramref name="key"/> 为 <c>null</c> 或空字符串时抛出。</exception>
	/// <exception cref="ArgumentNullException"><paramref name="factory"/> 为 <c>null</c> 时抛出。</exception>
	public IUnitOfWorkContext GetOrAddContext(string key, Func<IUnitOfWorkContext> factory)
	{
		ArgumentException.ThrowIfNullOrEmpty(key);
		ArgumentNullException.ThrowIfNull(factory);

		return _contexts.GetOrAdd(key, _ => factory());
	}

	/// <inheritdoc />
	public void SetOuter(IUnitOfWork outer)
	{
		Outer = outer;
	}

	/// <inheritdoc />
	/// <exception cref="InvalidOperationException">该工作单元已完成或正在完成中时抛出。</exception>
	/// <remarks>
	/// 若该工作单元已回滚则直接返回，不做任何处理；
	/// 完成过程中发生异常会记录到内部字段并重新抛出，随后由释放流程触发失败事件。
	/// </remarks>
	public async Task CompleteAsync(CancellationToken cancellationToken = default)
	{
		if (RolledBack)
		{
			return;
		}

		if (IsCompleted || _isCompleting)
		{
			throw new InvalidOperationException("Completion has already been requested for this unit of work.");
		}

		try
		{
			_isCompleting = true;
			await SaveChangesAsync(cancellationToken);
			IsCompleted = true;
			await OnCompletedAsync();
		}
		catch (Exception exception)
		{
			Exception = exception;
			throw;
		}
	}

	/// <inheritdoc />
	/// <remarks>依次调用每个已注册上下文的 <see cref="IUnitOfWorkContext.SaveChangesAsync(CancellationToken)"/>；若该工作单元已回滚则直接返回。</remarks>
	public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		if (RolledBack)
		{
			return;
		}

		foreach (var (_, context) in Contexts)
		{
			await context.SaveChangesAsync(cancellationToken);
		}
	}

	/// <summary>
	/// 依次调用已注册的完成处理器，并触发 <see cref="Completed"/> 事件。
	/// </summary>
	private async Task OnCompletedAsync()
	{
		foreach (var handler in CompletedHandlers)
		{
			await handler.Invoke();
		}

		Completed?.Invoke(this, new UnitOfWorkEventArgs(this));
	}

	/// <inheritdoc />
	/// <remarks>将 <see cref="RolledBack"/> 置为 <c>true</c> 后依次回滚所有已注册的上下文；已回滚时直接返回。</remarks>
	public async Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		if (RolledBack)
		{
			return;
		}

		RolledBack = true;

		foreach (var (_, context) in Contexts)
		{
			await context.RollbackAsync(cancellationToken);
		}
	}

	#endregion

	#region Methods

	/// <inheritdoc />
	/// <remarks>
	/// 若该工作单元尚未完成或完成过程存在异常，会先触发 <see cref="Failed"/> 事件，
	/// 随后抛出释放通知。重复调用不会重复执行释放逻辑。
	/// </remarks>
	protected override void Dispose(bool disposing)
	{
		if (IsDisposed)
		{
			return;
		}

		IsDisposed = true;

		foreach (var (_, _) in Contexts)
		{
			//context.Dispose();
		}

		if (!IsCompleted || Exception != null)
		{
			OnFailed();
		}

		InvokeDisposedEvent(this, new DisposedEventArgs());
	}

	#endregion

	#region Methods of self

	/// <summary>
	/// 使用捕获的异常与回滚状态触发 <see cref="Failed"/> 事件。
	/// </summary>
	private void OnFailed()
	{
		Failed?.Invoke(this, new UnitOfWorkFailedEventArgs(this, Exception, RolledBack));
	}

	#endregion
}