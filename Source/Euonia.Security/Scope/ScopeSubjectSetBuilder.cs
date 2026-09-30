namespace Nerosoft.Euonia.Security;

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
	private readonly Dictionary<string, ScopeSubject> _subjects = new(StringComparer.OrdinalIgnoreCase);
	private readonly HashSet<string> _codes = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 授予一个权限码（类型级闸门）。
	/// </summary>
	/// <param name="code">权限码，不得使用框架保留前缀 <c>@</c>；为 <see langword="null"/> 或空白时忽略。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	/// <exception cref="InvalidOperationException">当权限码落在框架保留命名空间内时抛出。</exception>
	public ScopeSubjectSetBuilder AddCode(string code)
	{
		if (string.IsNullOrWhiteSpace(code))
		{
			return this;
		}

		Check.Ensure(
			!ScopeKeys.IsReserved(code),
			Resources.IDS_SCOPE_CODE_RESERVED_PREFIX,
			code,
			ScopeKeys.Prefix);

		_codes.Add(code);

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
	/// <see cref="ScopePolicy{T}.Self"/> 等价于 <c>Grant(owner)</c>，解析器<b>必须</b>调用本方法
	/// （或自行授予 owner 维度）才能让「本人可访问」成立——这是有意的：所有者关系因此可撤销。
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
			Resources.IDS_SCOPE_CODE_RESERVED_PREFIX,
			key,
			ScopeKeys.Prefix);

		if (!_subjects.TryGetValue(key, out var subject))
		{
			subject = new ScopeSubject();
			_subjects[key] = subject;
		}

		subject.Add(dimension, value);

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
	/// <remarks>
	/// <b>快照</b>：内部集合会被深拷贝，因此 Build 之后继续调用 <see cref="AddCode"/> / <see cref="AddGrant(string, string, string)"/>
	/// 不会改动已经发布出去的那份授权数据。发布与构建必须是两份状态——共享同一份可变集合
	/// 意味着「解析器先 Build、后补一条授予」会悄悄改掉上层已判定过的结论。
	/// </remarks>
	public ScopeSubjectSet Build()
	{
		if (_subjects.Count == 0 && _codes.Count == 0)
		{
			return ScopeSubjectSet.Empty;
		}

		var subjects = new Dictionary<string, ScopeSubject>(_subjects.Count, StringComparer.OrdinalIgnoreCase);

		foreach (var (key, subject) in _subjects)
		{
			subjects[key] = subject.Clone();
		}

		return ScopeSubjectSet.Create(subjects, new HashSet<string>(_codes, StringComparer.OrdinalIgnoreCase));
	}
}
