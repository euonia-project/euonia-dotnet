using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>更新团队命令（仅改名称）。走工厂 Update 边界（<c>team:edit</c>）。</summary>
internal sealed class UpdateTeamCommand : Command
{
	public string Id { get; set; }

	public string Name { get; set; }
}