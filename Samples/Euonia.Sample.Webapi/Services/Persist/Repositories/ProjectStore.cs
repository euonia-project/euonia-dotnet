using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Sample.Domain.Aggregates;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Persist.Repositories;

/// <summary>
/// <see cref="IProjectStore"/> 的 EF Core + SQLite 实现。
/// 仓储只负责存取，权限判定全部由权限引擎完成；读侧下推直接作用于
/// <see cref="Project"/> 的查询条件，越权行在数据库侧被过滤。
/// </summary>
public sealed class ProjectStore(IApplicationDataContext context) : IProjectStore
{
	public IQueryable<Project> Query()
	{
		return context.Projects;
	}

	/// <inheritdoc />
	public async Task<Project> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		// 只读实体用于填充工厂新建的聚合实例（LoadProperty），不参与 SaveChanges，
		// 必须 AsNoTracking：否则随后的 UpdateAsync(context.Update(...)) 会因同一主键被跟踪两次
		// 而抛出 identity conflict（一个来自 GetAsync 的 FindAsync，一个来自保存的聚合）。
		return await context.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
	}

	/// <inheritdoc />
	public async Task AddAsync(Project project, CancellationToken cancellationToken = default)
	{
		context.Projects.Add(project);
		await context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
	{
		context.Update(project);
		await context.SaveChangesAsync(cancellationToken);
	}

	/// <inheritdoc />
	public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
	{
		var project = await context.Projects.FindAsync([id], cancellationToken);
		if (project != null)
		{
			context.Projects.Remove(project);
			await context.SaveChangesAsync(cancellationToken);
		}
	}
}