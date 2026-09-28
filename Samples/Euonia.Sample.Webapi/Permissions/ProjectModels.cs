namespace Nerosoft.Euonia.Sample.Permissions;

/// <summary>项目列表/详情项（读模型）。</summary>
public sealed record ProjectDto(string Id, string Name, string Description, string OwnerId, bool IsArchived);

/// <summary>创建项目入参。描述可省略。</summary>
public sealed record ProjectCreateInput(string Name, string Description = null);

/// <summary>更新项目入参：仅允许改名称与描述；负责人与归档状态属生命周期管理，更新路径不允许修改。</summary>
public sealed record ProjectUpdateInput(string Name, string Description = null);