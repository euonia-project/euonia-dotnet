using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 工作单元上下文的默认实现，将 <see cref="IRepositoryContext"/> 适配为 <see cref="IUnitOfWorkContext"/>。
/// </summary>
internal class UnitOfWorkContext : IUnitOfWorkContext
{
	/// <summary>
	/// 初始化 <see cref="UnitOfWorkContext"/> 类的新实例。
	/// </summary>
	/// <param name="context">要包装的仓储上下文。</param>
	public UnitOfWorkContext(IRepositoryContext context)
	{
		Context = context;
	}

	/// <summary>
	/// 获取被包装的仓储上下文。
	/// </summary>
	/// <value>当前工作单元所使用的仓储上下文。</value>
	public IRepositoryContext Context { get; }

	//public DbTransaction Transaction { get; }

	/// <inheritdoc />
	public void Dispose()
	{
		Context.Dispose();
	}

	/// <inheritdoc />
	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return Context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public Task CommitAsync(CancellationToken cancellationToken = default)
	{
		return Context.CommitAsync(cancellationToken);
	}

	/// <inheritdoc />
	public Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		return Context.RollbackAsync(cancellationToken);
	}
}