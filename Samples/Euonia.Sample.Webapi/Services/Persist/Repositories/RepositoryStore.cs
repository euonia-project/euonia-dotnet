using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Persist.Repositories;

/// <summary>
/// <see cref="IRepositoryStore"/> 的 EF Core + SQLite 实现。
/// 仓储只负责存取，权限判定全部由权限引擎完成；读侧下推直接作用于
/// <see cref="CodeRepository"/> 的查询条件，越权行在数据库侧被过滤。
/// </summary>
public sealed class RepositoryStore(IApplicationDataContext context) : IRepositoryStore
{
	public IQueryable<CodeRepository> Query()
	{
		return context.CodeRepositories;
	}

	/// <inheritdoc />
	public async Task<CodeRepository> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		// 只读实体用于填充工厂新建的聚合实例（LoadProperty），不参与 SaveChanges，
		// 必须 AsNoTracking：否则随后的 UpdateAsync(context.Update(...)) 会因同一主键被跟踪两次
		// 而抛出 identity conflict（一个来自 GetAsync 的 FindAsync，一个来自保存的聚合）。
		return await context.CodeRepositories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
	}

	/// <inheritdoc />
	public async Task AddAsync(CodeRepository repository, CancellationToken cancellationToken = default)
	{
		context.CodeRepositories.Add(repository);
		await context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task UpdateAsync(CodeRepository repository, CancellationToken cancellationToken = default)
	{
		context.Update(repository);
		await context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var repository = await context.CodeRepositories.FindAsync([id], cancellationToken);
		if (repository != null)
		{
			context.CodeRepositories.Remove(repository);
			await context.SaveChangesAsync(cancellationToken);
		}
	}
}