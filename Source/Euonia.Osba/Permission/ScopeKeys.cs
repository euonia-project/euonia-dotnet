namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 数据权限策略的键：框架保留命名空间。
/// </summary>
/// <remarks>
/// <para>
/// 策略按<b>权限码</b>声明。「用户持有的权限码」用于类型级的操作闸门，
/// 「该码下的行级授予」用于按行的判定，两者共用同一个键空间。
/// </para>
/// <para>
/// 以 <see cref="Prefix"/>（<c>@</c>）开头的键是<b>框架保留</b>的，应用声明的权限码不得使用该前缀。
/// 这里刻意不使用字面量 <c>*</c> 作为通配键：<c>*</c> 在本框架中已表示「权限码的前缀通配」，
/// 再叠加一层「策略键通配」会让排障变成猜谜。
/// </para>
/// </remarks>
public static class ScopeKeys
{
	/// <summary>
	/// 保留命名空间前缀。
	/// </summary>
	public const string Prefix = "@";

	/// <summary>
	/// 默认键：模型未针对某个权限码单独声明策略时生效的键。
	/// </summary>
	public const string Default = "@default";

	/// <summary>
	/// <see cref="BusinessOperation.Read"/> 的默认键。
	/// </summary>
	public const string Read = "@read";

	/// <summary>
	/// <see cref="BusinessOperation.Create"/> 的默认键。
	/// </summary>
	public const string Create = "@create";

	/// <summary>
	/// <see cref="BusinessOperation.Update"/> 的默认键。
	/// </summary>
	public const string Update = "@update";

	/// <summary>
	/// <see cref="BusinessOperation.Delete"/> 的默认键。
	/// </summary>
	public const string Delete = "@delete";

	/// <summary>
	/// <see cref="BusinessOperation.Execute"/> 的默认键。
	/// </summary>
	public const string Execute = "@execute";

	/// <summary>
	/// 获取指定操作对应的默认键。
	/// </summary>
	/// <param name="operation">业务操作。</param>
	/// <returns>该操作的默认键。</returns>
	public static string For(BusinessOperation operation)
	{
		return operation switch
		{
			BusinessOperation.Read => Read,
			BusinessOperation.Create => Create,
			BusinessOperation.Update => Update,
			BusinessOperation.Delete => Delete,
			BusinessOperation.Execute => Execute,
			_ => Default
		};
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
	/// <exception cref="ArgumentNullException">当 <paramref name="code"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ArgumentException">当权限码为空白时抛出。</exception>
	/// <exception cref="InvalidOperationException">当权限码落在保留命名空间内时抛出。</exception>
	public static string Validate(string code)
	{
		Check.EnsureNotNullOrWhiteSpace(code, nameof(code));
		Check.Ensure(
			!IsReserved(code),
			"权限码 '{0}' 使用了框架保留前缀 '{1}'。请改用不含该前缀的码。",
			code,
			Prefix);

		return code;
	}
}
