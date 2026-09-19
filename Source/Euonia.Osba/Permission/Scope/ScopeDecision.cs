namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限的判定结果与命中路径，用于审计与排障。
/// </summary>
/// <param name="Allowed">是否可访问。</param>
/// <param name="MatchedAllows">成立的条件中，作为允许条件的那些。</param>
/// <param name="MatchedDenies">成立的条件中，作为拒绝条件的那些。</param>
public sealed record ScopeDecision(bool Allowed, IReadOnlyList<string> MatchedAllows, IReadOnlyList<string> MatchedDenies)
{
	/// <summary>
	/// 生成可读的判定说明。
	/// </summary>
	/// <returns>判定说明。</returns>
	public override string ToString()
	{
		var verdict = Allowed ? "允许" : "拒绝";
		var allows = MatchedAllows.Count == 0 ? "（无）" : string.Join(", ", MatchedAllows);
		var denies = MatchedDenies.Count == 0 ? "（无）" : string.Join(", ", MatchedDenies);

		return $"判定：{verdict}；成立的允许条件：{allows}；成立的拒绝条件：{denies}";
	}
}
