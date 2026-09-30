namespace Nerosoft.Euonia.Security;

/// <summary>
/// 用户当前被授予的授权数据：权限码集合 + 按策略键分组的行级授予。
/// </summary>
/// <remarks>
/// <para>
/// 由 <see cref="IScopeSubjectResolver"/> 从应用数据实时解析，是操作权限与数据权限共同的输入；
/// 层级关系（例如部门树）应在解析期展开为扁平集合，使判定与下推始终只是集合成员判断。
/// </para>
/// <para>
/// 行级授予的形状是「策略键 → <see cref="ScopeSubject"/>」，而 <see cref="ScopeSubject"/> 是
/// 「维度 → 值集合」——每一层都有一个名字，代码里看不到三层泛型嵌套。
/// </para>
/// <para>
/// <b>查找规则</b>：<see cref="ValuesOf"/> / <see cref="Contains"/> 先看该权限码下的授予，没有再回落到
/// <see cref="ScopeKeys.Default"/> 上的授予——是<b>覆盖</b>而不是并集（见 DESIGN §1.6）。
/// </para>
/// <para>
/// <b>通配不参与维度查找</b>：权限码的 <c>*</c> 前缀通配只用于「是否持有该权限码」的布尔判定
/// （见 <see cref="HoldsPermission"/>），绝不用于维度取值的回落。
/// </para>
/// <para>
/// 维度名比较<b>大小写不敏感</b>，维度值比较<b>大小写敏感</b>；
/// 权限码（策略键）在<b>所有</b>入口一律大小写不敏感——按码取授予与 <see cref="HoldsPermission"/>
/// 的持有判定必须同口径，否则「闸门放行、授予落空回落到更宽的默认键」会开出一条越权通道。
/// </para>
/// </remarks>
public sealed class ScopeSubjectSet
{
	// 权限码/策略键一律 OrdinalIgnoreCase：HoldsPermission 与按码取授予必须给同一个答案，
	// 否则差值就是越权面。维度值仍大小写敏感（由 ScopeSubject 内部的字典保证）。
	private readonly Dictionary<string, ScopeSubject> _subjects;
	private readonly HashSet<string> _codes;

	/// <summary>只读视图：绝不把底层 HashSet 直接暴露出去。</summary>
	private readonly IReadOnlyCollection<string> _codesView;

	/// <summary>只读视图：同上。</summary>
	private readonly IReadOnlyCollection<string> _keysView;

	private ScopeSubjectSet(Dictionary<string, ScopeSubject> subjects, HashSet<string> codes)
	{
		_subjects = subjects;
		_codes = codes;

		// 必须包一层：Codes 原本直接返回底层 HashSet<string>，任何拿到本对象的人都能强转回去
		// 调 Add —— 而 Empty 是全体未认证用户共享的静态实例，向它注入一个 'admin:*'
		// 就等于给匿名用户发了权限码。只读包装在这里构造一次，取用时零分配。
		_codesView = Array.AsReadOnly([.. codes]);
		_keysView = Array.AsReadOnly([.. subjects.Keys]);
	}

	/// <summary>
	/// 空集合：未授予任何权限码与主体。
	/// </summary>
	public static ScopeSubjectSet Empty { get; } = new(
		new Dictionary<string, ScopeSubject>(StringComparer.OrdinalIgnoreCase),
		new HashSet<string>(StringComparer.OrdinalIgnoreCase));

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
	/// <remarks>返回的是只读快照，无法通过强转回写底层集合。</remarks>
	public IReadOnlyCollection<string> Codes => _codesView;


	/// <summary>
	/// 判断用户是否被授予指定维度上的指定值（按权限码查找，覆盖式回落到默认键）。
	/// </summary>
	/// <param name="scopeKey">权限码。</param>
	/// <param name="dimension">维度名，大小写不敏感。</param>
	/// <param name="value">该维度上的值，大小写敏感。</param>
	/// <returns>被授予则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	public bool Contains(string scopeKey, string dimension, string value)
	{
		return value != null && GrantedFor(scopeKey).Contains(dimension, value);
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

		return GrantedFor(scopeKey).ValuesOf(dimension);
	}

	/// <summary>
	/// 判断用户是否持有指定权限码。
	/// </summary>
	/// <param name="code">权限码。</param>
	/// <returns>持有则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	/// <remarks>
	/// 支持以 <c>*</c> 结尾的前缀通配，例如持有 <c>repo:*</c> 可通过 <c>repo:push</c> 的类型级闸门。
	/// 该通配<b>不影响</b>行级授予的查找（见类型备注）。
	/// 比较口径与 <see cref="GrantedFor"/> 一致（大小写不敏感），两个入口对同一个码给出同一个答案。
	/// </remarks>
	public bool HoldsPermission(string code)
	{
		if (string.IsNullOrEmpty(code))
		{
			return true;
		}

		// _codes 的比较器就是 OrdinalIgnoreCase，这里不再自己比一遍
		if (_codes.Contains(code))
		{
			return true;
		}

		foreach (var granted in _codes)
		{
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
	public IReadOnlyCollection<string> KeysWithGrants => _keysView;

	/// <summary>
	/// 按策略键取授予主体集合：该码下没有授予时回落到默认键（覆盖，不是并集）。
	/// </summary>
	private ScopeSubject GrantedFor(string scopeKey)
	{
		if (scopeKey != null
		    && !string.Equals(scopeKey, ScopeKeys.Default, StringComparison.OrdinalIgnoreCase)
		    && _subjects.TryGetValue(scopeKey, out var scoped)
		    && !scoped.IsEmpty)
		{
			return scoped;
		}

		return _subjects.TryGetValue(ScopeKeys.Default, out var defaults) ? defaults : ScopeSubject.Empty;
	}

	/// <summary>
	/// 由构建器调用的内部构造入口。
	/// </summary>
	internal static ScopeSubjectSet Create(Dictionary<string, ScopeSubject> subjects, HashSet<string> codes)
	{
		return subjects.Count == 0 && codes.Count == 0
			? Empty
			: new ScopeSubjectSet(subjects, codes);
	}
}
