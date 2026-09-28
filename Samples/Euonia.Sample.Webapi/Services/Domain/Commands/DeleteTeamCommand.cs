using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>删除团队命令。走工厂 Delete 边界（<c>team:delete</c>，行级负责人判定）。</summary>
internal sealed class DeleteTeamCommand : Command
{
	public string Id { get; set; }
}