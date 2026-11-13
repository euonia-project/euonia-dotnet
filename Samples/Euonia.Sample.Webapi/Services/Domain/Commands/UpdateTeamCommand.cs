using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 更新团队命令（仅改名称）。走工厂 Update 边界（<c>team:edit</c>）。
/// </summary>
internal sealed class UpdateTeamCommand : Command
{
	/// <summary>
	/// 要更新的团队标识。
	/// </summary>
	public string Id { get; set; } = null!;

	/// <summary>
	/// 新的团队名称。
	/// </summary>
	public string Name { get; set; } = null!;
}