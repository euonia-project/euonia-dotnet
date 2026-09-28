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

	/// <summary>
	/// 按标识获取团队，并一并取回成员（子表行）。
	/// </summary>
	/// <param name="id">团队标识。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>团队；不存在时返回 <see langword="null"/>。</returns>
	/// <remarks>
	/// 视图策略用到成员维度（见 <see cref="Permissions.TeamScopeModel"/>），而单行判定要求对象图完整，
	/// 因此详情读取必须走本方法；只做写侧判定（行内列）的调用方用 <see cref="GetAsync"/> 即可。
	/// </remarks>
	Task<Team> GetWithMembersAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>取某用户当前<b>有效</b>的团队标识（供解析器反向展开为扁平集合）。</summary>
	Task<IReadOnlyCollection<string>> GetTeamIdsAsync(string userId, CancellationToken cancellationToken = default);

	/// <summary>取全部有效成员关系（用户标识 → 团队标识）；供授权视图一次取全，避免逐账号查询。</summary>
	Task<IReadOnlyDictionary<string, IReadOnlyCollection<string>>> GetMembershipsAsync(CancellationToken cancellationToken = default);

	/// <summary>把用户加入团队；已失效的关系恢复为有效。</summary>
	Task AddMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default);

	/// <summary>把用户移出团队（关系置为失效并保留行）。</summary>
	Task RemoveMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default);

	/// <summary>追加一个新团队。</summary>
	Task AddAsync(Team team, CancellationToken cancellationToken = default);

	/// <summary>替换标识相同的团队数据。</summary>
	Task UpdateAsync(Team team, CancellationToken cancellationToken = default);

	/// <summary>删除标识对应的团队。</summary>
	Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}