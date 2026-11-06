using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>创建代码仓库命令。走工厂 Create 边界（<c>repository:create</c> + 角色闸门）。</summary>
internal sealed class CreateRepositoryCommand : Command
{
	/// <summary>仓库名称。</summary>
	public string Name { get; set; } = null!;

	/// <summary>仓库所属团队的标识。</summary>
	public string TeamId { get; set; } = null!;

	/// <summary>仓库密级；空值时保留聚合默认值。</summary>
	public string Level { get; set; } = null!;

	/// <summary>是否公开；为 <see langword="null"/> 时保留聚合默认值。</summary>
	public bool? IsPublic { get; set; }
}