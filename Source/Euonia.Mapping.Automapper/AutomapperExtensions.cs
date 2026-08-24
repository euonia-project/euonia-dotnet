using System.Linq.Expressions;
using AutoMapper;

namespace Nerosoft.Euonia.Mapping;

/// <summary>
/// 为 AutoMapper 的 <see cref="IMappingExpression"/> 实例提供扩展方法，
/// 以简化映射配置中忽略目标成员的操作。
/// </summary>
/// <remarks>
/// 这些扩展方法提供了一种便捷的流式（fluent）方式，
/// 可通过成员名称或表达式为一个或多个目标成员调用 <c>ForMember(..., opt => opt.Ignore())</c>。
/// </remarks>
public static class AutomapperExtensions
{
	/// <summary>
	/// 配置映射时按名称忽略一个或多个目标属性。
	/// </summary>
	/// <param name="expression">要配置的映射表达式。</param>
	/// <param name="propertyNames">要忽略的一个或多个目标属性名称。</param>
	/// <returns>相同的 <see cref="IMappingExpression"/> 实例，以支持流式链式调用。</returns>
	/// <remarks>
	/// <paramref name="propertyNames"/> 中的每一项都会传递给 <c>ForMember(propertyName, opt => opt.Ignore())</c>。
	/// 当需要以字符串名称指定要忽略的属性时，请使用此重载。
	/// </remarks>
	/// <example>
	/// 用法示例：
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
	/// 配置映射时，通过 lambda 表达式忽略一个或多个目标属性。
	/// </summary>
	/// <typeparam name="TSource">映射的源类型。</typeparam>
	/// <typeparam name="TDestination">映射的目标类型。</typeparam>
	/// <param name="expression">要配置的映射表达式。</param>
	/// <param name="propertyExpressions">用于选择要忽略的目标属性的一个或多个表达式（例如 <c>d =&gt; d.Property</c>）。</param>
	/// <returns>相同的 <see cref="IMappingExpression{TSource,TDestination}"/> 实例，以支持流式链式调用。</returns>
	/// <remarks>
	/// 此重载是强类型的，在能够以 lambda 表达式表示目标成员时优先使用。
	/// <paramref name="propertyExpressions"/> 中的每一项都会传递给 <c>ForMember(propertyExpression, opt => opt.Ignore())</c>。
	/// </remarks>
	/// <example>
	/// // 用法示例：
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