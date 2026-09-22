namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 子工作单元，作为父工作单元的轻量级代理，用于嵌套工作单元场景。
/// </summary>
/// <remarks>
/// <para>当开启新工作单元时若已存在当前工作单元且未要求新建，则返回该代理实例（见 <see cref="IUnitOfWorkManager.Begin(UnitOfWorkOptions, bool)"/>）。</para>
/// <para>该实例将状态、上下文与生命周期操作全部委托给父工作单元，因此与父级共享同一组上下文；
/// 其自身不拥有任何需要释放的资源，<see cref="CompleteAsync"/> 亦为空操作，实际的提交与释放由根工作单元负责。</para>
/// <para>父工作单元的事件会被转发到该实例，以便调用方按子作用域订阅。</para>
/// </remarks>
internal class ChildUnitOfWork : UnitOfWorkBase, IUnitOfWork
{
	/// <inheritdoc />
	public event EventHandler<UnitOfWorkEventArgs> Completed;

	/// <inheritdoc />
	public event EventHandler<UnitOfWorkFailedEventArgs> Failed;

	/// <inheritdoc />
	public Guid Id { get; } = Guid.NewGuid();

	/// <inheritdoc />
	public Dictionary<string, object> Items => _parent.Items;

	/// <inheritdoc />
	public IReadOnlyDictionary<string, IUnitOfWorkContext> Contexts => _parent.Contexts;

	/// <inheritdoc />
	public override IServiceProvider ServiceProvider => _parent.ServiceProvider;

	/// <inheritdoc />
	public IUnitOfWorkOptions Options => _parent.Options;

	/// <inheritdoc />
	public IUnitOfWork Outer => _parent.Outer;

	/// <inheritdoc />
	public bool IsReserved => _parent.IsReserved;

	/// <inheritdoc />
	public bool IsDisposed => _parent.IsDisposed;

	/// <inheritdoc />
	public bool IsCompleted => _parent.IsCompleted;

	/// <inheritdoc />
	public string ReservationName => _parent.ReservationName;

	private readonly IUnitOfWork _parent;

	/// <summary>
	/// 初始化 <see cref="ChildUnitOfWork"/> 类的新实例，并订阅父工作单元的事件以进行转发。
	/// </summary>
	/// <param name="parent">作为委托目标的父工作单元。</param>
	/// <exception cref="ArgumentNullException"><paramref name="parent"/> 为 <c>null</c> 时抛出。</exception>
	public ChildUnitOfWork(IUnitOfWork parent)
	{
		Check.EnsureNotNull(parent, nameof(parent));

		_parent = parent;

		_parent.Failed += (sender, args) =>
		{
			Failed?.Invoke(sender, args);
		};
		_parent.Disposed += InvokeDisposedEvent;
		_parent.Completed += (sender, args) =>
		{
			Completed?.Invoke(sender, args);
		};
	}

	/// <inheritdoc />
	/// <remarks>该调用直接委托给父工作单元，实际保存由父工作单元统一执行。</remarks>
	public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		await _parent.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	/// <remarks>该调用直接委托给父工作单元，将回滚父工作单元中的所有上下文。</remarks>
	public async Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		await _parent.RollbackAsync(cancellationToken);
	}

	/// <inheritdoc />
	/// <remarks>
	/// 此为无操作实现：子工作单元不独立提交，完成与提交由根工作单元的 <see cref="IUnitOfWork.CompleteAsync"/> 负责。
	/// </remarks>
	public async Task CompleteAsync(CancellationToken cancellationToken = default)
	{
		await Task.CompletedTask;
	}

	/// <inheritdoc />
	public void OnCompleted(Func<Task> handler)
	{
		_parent.OnCompleted(handler);
	}

	/// <inheritdoc />
	public void SetOuter(IUnitOfWork outer)
	{
		_parent.SetOuter(outer);
	}

	/// <inheritdoc />
	public void Initialize(UnitOfWorkOptions options)
	{
		_parent.Initialize(options);
	}

	/// <inheritdoc />
	public void Reserve(string reservationName)
	{
		_parent.Reserve(reservationName);
	}

	/// <inheritdoc />
	public IUnitOfWorkContext FindContext(string key)
	{
		return _parent.FindContext(key);
	}

	/// <inheritdoc />
	public void AddContext(string key, IUnitOfWorkContext context)
	{
		_parent.AddContext(key, context);
	}

	/// <inheritdoc />
	public IUnitOfWorkContext GetOrAddContext(string key, Func<IUnitOfWorkContext> factory)
	{
		return _parent.GetOrAddContext(key, factory);
	}

	/// <inheritdoc />
	/// <remarks>不执行任何释放操作，上下文的释放由父工作单元（及其所属的依赖注入作用域）负责。</remarks>
	protected override void Dispose(bool disposing)
	{
	}
}