namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 只读对象。
/// </summary>
/// <typeparam name="T">只读对象的具体类型。</typeparam>
public class ReadOnlyObject<T> : BusinessObject<T>, IReadOnlyObject, IOperableProperty
	where T : ReadOnlyObject<T>
{
	/// <summary>
	/// 只读对象拒绝任何属性写入。
	/// </summary>
	/// <remarks>
	/// <para>
	/// 「只读」由本覆盖表达：所有写入路径经 <c>SetProperty</c> 入口的
	/// <c>CanWriteProperty(property, true)</c> 统一裁决，拒绝时抛
	/// <see cref="System.Security.SecurityException"/>；
	/// 显式的 <c>BypassRuleChecks</c> 仍然可以写（那是调用方主动声明的绕过，
	/// 语义由 <see cref="ObjectBase"/> 统一定义）；读取权限由 <c>CanReadProperty</c> 正常判定。
	/// </para>
	/// </remarks>
	public override bool CanWriteProperty(IPropertyInfo property)
	{
		return false;
	}

	/// <summary>
	/// 只读对象只在<b>装载作用域</b>内允许 <c>LoadProperty</c>（工厂方法填充数据时），
	/// 外部调用与显式 <c>BypassRuleChecks</c> 之外的写入仍然拒绝。
	/// </summary>
	/// <param name="property">属性。</param>
	/// <returns>允许装载则返回 <see langword="true"/>。</returns>
	protected internal override bool CanLoadProperty(IPropertyInfo property)
	{
		return IsLoading || IsBypassingRuleChecks;
	}
}
