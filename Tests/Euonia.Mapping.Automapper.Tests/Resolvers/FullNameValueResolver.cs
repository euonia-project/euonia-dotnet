using System.Linq.Expressions;
using AutoMapper;

namespace Nerosoft.Euonia.Mapping.Tests;

/// <summary>
/// 通过反射读取源对象的名与姓并拼接为目标全名的值解析器。
/// </summary>
/// <typeparam name="TSource">源对象类型。</typeparam>
/// <typeparam name="TDestination">目标对象类型。</typeparam>
public class FullNameValueResolver<TSource, TDestination> : IValueResolver<TSource, TDestination, string>
{
	private readonly StringHelper _helper;

	/// <summary>
	/// 初始化解析器，并注入用于拼接姓名的字符串辅助工具。
	/// </summary>
	/// <param name="helper">用于拼接姓名的字符串辅助工具。</param>
	public FullNameValueResolver(StringHelper helper)
	{
		_helper = helper;
	}

	/// <summary>
	/// 读取源对象的 <c>FirstName</c> 与 <c>LastName</c> 并拼接为目标成员的值。
	/// </summary>
	/// <param name="source">提供姓名数据的源对象。</param>
	/// <param name="destination">映射目标对象。</param>
	/// <param name="destMember">目标成员名称。</param>
	/// <param name="context">当前解析上下文。</param>
	/// <returns>拼接后的完整姓名。</returns>
	public string Resolve(TSource source, TDestination destination, string destMember, ResolutionContext context)
	{
		var firstName = GetValue<string>(source, "FirstName");
		var lastName = GetValue<string>(source, "LastName");
		return _helper.Combine(firstName, lastName);
	}

	private static TValue GetValue<TValue>(TSource source, string name)
	{
		var property = Expression.PropertyOrField(Expression.Constant(source), name);
		var lambda = Expression.Lambda<Func<TSource, TValue>>(property, Expression.Parameter(typeof(TSource), nameof(source))).Compile();
		return lambda(source);
	}
}