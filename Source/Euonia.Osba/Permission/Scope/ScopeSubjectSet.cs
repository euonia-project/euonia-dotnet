namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用户当前被授予的主体集合（按维度分组），是数据权限判定的输入之一。
/// </summary>
/// <remarks>
/// <para>
/// 由 <see cref="IScopeSubjectResolver"/> 从应用数据实时解析。层级关系（例如部门树）应当在解析期
/// 展开为扁平集合，这样判定与下推始终只是集合成员判断（数据库侧即 <c>IN (...)</c>），
/// 不需要框架理解任何层级语义。
/// </para>
/// <para>
/// 维度名比较<b>大小写不敏感</b>；值比较<b>大小写敏感</b>。
/// </para>
/// </remarks>
public sealed class ScopeSubjectSet
{
	private readonly Dictionary<string, HashSet<string>> _values;

	/// <summary>
	/// 使用指定的主体序列初始化 <see cref="ScopeSubjectSet"/> 的新实例。
	/// </summary>
	/// <param name="subjects">用户被授予的全部主体。</param>
	public ScopeSubjectSet(IEnumerable<ScopeSubject> subjects)
	{
		_values = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

		if (subjects == null)
		{
			return;
		}

		foreach (var subject in subjects)
		{
			if (string.IsNullOrWhiteSpace(subject.Dimension) || subject.Value == null)
			{
				continue;
			}

			if (!_values.TryGetValue(subject.Dimension, out var values))
			{
				values = new HashSet<string>(StringComparer.Ordinal);
				_values[subject.Dimension] = values;
			}

			values.Add(subject.Value);
		}
	}

	/// <summary>
	/// 空集合：未授予任何主体。
	/// </summary>
	/// <remarks>空集合只会让 <see cref="ScopePolicy{T}.Grant"/> 不成立，而不会让判定失败。</remarks>
	public static ScopeSubjectSet Empty { get; } = new(null);

	/// <summary>
	/// 创建用于逐步构造主体集合的构建器。
	/// </summary>
	/// <returns>主体集合构建器。</returns>
	public static ScopeSubjectSetBuilder CreateBuilder()
	{
		return new ScopeSubjectSetBuilder();
	}

	/// <summary>
	/// 判断用户在指定维度上是否被授予了指定值。
	/// </summary>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <param name="value">该维度上的值，大小写敏感。</param>
	/// <returns>被授予则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	public bool Contains(string dimension, string value)
	{
		return dimension != null
		       && value != null
		       && _values.TryGetValue(dimension, out var values)
		       && values.Contains(value);
	}

	/// <summary>
	/// 获取用户在指定维度上被授予的全部值。
	/// </summary>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <returns>该维度上的值集合；该维度无授予时返回空集合。</returns>
	/// <remarks>该结果可直接用于数据库侧下推，例如 <c>WHERE dept_id IN (...)</c>。</remarks>
	public IReadOnlyCollection<string> ValuesOf(string dimension)
	{
		if (dimension != null && _values.TryGetValue(dimension, out var values))
		{
			return values;
		}

		return Array.Empty<string>();
	}

	/// <summary>
	/// 获取当前集合中出现的全部维度名。
	/// </summary>
	/// <returns>维度名集合。</returns>
	public IReadOnlyCollection<string> Dimensions => _values.Keys;
}
