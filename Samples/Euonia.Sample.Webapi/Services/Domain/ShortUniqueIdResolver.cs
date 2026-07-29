using System.Linq.Expressions;
using AutoMapper;

namespace Nerosoft.Euonia.Sample.Domain;

internal class ShortUniqueIdResolver<TSource, TDestination> : IValueResolver<TSource, TDestination, string>
{
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

