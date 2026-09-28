namespace Nerosoft.Euonia.Sample.Permissions;

/// <summary>团队列表项。</summary>
public sealed record TeamDto(string Id, string Name, string LeaderId);

/// <summary>创建团队入参。</summary>
public sealed record TeamCreateInput(string Name);

/// <summary>更新团队入参：仅允许改名称；负责人变更属组织调整，更新路径不允许修改。</summary>
public sealed record TeamUpdateInput(string Name);

/// <summary>团队成员变更入参（加入/移出）。</summary>
public sealed record TeamMemberChangeInput(string UserId);