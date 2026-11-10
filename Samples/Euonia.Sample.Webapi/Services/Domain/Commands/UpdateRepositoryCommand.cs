using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Sample.Domain.Commands;

/// <summary>
/// 更新代码仓库命令（名称/密级/公开性）。工厂更新边界即 push（<c>repository:push</c>，见 SAMPLE.md 场景三）。
/// </summary>
internal sealed class UpdateRepositoryCommand : Command
{
	/// <summary>
	/// 要更新的仓库标识。
	/// </summary>
	public string Id { get; set; } = null!;

	/// <summary>
	/// 新的仓库名称。
	/// </summary>
	public string Name { get; set; } = null!;

	/// <summary>
	/// 新的仓库密级；空值时保留原值。
	/// </summary>
	public string Level { get; set; } = null!;

	/// <summary>
	/// 是否公开；为 <see langword="null"/> 时保留原值。
	/// </summary>
	public bool? IsPublic { get; set; }
}