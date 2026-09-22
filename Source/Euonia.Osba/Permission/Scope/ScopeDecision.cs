namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限的判定结果与命中路径，用于审计与排障。
/// </summary>
/// <param name="Allowed">是否可访问。</param>
/// <param name="ScopeKey">本次判定使用的策略键（权限码）。</param>
/// <param name="MatchedAllows">成立的条件中，作为允许条件的那些。</param>
/// <param name="MatchedDenies">成立的条件中，作为拒绝条件的那些。</param>
public sealed record ScopeDecision(bool Allowed, string ScopeKey, IReadOnlyList<string> MatchedAllows, IReadOnlyList<string> MatchedDenies)
{
	/// <summary>
	/// 生成可读的判定说明。
	/// </summary>
	/// <returns>判定说明。</returns>
	/// <remarks>带上策略键，便于审计时定位「是哪条行级策略给出的结论」。</remarks>
	public override string ToString()
	{
		var verdict = Allowed ? "允许" : "拒绝";
		var allows = MatchedAllows.Count == 0 ? "（无）" : string.Join(", ", MatchedAllows);
		var denies = MatchedDenies.Count == 0 ? "（无）" : string.Join(", ", MatchedDenies);

		return $"判定：{verdict}；策略键：{ScopeKey}；成立的允许条件：{allows}；成立的拒绝条件：{denies}";
	}
}
