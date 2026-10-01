namespace Nerosoft.Euonia.Security;

/// <summary>
/// 数据权限的框架保留命名空间。
/// </summary>
/// <remarks>
/// <para>
/// 授权的键空间由应用自己的<b>权限码</b>构成：用户持有某个码用于类型级闸门，该码下的授予用于按行判定。
/// 框架只保留 <see cref="Prefix"/>（<c>@</c>）这一个前缀，其中只有一个有意义的键——<see cref="Default"/>。
/// </para>
/// <para>
/// <b>操作名不再映射到 <c>@</c> 命名空间</b>：未单独声明策略的标识直接以自身为键
/// （见 <c>ScopeModelRegistration.TryResolve</c>），因此宿主自定义的操作（<c>approve</c>、<c>push</c>）
/// 与权限码一样能承载行级授予——把操作名塞进保留前缀只会让它的授予永远取不到。
/// </para>
/// <para>
/// 这里刻意不用字面量 <c>*</c> 作通配键（理由见 DESIGN §1.6）。键的完整解析规则见 README §5.8。
/// </para>
/// </remarks>
public static class ScopeKeys
{
	/// <summary>
	/// 保留命名空间前缀。
	/// </summary>
	public const string Prefix = "@";

	/// <summary>
	/// 默认键：调用方没有给出标识时生效的键，也是任何标识在自身没有任何授予时的回落目标。
	/// </summary>
	public const string Default = "@default";

	/// <summary>
	/// 校验一个操作名。
	/// </summary>
	/// <param name="operation">操作名。</param>
	/// <returns>校验通过的操作名。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="operation"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	/// <exception cref="InvalidOperationException">当 <paramref name="operation"/> 使用了框架保留前缀 <see cref="Prefix"/> 时抛出。</exception>
	public static string ValidateOperation(string operation)
	{
		Check.EnsureNotNullOrWhiteSpace(operation, nameof(operation));
		Check.Ensure(
			!IsReserved(operation),
			Resources.IDS_SCOPE_OPERATION_RESERVED_PREFIX,
			operation,
			Prefix);

		return operation;
	}

	/// <summary>
	/// 判断权限码是否落在框架保留命名空间内。
	/// </summary>
	/// <param name="code">权限码。</param>
	/// <returns>是保留码则返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
	public static bool IsReserved(string code)
	{
		return code != null && code.StartsWith(Prefix, StringComparison.Ordinal);
	}

	/// <summary>
	/// 校验一个应用声明的权限码。
	/// </summary>
	/// <param name="code">权限码。</param>
	/// <returns>校验通过的权限码。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="code"/> 为 <see langword="null"/>、空或仅由空白字符组成时抛出。</exception>
	/// <exception cref="InvalidOperationException">当权限码落在框架保留命名空间内时抛出。</exception>
	public static string Validate(string code)
	{
		Check.EnsureNotNullOrWhiteSpace(code, nameof(code));
		Check.Ensure(
			!IsReserved(code),
			Resources.IDS_SCOPE_CODE_RESERVED_PREFIX,
			code,
			Prefix);

		return code;
	}

	/// <summary>
	/// 取一个<b>没有单独声明策略</b>的标识所用的授予键。
	/// </summary>
	/// <param name="identifier">授权标识：操作名或权限码。</param>
	/// <returns>授予键。</returns>
	/// <remarks>
	/// <para>
	/// 规则是「以自身为键」，而不是把操作名派生成 <c>@&lt;operation&gt;</c>：后者落在保留命名空间里，
	/// 而授予写入（<c>AddGrant</c>）明确拒绝保留键，于是那条策略的授予永远取不到、只能回落到
	/// <see cref="Default"/>——自定义操作由此变成二等公民。<b>以自身为键没有这个缺口</b>：
	/// 宿主在 <c>approve</c> 这类标识下写的授予会被读到，没写则与从前一样回落到
	/// <see cref="Default"/>（见 <c>ScopeSubjectSet.GrantedFor</c>）。
	/// </para>
	/// <para>
	/// 保留命名空间内（以及空）的标识一律落到 <see cref="Default"/>：调用方可以传任意字符串，
	/// 而 <c>@</c> 下只应有框架自己的键。
	/// </para>
	/// </remarks>
	internal static string KeyFor(string identifier)
	{
		return string.IsNullOrWhiteSpace(identifier) || IsReserved(identifier)
			? Default
			: identifier;
	}
}
