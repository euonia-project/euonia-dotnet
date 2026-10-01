using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Domain.Repositories;

/// <summary>
/// 项目数据的持久化仓储（EF Core + SQLite）。仓储只负责存取，权限判定全部由权限引擎完成。
/// </summary>
public interface IProjectStore
{
	/// <summary>返回全部项目的数据源（供读侧下推使用）。</summary>
	IQueryable<Project> Query();

	/// <summary>按标识获取项目；不存在时返回 <see langword="null"/>。</summary>
	Task<Project> GetAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>追加一个新项目。</summary>
	Task AddAsync(Project project, CancellationToken cancellationToken = default);

	/// <summary>替换标识相同的项目数据。</summary>
	Task UpdateAsync(Project project, CancellationToken cancellationToken = default);

	/// <summary>删除标识对应的项目。</summary>
	Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}