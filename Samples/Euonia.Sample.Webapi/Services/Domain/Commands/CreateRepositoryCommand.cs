using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>创建代码仓库命令。走工厂 Create 边界（<c>repository:create</c> + 角色闸门）。</summary>
internal sealed class CreateRepositoryCommand : Command
{
	public string Name { get; set; }

	public string TeamId { get; set; }

	public string Level { get; set; }

	public bool? IsPublic { get; set; }
}