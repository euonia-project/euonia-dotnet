namespace Nerosoft.Euonia.Osba;

/// <summary>
/// <see cref="ScopeSubjectSet"/> 的构建器，供 <see cref="IScopeSubjectResolver"/> 实现使用。
/// </summary>
public sealed class ScopeSubjectSetBuilder
{
	private readonly List<ScopeSubject> _subjects = [];

	/// <summary>
	/// 授予指定维度上的一个值。
	/// </summary>
	/// <param name="dimension">维度名。</param>
	/// <param name="value">该维度上的值。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	public ScopeSubjectSetBuilder Add(string dimension, string value)
	{
		if (!string.IsNullOrWhiteSpace(dimension) && value != null)
		{
			_subjects.Add(new ScopeSubject(dimension, value));
		}

		return this;
	}

	/// <summary>
	/// 授予指定维度上的一组值。
	/// </summary>
	/// <param name="dimension">维度名。</param>
	/// <param name="values">该维度上的值序列，通常为层级展开后的扁平集合。</param>
	/// <returns>当前构建器，便于链式调用。</returns>
	public ScopeSubjectSetBuilder AddRange(string dimension, IEnumerable<string> values)
	{
		if (values == null)
		{
			return this;
		}

		foreach (var value in values)
		{
			Add(dimension, value);
		}

		return this;
	}

	/// <summary>
	/// 授予「本人」，即把指定用户标识加入 <see cref="ScopeDimensions.Owner"/> 维度。
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
	/// 构建主体集合。
	/// </summary>
	/// <returns>构建好的 <see cref="ScopeSubjectSet"/>。</returns>
	public ScopeSubjectSet Build()
	{
		return new ScopeSubjectSet(_subjects);
	}
}
