using System.Linq.Expressions;
using AutoMapper;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// Provides extension methods for AutoMapper <see cref="IMappingExpression"/> instances to
/// simplify ignoring destination members during mapping configuration.
/// </summary>
/// <remarks>
/// These extension methods offer a convenient, fluent way to call <c>ForMember(..., opt => opt.Ignore())</c>
/// for one or more destination members by name or by expression.
/// </remarks>
public static class AutomapperExtensions
{
	/// <summary>
	/// Ignores one or more destination properties by name when configuring a mapping.
	/// </summary>
	/// <param name="expression">The mapping expression to configure.</param>
	/// <param name="propertyNames">One or more destination property names to ignore.</param>
	/// <returns>The same <see cref="IMappingExpression"/> instance to allow fluent chaining.</returns>
	/// <remarks>
	/// Each <paramref name="propertyNames"/> entry is passed to <c>ForMember(propertyName, opt => opt.Ignore())</c>.
	/// Use this overload when you want to specify properties to ignore by their string names.
	/// </remarks>
	/// <example>
	/// Example usage:
	/// <para>
	/// <![CDATA[
	///	CreateMap<Source, Dest>().Ignore(nameof(Dest.ReadOnlyProperty), "AnotherProperty");
	/// ]]>
	/// </para>
	/// </example>
	public static IMappingExpression Ignore(this IMappingExpression expression, params string[] propertyNames)
	{
		foreach (var propertyName in propertyNames)
		{
			expression.ForMember(propertyName, opt => opt.Ignore());
		}

		return expression;
	}

	/// <summary>
	/// Ignores one or more destination properties specified by lambda expressions when configuring a mapping.
	/// </summary>
	/// <typeparam name="TSource">The source type of the mapping.</typeparam>
	/// <typeparam name="TDestination">The destination type of the mapping.</typeparam>
	/// <param name="expression">The mapping expression to configure.</param>
	/// <param name="propertyExpressions">One or more expressions that select destination properties to ignore (e.g. <c>d =&gt; d.Property</c>).</param>
	/// <returns>The same <see cref="IMappingExpression{TSource,TDestination}"/> instance to allow fluent chaining.</returns>
	/// <remarks>
	/// This overload is strongly typed and preferred when you can express the destination members as lambda expressions.
	/// Each <paramref name="propertyExpressions"/> entry is passed to <c>ForMember(propertyExpression, opt => opt.Ignore())</c>.
	/// </remarks>
	/// <example>
	/// // Example usage:
	/// // CreateMap&lt;Source, Dest&gt;().Ignore&lt;Source, Dest&gt;(d => d.ReadOnlyProperty, d => d.AnotherProperty);
	/// </example>
	public static IMappingExpression<TSource, TDestination> Ignore<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expression, params Expression<Func<TDestination, object>>[] propertyExpressions)
	{
		foreach (var propertyExpression in propertyExpressions)
		{
			expression.ForMember(propertyExpression, opt => opt.Ignore());
		}

		return expression;
	}
}