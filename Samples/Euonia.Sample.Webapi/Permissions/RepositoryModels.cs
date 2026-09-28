namespace Nerosoft.Euonia.Sample.Permissions;

/// <summary>仓库列表/详情项（读模型）。</summary>
public sealed record RepositoryDto(string Id, string Name, string TeamId, string OwnerId, string Level, bool IsPublic);

/// <summary>创建仓库入参。密级与公开性可省略，省略时取默认（normal / 私有）。</summary>
public sealed record RepositoryCreateInput(string Name, string TeamId, string Level = null, bool? IsPublic = null);

/// <summary>更新仓库入参：仅允许改名称、密级与公开性；归属团队与所有者属组织调整，演示中不允许普通更新路径改。</summary>
public sealed record RepositoryUpdateInput(string Name, string Level = null, bool? IsPublic = null);

/// <summary>分页响应体。</summary>
public sealed record PagedResult<T>(int Total, int Page, int PageSize, IReadOnlyCollection<T> Items)
{
	public static PagedResult<T> Empty(int page, int pageSize)
	{
		return new PagedResult<T>(0, page, pageSize, []);
	}
}