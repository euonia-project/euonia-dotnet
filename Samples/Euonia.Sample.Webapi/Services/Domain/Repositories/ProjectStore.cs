using System.Collections.Concurrent;
using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Domain.Repositories;

/// <summary>
/// <see cref="IProjectStore"/> 的内存实现，进程内共享一份种子数据。
/// </summary>
public sealed class ProjectStore : IProjectStore
{
	private readonly ConcurrentDictionary<string, Project> _items = new();

	public ProjectStore()
	{
		Seed("p-100", "库存盘点", "u-1", "d-1");
		Seed("p-101", "客户结算", "u-2", "d-1");
		Seed("p-102", "渠道对账", "u-1", "d-2");
		Seed("p-103", "费用复核", "u-3", "d-3");
	}

	private void Seed(string id, string name, string ownerId, string deptId)
	{
		_items[id] = new Project { Id = id, Name = name, OwnerId = ownerId, DeptId = deptId };
	}

	/// <inheritdoc />
	public IQueryable<Project> Query()
	{
		return _items.Values.AsQueryable();
	}

	/// <inheritdoc />
	public Task<Project> GetAsync(string id, CancellationToken cancellationToken = default)
	{
		_items.TryGetValue(id, out var project);
		return Task.FromResult(project);
	}

	/// <inheritdoc />
	public Task AddAsync(Project project, CancellationToken cancellationToken = default)
	{
		_items[project.Id] = project;
		return Task.CompletedTask;
	}

	/// <inheritdoc />
	public Task UpdateAsync(Project project, CancellationToken cancellationToken = default)
	{
		_items[project.Id] = project;
		return Task.CompletedTask;
	}
}