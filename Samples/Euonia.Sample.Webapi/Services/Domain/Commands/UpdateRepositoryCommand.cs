using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>更新代码仓库命令（名称/密级/公开性）。工厂更新边界即 push（<c>repository:push</c>，见 SAMPLE.md 场景三）。</summary>
internal sealed class UpdateRepositoryCommand : Command
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string Level { get; set; }

	public bool? IsPublic { get; set; }
}