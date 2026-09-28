using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>删除代码仓库命令。走工厂 Delete 边界（<c>repository:delete</c>，行级单行授予判定）。</summary>
internal sealed class DeleteRepositoryCommand : Command
{
	public string Id { get; set; }
}