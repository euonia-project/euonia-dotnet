using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 更新项目命令（仅名称/描述）。工厂更新边界即 <c>project:edit</c>。
/// </summary>
internal sealed class UpdateProjectCommand : Command
{
	/// <summary>
	/// 要更新的项目标识。
	/// </summary>
	public string Id { get; set; } = null!;

	/// <summary>
	/// 新的项目名称。
	/// </summary>
	public string Name { get; set; } = null!;

	/// <summary>
	/// 新的项目描述；非空时才会覆盖原值。
	/// </summary>
	public string Description { get; set; } = null!;
}