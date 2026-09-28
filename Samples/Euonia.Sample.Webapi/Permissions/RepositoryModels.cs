namespace Nerosoft.Euonia.Sample.Permissions;

/// <summary>仓库列表项。</summary>
public sealed record RepositoryDto(string Id, string Name, string TeamId);

/// <summary>创建仓库入参。</summary>
public sealed record RepositoryCreateInput(string Name, string TeamId);

/// <summary>更新仓库入参：仅允许改名称；仓库归属团队属组织调整，演示中不允许普通更新路径改。</summary>
public sealed record RepositoryUpdateInput(string Name);