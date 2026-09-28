using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Domain.Repositories;

/// <summary>
/// 项目数据的内存仓储。生产环境应换用持久化仓储；此处用内存数据便于演示，
/// 仓储只负责存取，权限判定全部由权限引擎完成。
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
}