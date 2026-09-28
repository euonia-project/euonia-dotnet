using Nerosoft.Euonia.Sample.Domain.Aggregates;

namespace Nerosoft.Euonia.Sample.Domain.Repositories;

/// <summary>
/// 团队数据的持久化仓储（EF Core + SQLite）。仓储只负责存取，权限判定全部由权限引擎完成。
/// </summary>
public interface ITeamStore
{
	/// <summary>返回全部团队的数据源（供读侧下推使用）。</summary>
	IQueryable<Team> Query();

	/// <summary>按标识获取团队；不存在时返回 <see langword="null"/>。</summary>
	Task<Team> GetAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>追加一个新团队。</summary>
	Task AddAsync(Team team, CancellationToken cancellationToken = default);

	/// <summary>替换标识相同的团队数据。</summary>
	Task UpdateAsync(Team team, CancellationToken cancellationToken = default);

	/// <summary>删除标识对应的团队。</summary>
	Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}