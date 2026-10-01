using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>更新项目命令（仅名称/描述）。工厂更新边界即 <c>project:edit</c>。</summary>
internal sealed class UpdateProjectCommand : Command
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }
}