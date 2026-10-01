using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Sample.Permissions;

namespace Nerosoft.Euonia.Sample.Persist.Requests;

/// <summary>项目分页列表查询：读侧在 <see cref="Handlers.ProjectRequestHandler"/> 中做行级视图下推。</summary>
public record ProjectListQueryRequest(string Keyword, string SortBy, int Page, int PageSize) : IRequest<PagedResult<ProjectDto>>;