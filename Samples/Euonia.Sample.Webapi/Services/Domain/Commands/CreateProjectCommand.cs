using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>创建项目命令。走工厂 Create 边界（<c>project:create</c> + 角色闸门）。</summary>
internal sealed class CreateProjectCommand : Command
{
	public string Name { get; set; }

	public string Description { get; set; }
}