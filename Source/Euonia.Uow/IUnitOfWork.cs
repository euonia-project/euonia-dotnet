namespace Nerosoft.Euonia.Uow;

/// <summary>
/// 表示一个工作单元作用域。管理逻辑操作的生存期、上下文、服务解析
/// 以及提交/回滚生命周期。
/// </summary>
public interface IUnitOfWork : IDisposable
{
	/// <summary>
	/// 当工作单元被释放时发生。
	/// </summary>
	event EventHandler<DisposedEventArgs> Disposed;

	/// <summary>
	/// 当工作单元成功完成时发生。
	/// </summary>
	event EventHandler<UnitOfWorkEventArgs>? Completed;

	/// <summary>
	/// 当工作单元失败时发生。
	/// </summary>
	event EventHandler<UnitOfWorkFailedEventArgs>? Failed;

	/// <summary>
	/// 获取此工作单元实例的唯一标识符。
	/// </summary>
	Guid Id { get; }

	/// <summary>
	/// 获取用于存储此工作单元任意上下文数据的可变字典。
	/// </summary>
	Dictionary<string, object> Items { get; }

	/// <summary>
	/// 获取此工作单元使用的配置选项。
	/// </summary>
	IUnitOfWorkOptions Options { get; }

	/// <summary>
	/// 获取已注册 <see cref="IUnitOfWorkContext"/> 实例的只读集合，
	/// 以字符串标识符为键。
	/// </summary>
	IReadOnlyDictionary<string, IUnitOfWorkContext> Contexts { get; }

	/// <summary>
	/// 获取作用域限定于此工作单元的 <see cref="IServiceProvider"/>。
	/// 可使用它按工作单元的生存期解析服务。
	/// </summary>
	IServiceProvider ServiceProvider { get; }

	/// <summary>
	/// 获取此工作单元嵌套时的外层（父级）工作单元；否则为 null。
	/// </summary>
	IUnitOfWork? Outer { get; }

	/// <summary>
	/// 获取一个值，指示此工作单元是否已被保留。
	/// </summary>
	bool IsReserved { get; }

	/// <summary>
	/// 获取此工作单元被保留时的保留名称；否则为 null。
	/// </summary>
	string? ReservationName { get; }

	/// <summary>
	/// 获取一个值，指示此工作单元是否已被释放。
	/// </summary>
	bool IsDisposed { get; }

	/// <summary>
	/// 获取一个值，指示此工作单元是否已完成。
	/// </summary>
	bool IsCompleted { get; }

	/// <summary>
	/// 持久化此工作单元内所有已注册上下文的挂起更改。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的标记。</param>
	/// <returns>表示异步保存操作的任务。</returns>
	Task SaveChangesAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 回滚此工作单元中的任何挂起操作。
	/// 实现应确保上下文恢复到一致状态。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的标记。</param>
	/// <returns>表示异步回滚操作的任务。</returns>
	Task RollbackAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 完成工作单元，按需提交更改并触发完成事件。
	/// </summary>
	/// <param name="cancellationToken">用于监视取消请求的标记。</param>
	/// <returns>表示异步完成操作的任务。</returns>
	Task CompleteAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// 注册在工作单元完成时执行的异步处理程序。
	/// </summary>
	/// <param name="handler">完成时要执行的处理程序。</param>
	void OnCompleted(Func<Task> handler);

	/// <summary>
	/// 设置此实例的外层（父级）工作单元。用于嵌套工作单元场景。
	/// </summary>
	/// <param name="outer">父级 <see cref="IUnitOfWork"/>。</param>
	void SetOuter(IUnitOfWork? outer);

	/// <summary>
	/// 使用提供的选项初始化此工作单元。
	/// </summary>
	/// <param name="options">要应用于此工作单元的选项。</param>
	void Initialize(UnitOfWorkOptions options);

	/// <summary>
	/// 使用指定名称保留此工作单元，将其标记为独占使用。
	/// </summary>
	/// <param name="reservationName">保留标识符。</param>
	void Reserve(string reservationName);

	/// <summary>
	/// 从工作单元的作用域中解析类型为 <typeparamref name="TService"/> 的服务。
	/// </summary>
	/// <typeparam name="TService">要解析的服务类型。</typeparam>
	/// <returns><typeparamref name="TService"/> 的实例；若未注册则为 null。</returns>
	TService? GetService<TService>() where TService : class;

	/// <summary>
	/// 从工作单元的作用域中解析所有类型为 <typeparamref name="TService"/> 的服务。
	/// </summary>
	/// <typeparam name="TService">要解析的服务类型。</typeparam>
	/// <returns>已解析服务的序列；若未注册任何服务则为空。</returns>
	IEnumerable<TService> GetServices<TService>() where TService : class;

	/// <summary>
	/// 从工作单元的作用域中解析指定的 <paramref name="serviceType"/> 服务。
	/// </summary>
	/// <param name="serviceType">要解析的服务类型。</param>
	/// <returns>该服务的实例；若未注册则为 null。</returns>
	object? GetService(Type serviceType);

	/// <summary>
	/// 从工作单元的作用域中解析所有指定的 <paramref name="serviceType"/> 服务。
	/// </summary>
	/// <param name="serviceType">要解析的服务类型。</param>
	/// <returns>已解析服务的序列；若未注册任何服务则为空。</returns>
	IEnumerable<object?> GetServices(Type serviceType);

	/// <summary>
	/// 按其键查找已注册的 <see cref="IUnitOfWorkContext"/>。
	/// </summary>
	/// <param name="key">要查找的上下文键。</param>
	/// <returns>若找到则为 <see cref="IUnitOfWorkContext"/>；否则为 null。</returns>
	IUnitOfWorkContext? FindContext(string key);

	/// <summary>
	/// 使用提供的键将指定的 <see cref="IUnitOfWorkContext"/> 添加到此工作单元。
	/// </summary>
	/// <param name="key">用于注册上下文的键。</param>
	/// <param name="context">要添加的上下文实例。</param>
	void AddContext(string key, IUnitOfWorkContext context);

	/// <summary>
	/// 按键获取现有的 <see cref="IUnitOfWorkContext"/>，或添加由提供的工厂创建的新实例。
	/// </summary>
	/// <param name="key">要获取或添加的上下文键。</param>
	/// <param name="factory">上下文不存在时用于创建上下文的工厂。</param>
	/// <returns>现有的或新创建的 <see cref="IUnitOfWorkContext"/> 实例。</returns>
	IUnitOfWorkContext GetOrAddContext(string key, Func<IUnitOfWorkContext> factory);
}