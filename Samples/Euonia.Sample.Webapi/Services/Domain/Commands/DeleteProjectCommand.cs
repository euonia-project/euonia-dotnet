using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>删除项目命令。走工厂 Delete 边界（<c>project:delete</c>，行级判定 + 归档不可删）。</summary>
internal sealed class DeleteProjectCommand : Command
{
	public string Id { get; set; }
}