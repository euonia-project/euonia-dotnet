using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Facade.Contracts;

/// <summary>
/// 项目应用服务：写侧经命令总线/对象工厂执行（操作权限与行级数据权限在工厂边界强制），
/// 读侧经查询请求做行级视图下推。
/// </summary>
public interface IProjectApplicationService : IApplicationService
{
	/// <summary>分页查询项目（已登录按 <c>project:view</c> 码与行范围下推；项目不对外公开）。</summary>
	Task<PagedResult<ProjectDto>> FindAsync(string keyword, string sortBy, int page, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>查看项目详情（行级视图判定；项目不对外公开，匿名 403）。</summary>
	Task<ProjectDto> GetAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>创建项目（<c>project:create</c>），返回新项目标识。</summary>
	Task<string> CreateAsync(ProjectCreateInput input, CancellationToken cancellationToken = default);

	/// <summary>更新项目（<c>project:edit</c>；归档项目不可改）。</summary>
	Task UpdateAsync(string id, ProjectUpdateInput input, CancellationToken cancellationToken = default);

	/// <summary>归档项目（<c>project:archive</c>；归档即只读凝固）。</summary>
	Task ArchiveAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>删除项目（<c>project:delete</c>，行级判定；归档项目不可删）。</summary>
	Task DeleteAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>解释某个项目在当前用户数据范围内的判定结论。</summary>
	Task<object> ExplainAsync(string id, CancellationToken cancellationToken = default);
}