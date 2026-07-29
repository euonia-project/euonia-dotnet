using Nerosoft.Euonia.Uow;

namespace Nerosoft.Euonia.Repository.EfCore;

internal class UnitOfWorkContext : IUnitOfWorkContext
{
	public UnitOfWorkContext(IRepositoryContext context)
	{
		Context = context;
	}

	public IRepositoryContext Context { get; }

	//public DbTransaction Transaction { get; }

	public void Dispose()
	{
		Context.Dispose();
	}

	public Task SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return Context.SaveChangesAsync(cancellationToken);
	}

	public Task CommitAsync(CancellationToken cancellationToken = default)
	{
		return Context.CommitAsync(cancellationToken);
	}

	public Task RollbackAsync(CancellationToken cancellationToken = default)
	{
		return Context.RollbackAsync(cancellationToken);
	}
}