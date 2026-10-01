using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>创建团队命令。走工厂 Create 边界（<c>team:create</c> + 角色闸门），负责人取当前用户。</summary>
internal sealed class CreateTeamCommand : Command
{
	public string Name { get; set; }
}