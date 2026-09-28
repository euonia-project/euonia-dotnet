using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Persist.Requests;

/// <summary>仓库分页列表查询：读侧在 <see cref="Handlers.RepositoryRequestHandler"/> 中做行级视图下推。</summary>
public record RepositoryListQueryRequest(string Keyword, string SortBy, int Page, int PageSize) : IRequest<PagedResult<RepositoryDto>>;