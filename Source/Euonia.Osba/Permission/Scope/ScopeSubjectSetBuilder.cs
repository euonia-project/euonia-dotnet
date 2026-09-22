namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="ScopeSubjectSet"/> 的构建器，供 <see cref="IScopeSubjectResolver"/> 实现使用。
/// </summary>
/// <remarks>
/// <see cref="Add"/> / <see cref="AddRange"/> / <see cref="AddSelf"/> 落在
/// <see cref="ScopeKeys.Default"/> 上，对<b>所有</b>权限码生效（除非某个码有自己的授予——覆盖式回退）。
/// 需要按操作区分行范围时，用 <see cref="AddGrant(string, string, string)"/> 显式指定权限码。
/// </remarks>
public sealed class ScopeSubjectSetBuilder
{
	private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _values = new(StringComparer.Ordinal);
	private readonly HashSet<string> _codes = new(StringComparer.Ordinal);

	/// <summary>
	/// 授予一个权限码（类型级闸门）。
	/// </summary>
	/// <param name="code">权限码，不得使用框架保留前缀 <c>@</c>。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	public ScopeSubjectSetBuilder AddCode(string code)
	{
		if (!string.IsNullOrWhiteSpace(code))
		{
			_codes.Add(code);
		}

		return this;
	}

	/// <summary>
	/// 授予一组权限码。
	/// </summary>
	/// <param name="codes">权限码序列。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	public ScopeSubjectSetBuilder AddCodes(IEnumerable<string> codes)
	{
		if (codes == null)
		{
			return this;
		}

		foreach (var code in codes)
		{
			AddCode(code);
		}

		return this;
	}

	/// <summary>
	/// 在默认键上授予指定维度的一个值。
	/// </summary>
	/// <param name="dimension">维度名。</param>
	/// <param name="value">该维度上的值。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	public ScopeSubjectSetBuilder Add(string dimension, string value)
	{
		return AddGrant(null, dimension, value);
	}

	/// <summary>
	/// 在默认键上授予指定维度的一组值。
	/// </summary>
	/// <param name="dimension">维度名。</param>
	/// <param name="values">该维度上的值序列，通常为层级展开后的扁平集合。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	public ScopeSubjectSetBuilder AddRange(string dimension, IEnumerable<string> values)
	{
		return AddGrant(null, dimension, values);
	}

	/// <summary>
	/// 授予「本人」，即把指定用户标识加入 <see cref="ScopeDimensions.Owner"/> 维度（默认键）。
	/// </summary>
	/// <param name="userId">当前用户标识。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	/// <remarks>
	/// <see cref="ScopePolicy{T}.Self"/> 等价于 <c>Grant(owner)</c>，因此解析器<b>必须</b>调用本方法
	/// （或自行授予 owner 维度）才能让「本人可访问」成立。这是有意为之：所有者关系因此可撤销——
	/// 不授予即不可访问本人数据。
	/// </remarks>
	public ScopeSubjectSetBuilder AddSelf(string userId)
	{
		return Add(ScopeDimensions.Owner, userId);
	}

	/// <summary>
	/// 在指定权限码下授予某个维度的一个值。
	/// </summary>
	/// <param name="scopeKey">权限码；为 <see langword="null"/> 或空时落在默认键上。</param>
	/// <param name="dimension">维度名。</param>
	/// <param name="value">该维度上的值。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	/// <exception cref="InvalidOperationException">当权限码落在框架保留命名空间内时抛出。</exception>
	/// <remarks>
	/// 该码下的授予会<b>覆盖</b>默认键上的同名维度授予（不是并集），
	/// 这正是「同一用户、同一类型、不同行权限不同」得以表达的原因。
	/// </remarks>
	public ScopeSubjectSetBuilder AddGrant(string scopeKey, string dimension, string value)
	{
		if (string.IsNullOrWhiteSpace(dimension) || value == null)
		{
			return this;
		}

		var key = string.IsNullOrWhiteSpace(scopeKey) ? ScopeKeys.Default : scopeKey;

		Check.Ensure(
			!ScopeKeys.IsReserved(key) || key == ScopeKeys.Default,
			"权限码 '{0}' 使用了框架保留前缀 '{1}'，请改用不含该前缀的码。",
			key,
			ScopeKeys.Prefix);

		if (!_values.TryGetValue(key, out var dimensions))
		{
			dimensions = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
			_values[key] = dimensions;
		}

		if (!dimensions.TryGetValue(dimension, out var values))
		{
			values = new HashSet<string>(StringComparer.Ordinal);
			dimensions[dimension] = values;
		}

		values.Add(value);

		return this;
	}

	/// <summary>
	/// 在指定权限码下授予某个维度的一组值。
	/// </summary>
	/// <param name="scopeKey">权限码；为 <see langword="null"/> 或空时落在默认键上。</param>
	/// <param name="dimension">维度名。</param>
	/// <param name="values">该维度上的值序列。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	public ScopeSubjectSetBuilder AddGrant(string scopeKey, string dimension, IEnumerable<string> values)
	{
		if (values == null)
		{
			return this;
		}

		foreach (var value in values)
		{
			AddGrant(scopeKey, dimension, value);
		}

		return this;
	}

	/// <summary>
	/// 构建授权数据。
	/// </summary>
	/// <returns>构建好的 <see cref="ScopeSubjectSet"/>。</returns>
	public ScopeSubjectSet Build()
	{
		return ScopeSubjectSet.Create(_values, _codes);
	}
}
