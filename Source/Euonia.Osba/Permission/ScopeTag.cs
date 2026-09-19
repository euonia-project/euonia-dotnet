namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 表示一个数据范围标签：由维度名与不透明值组成的值对。
/// </summary>
/// <remarks>
/// 维度由使用方自行定义（例如 region、team、family、org 等）。值为任意不透明字符串，
/// 在真实场景中通常为数据库标识，框架不做任何值格式假设；值 "*" 表示通配。
/// </remarks>
/// <param name="Dimension">维度名，例如 <c>region</c> 或 <c>team</c>。</param>
/// <param name="Value">该维度的不透明值，例如数据库标识；值为 "*" 时表示该维度通配。</param>
public record ScopeTag(string Dimension, string Value)
{
	/// <summary>
	/// 表示全局通配的标签（维度与值均为 "*"），可跳过所有维度检查。
	/// </summary>
	public static ScopeTag Any { get; } = new("*", "*");
}