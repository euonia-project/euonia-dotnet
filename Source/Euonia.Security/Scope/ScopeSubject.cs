namespace Nerosoft.Euonia.Security;

/// <summary>
/// 一个策略键下的全部行级授予主体：<b>维度 → 值集合</b>。
/// </summary>
/// <remarks>
/// <para>
/// 它是授权数据的中间一层：<see cref="ScopeSubjectSet"/> 的形状是「策略键 → <see cref="ScopeSubject"/>」，
/// 而不是「策略键 → 维度 → 值集合」。后者的三层泛型嵌套要在读代码时反复对照括号与键，
/// 把「一个键下的主体集合」收成一个具名类型后，每一层都只剩一个词。
/// </para>
/// <para>
/// 维度名比较<b>大小写不敏感</b>（由本类型的字典比较器保证），维度值比较<b>大小写敏感</b>。
/// </para>
/// </remarks>
internal sealed class ScopeSubject
{
	/// <summary>没有任何授予的空主体集合。</summary>
	internal static ScopeSubject Empty { get; } = new();

	private readonly Dictionary<string, HashSet<string>> _values = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 记录一个授予；同一（维度，值）重复加入只保留一份。
	/// </summary>
	/// <param name="dimension">维度名。</param>
	/// <param name="value">该维度上的值。</param>
	internal void Add(string dimension, string value)
	{
		if (!_values.TryGetValue(dimension, out var values))
		{
			values = new HashSet<string>(StringComparer.Ordinal);
			_values[dimension] = values;
		}

		values.Add(value);
	}

	/// <summary>
	/// 获取本键下指定维度上被授予的全部值；没有授予时返回空集合。
	/// </summary>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <returns>该维度上的值集合。</returns>
	/// <remarks>
	/// 返回<b>副本</b>：原本直接返回底层 <see cref="HashSet{T}"/>，调用方强转即可向本授权数据回写一个值。
	/// 这里每次构造一份，调用方拿到的集合不可回写；下游（<c>ScopeCompileContext</c>）本来就会再拷一次。
	/// </remarks>
	internal IReadOnlyCollection<string> ValuesOf(string dimension)
	{
		return _values.TryGetValue(dimension, out var values) && values.Count > 0
			? new List<string>(values)
			: Array.Empty<string>();
	}

	/// <summary>
	/// 判断本键下是否授予了指定维度上的指定值。
	/// </summary>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <param name="value">该维度上的值，大小写敏感。</param>
	/// <returns>被授予则返回 <see langword="true"/>。</returns>
	internal bool Contains(string dimension, string value)
	{
		return _values.TryGetValue(dimension, out var values) && values.Contains(value);
	}

	/// <summary>
	/// 判断本键下是否记录了任何授予。
	/// </summary>
	internal bool IsEmpty => _values.Count == 0;

	/// <summary>
	/// 深拷贝出一份互不共享内部集合的副本。
	/// </summary>
	/// <returns>副本。</returns>
	/// <remarks>
	/// 由 <see cref="ScopeSubjectSetBuilder.Build"/> 调用：不拷贝的话，Build 之后继续调用
	/// Add/AddGrant 会改动<b>已经发布出去</b>的授权数据快照——那正是「构建期与使用期共享可变状态」。
	/// </remarks>
	internal ScopeSubject Clone()
	{
		var copy = new ScopeSubject();

		foreach (var (dimension, values) in _values)
		{
			copy._values[dimension] = new HashSet<string>(values, StringComparer.Ordinal);
		}

		return copy;
	}
}
