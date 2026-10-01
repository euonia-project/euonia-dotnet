using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Facade.Contracts;

/// <summary>
/// 团队应用服务：写侧经命令总线执行（操作权限与行级数据权限在工厂边界强制），
/// 读侧经查询请求做行级视图下推（本团队成员/负责人）。
/// </summary>
public interface ITeamApplicationService : IApplicationService
{
	/// <summary>分页查询团队（必须先持有 <c>team:view</c> 码，否则返回空页）。</summary>
	Task<PagedResult<TeamDto>> FindAsync(string keyword, string sortBy, int page, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>查看团队详情（行级视图判定）。</summary>
	Task<TeamDto> GetAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>创建团队（<c>team:create</c>，负责人取当前用户），返回新团队标识。</summary>
	Task<string> CreateAsync(TeamCreateInput input, CancellationToken cancellationToken = default);

	/// <summary>更新团队（<c>team:edit</c>，仅负责人可改）。</summary>
	Task UpdateAsync(string id, TeamUpdateInput input, CancellationToken cancellationToken = default);

	/// <summary>删除团队（<c>team:delete</c>，行级负责人判定）。</summary>
	Task DeleteAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>
	/// 把账号加入团队（<c>team:edit</c>，行级仅本团队负责人；
	/// 成员关系的变更即授权变更，见 Euonia.Security/README.md §5.9）。
	/// </summary>
	Task AddMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default);

	/// <summary>把账号移出团队（<c>team:edit</c>，行级仅本团队负责人）。</summary>
	Task RemoveMemberAsync(string teamId, string userId, CancellationToken cancellationToken = default);
}