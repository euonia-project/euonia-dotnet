using Nerosoft.Euonia.Application;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Facade.Contracts;

/// <summary>
/// 仓库应用服务：写侧经命令总线/对象工厂执行（操作权限与行级数据权限在工厂边界强制），
/// 读侧经查询请求做行级视图下推。
/// </summary>
public interface IRepositoryApplicationService : IApplicationService
{
	/// <summary>分页查询仓库（已登录按 <c>repository:view</c> 码与行范围下推；匿名只返回公开行）。</summary>
	Task<PagedResult<RepositoryDto>> FindAsync(string keyword, string sortBy, int page, int pageSize, CancellationToken cancellationToken = default);

	/// <summary>查看仓库详情（行级视图判定；匿名仅公开行）。</summary>
	Task<RepositoryDto> GetAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>创建仓库（<c>repository:create</c>），返回新仓库标识。</summary>
	Task<string> CreateAsync(RepositoryCreateInput input, CancellationToken cancellationToken = default);

	/// <summary>更新仓库（push 边界，<c>repository:push</c>）。</summary>
	Task UpdateAsync(string id, RepositoryUpdateInput input, CancellationToken cancellationToken = default);

	/// <summary>删除仓库（<c>repository:delete</c>，行级单行授予判定）。</summary>
	Task DeleteAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>对仓库执行 push（命令对象，行级 <c>repository:push</c> 判定）。</summary>
	Task PushAsync(string id, CancellationToken cancellationToken = default);

	/// <summary>解释某个仓库在当前用户数据范围内的判定结论。</summary>
	Task<object> ExplainAsync(string id, CancellationToken cancellationToken = default);
}