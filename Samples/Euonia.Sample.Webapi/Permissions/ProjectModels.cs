namespace Nerosoft.Euonia.Sample.Permissions;

/// <summary>项目读模型（只向调用方暴露数据字段）。</summary>
public record ProjectDto(string Id, string Name, string OwnerId, string DeptId);

/// <summary>创建项目入参。</summary>
public record ProjectCreateInput(string Name, string DeptId);

/// <summary>更新项目入参。</summary>
public record ProjectUpdateInput(string Name, string DeptId);