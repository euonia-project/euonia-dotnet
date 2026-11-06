using System.Linq.Expressions;
using AutoMapper;

namespace Nerosoft.Euonia.Sample.Domain;

/// <summary>
/// AutoMapper 值解析器：读取源对象的 <c>Id</c> 并编码为短唯一 ID 字符串。
/// </summary>
/// <typeparam name="TSource">源对象类型，必须包含类型为 <see cref="long"/> 的 <c>Id</c> 成员。</typeparam>
/// <typeparam name="TDestination">目标对象类型。</typeparam>
internal class ShortUniqueIdResolver<TSource, TDestination> : IValueResolver<TSource, TDestination, string>
{
	/// <inheritdoc/>
	public string Resolve(TSource source, TDestination destination, string destMember, ResolutionContext context)
	{
		var id = GetId(source);
		return ShortUniqueId.Default.EncodeInt64(id);
	}

	private static long GetId(TSource source)
	{
		var property = Expression.PropertyOrField(Expression.Constant(source), "Id");
		var lambda = Expression.Lambda<Func<TSource, long>>(property, Expression.Parameter(typeof(TSource), "source")).Compile();
		return lambda(source);
	}
}

