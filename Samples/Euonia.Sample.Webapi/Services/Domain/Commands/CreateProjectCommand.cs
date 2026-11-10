using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 创建项目命令。走工厂 Create 边界（<c>project:create</c> + 角色闸门）。
/// </summary>
internal sealed class CreateProjectCommand : Command
{
	/// <summary>
	/// 项目名称。
	/// </summary>
	public string Name { get; set; } = null!;

	/// <summary>
	/// 项目描述。
	/// </summary>
	public string Description { get; set; } = null!;
}