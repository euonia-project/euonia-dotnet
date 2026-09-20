namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 用户当前被授予的授权数据：权限码集合 + 按（权限码，维度）分组的行级授予值。
/// </summary>
/// <remarks>
/// <para>
/// 由 <see cref="IScopeSubjectResolver"/> 从应用数据实时解析，是操作权限与数据权限共同的输入。
/// 层级关系（例如部门树）应在解析期展开为扁平集合，使判定与下推始终只是集合成员判断
/// （数据库侧即 <c>IN (...)</c>）。
/// </para>
/// <para>
/// <b>查找规则</b>：<see cref="ValuesOf"/> / <see cref="Contains"/> 先看该权限码下的授予，
/// 没有再回落到 <see cref="ScopeKeys.Default"/> 上的授予——是<b>覆盖</b>而不是并集。
/// 若做并集，默认授予会把某个具体码下被收窄的行集合重新撑开，行级差异将失效。
/// </para>
/// <para>
/// <b>通配不参与维度查找</b>：权限码的 <c>*</c> 前缀通配只用于「是否持有该权限码」的布尔判定
/// （见 <see cref="HoldsPermission"/>），绝不用于维度取值的回落——否则给整个命名空间授权会
/// 顺带泄漏行级授予。
/// </para>
/// <para>维度名比较<b>大小写不敏感</b>；权限码与维度值比较<b>大小写敏感</b>。</para>
/// </remarks>
public sealed class ScopeSubjectSet
{
	private readonly Dictionary<string, Dictionary<string, HashSet<string>>> _values;
	private readonly HashSet<string> _codes;

	private ScopeSubjectSet(Dictionary<string, Dictionary<string, HashSet<string>>> values, HashSet<string> codes)
	{
		_values = values;
		_codes = codes;
	}

	/// <summary>
	/// 空集合：未授予任何权限码与主体。
	/// </summary>
	public static ScopeSubjectSet Empty { get; } = new(
		new Dictionary<string, Dictionary<string, HashSet<string>>>(StringComparer.Ordinal),
		new HashSet<string>(StringComparer.Ordinal));

	/// <summary>
	/// 创建用于逐步构造授权数据的构建器。
	/// </summary>
	/// <returns>构建器。</returns>
	public static ScopeSubjectSetBuilder CreateBuilder()
	{
		return new ScopeSubjectSetBuilder();
	}

	/// <summary>
	/// 获取用户持有的全部权限码。
	/// </summary>
	public IReadOnlyCollection<string> Codes => _codes;

	/// <summary>
	/// 判断用户是否被授予指定维度上的指定值（按权限码查找，覆盖式回落到默认键）。
	/// </summary>
	/// <param name="scopeKey">权限码。</param>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <param name="value">该维度上的值，大小写敏感。</param>
	/// <returns>被授予则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	public bool Contains(string scopeKey, string dimension, string value)
	{
		return value != null && ValuesOf(scopeKey, dimension).Contains(value);
	}

	/// <summary>
	/// 获取用户在指定权限码下、指定维度上被授予的全部值。
	/// </summary>
	/// <param name="scopeKey">权限码；为 <see langword="null"/> 时取 <see cref="ScopeKeys.Default"/>。</param>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <returns>该维度上的值集合；无授予时返回空集合。</returns>
	/// <remarks>该结果可直接用于数据库侧下推，例如 <c>WHERE dept_id IN (...)</c>。</remarks>
	public IReadOnlyCollection<string> ValuesOf(string scopeKey, string dimension)
	{
		if (dimension == null)
		{
			return Array.Empty<string>();
		}

		// 优先取该码下的授予；没有再回落到默认键（覆盖，不是并集）
		if (scopeKey != null
		    && !string.Equals(scopeKey, ScopeKeys.Default, StringComparison.Ordinal)
		    && _values.TryGetValue(scopeKey, out var scoped)
		    && scoped.TryGetValue(dimension, out var scopedValues)
		    && scopedValues.Count > 0)
		{
			return scopedValues;
		}

		return _values.TryGetValue(ScopeKeys.Default, out var defaults) && defaults.TryGetValue(dimension, out var values)
			? values
			: Array.Empty<string>();
	}

	/// <summary>
	/// 判断用户是否持有指定权限码。
	/// </summary>
	/// <param name="code">权限码。</param>
	/// <returns>持有则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>
	/// 支持以 <c>*</c> 结尾的前缀通配，例如持有 <c>repo:*</c> 可通过 <c>repo:push</c> 的类型级闸门。
	/// 该通配<b>不影响</b>行级授予的查找（见类型备注）。
	/// </remarks>
	public bool HoldsPermission(string code)
	{
		if (string.IsNullOrEmpty(code))
		{
			return true;
		}

		foreach (var granted in _codes)
		{
			if (string.Equals(granted, code, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			if (granted.EndsWith('*')
			    && code.StartsWith(granted[..^1], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// 获取当前集合中带有行级授予的全部权限码（不含只持有权限码而无维度授予的码）。
	/// </summary>
	/// <returns>权限码集合。</returns>
	/// <remarks>命名刻意避开 <see cref="ScopeKeys"/>，以免与框架保留键的常量类型混淆。</remarks>
	public IReadOnlyCollection<string> KeysWithGrants => _values.Keys;

	/// <summary>
	/// 由构建器调用的内部构造入口。
	/// </summary>
	internal static ScopeSubjectSet Create(Dictionary<string, Dictionary<string, HashSet<string>>> values, HashSet<string> codes)
	{
		return values.Count == 0 && codes.Count == 0
			? Empty
			: new ScopeSubjectSet(values, codes);
	}
}
