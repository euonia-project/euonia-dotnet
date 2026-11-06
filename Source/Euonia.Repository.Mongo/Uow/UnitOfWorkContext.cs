using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 基于 Mongo 仓储上下文实现的工作单元上下文，将保存、提交与回滚操作委托给底层仓储上下文。
/// </summary>
internal class UnitOfWorkContext : IUnitOfWorkContext
{
	/// <summary>
	/// 使用指定的仓储上下文初始化工作单元上下文。
	/// </summary>
	/// <param name="context">承载实际数据操作的仓储上下文。</param>
	public UnitOfWorkContext(IRepositoryContext context)
	{
		Context = context;
	}

	/// <summary>
	/// 获取底层仓储上下文。
	/// </summary>
	public IRepositoryContext Context { get; }

	//public DbTransaction Transaction { get; }

	/// <inheritdoc/>
	public void Dispose()
	{
		Context.Dispose();
	}

	/// <inheritdoc/>
	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return Context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc/>
	public Task CommitAsync(CancellationToken cancellationToken = default)
	{
		return Context.CommitAsync(cancellationToken);
	}

	/// <inheritdoc/>
	public Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		return Context.RollbackAsync(cancellationToken);
	}
}