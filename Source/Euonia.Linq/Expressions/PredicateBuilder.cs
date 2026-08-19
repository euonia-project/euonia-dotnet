using System.Linq.Expressions;
using System.Reflection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// Enables the efficient, dynamic composition of query predicates.
/// </summary>
/// <remarks>
/// See http://petemontgomery.wordpress.com/2011/02/10/a-universal-predicatebuilder/
/// </remarks>
public static class PredicateBuilder
{
	/// <summary>
	/// Creates a predicate that evaluates to true.
	/// </summary>
	public static Expression<Func<T, bool>> True<T>()
	{
		return param => true;
	}

	/// <summary>
	/// Creates a predicate that evaluates to false.
	/// </summary>
	public static Expression<Func<T, bool>> False<T>()
	{
		return param => false;
	}

	/// <summary>
	/// Creates a predicate expression from the specified lambda expression.
	/// </summary>
	public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> predicate)
	{
		return predicate;
	}

	/// <summary>
	/// Gets the compare condition.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TValue">The type of the t value.</typeparam>
	/// <param name="source">The source.</param>
	/// <param name="propertyName">Name of the property.</param>
	/// <param name="value">The value.</param>
	/// <param name="operator">Type of the compare.</param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	public static Expression<Func<T, bool>> GetCompareCondition<T, TValue>(T source, string propertyName, TValue value, QueryOperator @operator)
	{
		var param = Expression.Parameter(typeof(T), "p");
		var exp = Expression.Constant(value);
		var structure = propertyName.Split('.').ToList();
		MemberExpression member = SearchMember(param, structure);
		Expression condition = @operator switch
		{
			QueryOperator.Equal => Expression.Equal(member, exp),
			QueryOperator.NotEqual => Expression.NotEqual(member, exp),
			QueryOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, exp),
			QueryOperator.LessThanOrEqual => Expression.LessThanOrEqual(member, exp),
			QueryOperator.GreaterThan => Expression.GreaterThan(member, exp),
			QueryOperator.LessThan => Expression.LessThan(member, exp),
			_ => throw new InvalidOperationException(),
		};
		var lambda = Expression.Lambda<Func<T, bool>>(condition, param);
		return lambda;
	}

	/// <summary>
	/// Gets the contains condition.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <typeparam name="TValue">The type of the t value.</typeparam>
	/// <param name="source">The source.</param>
	/// <param name="propertyName">Name of the property.</param>
	/// <param name="value">The value.</param>
	/// <returns></returns>
	public static Expression<Func<T, bool>> GetContainsCondition<T, TValue>(T source, string propertyName, List<TValue> value)
	{
		var param = Expression.Parameter(typeof(T), "p");
		var methodInfo = typeof(List<TValue>).GetRuntimeMethod("Contains", new[] { typeof(TValue) });
		if (methodInfo == null)
		{
			throw new MissingMethodException("The method of 'Contains' not found.");
		}

		var list = Expression.Constant(value, typeof(List<TValue>));
		var structure = propertyName.Split('.').ToList();
		var member = SearchMember(param, structure);
		var condition = Expression.Call(list, methodInfo, member);
		var lambda = Expression.Lambda<Func<T, bool>>(condition, param);
		return lambda;
	}

	/// <summary>
	/// Searches the member.
	/// </summary>
	/// <param name="expression">The expression.</param>
	/// <param name="propertiesName">Name of the properties.</param>
	/// <returns>MemberExpression.</returns>
	private static MemberExpression SearchMember(Expression expression, IList<string> propertiesName)
	{
		while (true)
		{
			if (propertiesName.Count != 0)
			{
				expression = Expression.Property(expression, propertiesName.First());
				propertiesName.RemoveAt(0);
			}
			else
			{
				return (MemberExpression)expression;
			}
		}
	}

	/// <summary>
	/// Get property value from source.
	/// </summary>
	/// <param name="source"></param>
	/// <param name="propertyName"></param>
	/// <typeparam name="TObject"></typeparam>
	/// <typeparam name="TProperty"></typeparam>
	/// <returns></returns>
	public static TProperty GetProperty<TObject, TProperty>(TObject source, string propertyName)
	{
		var property = Expression.PropertyOrField(Expression.Constant(source), propertyName);
		var lambda = Expression.Lambda<Func<TObject, TProperty>>(property, Expression.Parameter(typeof(TObject), nameof(source))).Compile();
		return lambda(source);
	}

	/// <summary>
	/// Build property equal expression.
	/// </summary>
	/// <param name="propertyName">The name of the property to compare.</param>
	/// <param name="value">The value to compare with property.</param>
	/// <typeparam name="TObject">The type of the object with property to be compared.</typeparam>
	/// <typeparam name="TValue">The type of the give value.</typeparam>
	/// <returns>source =&gt; (source.Id == value)</returns>
	public static Expression<Func<TObject, bool>> PropertyEqual<TObject, TValue>(string propertyName, TValue value)
	{
		// var parameter = Expression.Parameter(typeof(TEntity), "source");
		// var member = Expression.PropertyOrField(parameter, "Id");
		// var expression = Expression.Call(typeof(object), nameof(Equals), new[] { member.Type }, member, Expression.Constant(id));
		// return Expression.Lambda<Func<TEntity, bool>>(expression, parameter);

		var parameter = Expression.Parameter(typeof(TObject), "source");
		var member = Expression.PropertyOrField(parameter, propertyName);
		var expression = Expression.Equal(member, Expression.Constant(value, member.Type));
		var predicate = Expression.Lambda<Func<TObject, bool>>(expression, parameter);
		return predicate;
	}

	/// <summary>
	/// Build property not equal expression.
	/// </summary>
	/// <param name="propertyName">The name of the property to compare.</param>
	/// <param name="value">The value to compare with property.</param>
	/// <typeparam name="TObject">The type of the object with property to be compared.</typeparam>
	/// <typeparam name="TValue">The type of the give value.</typeparam>
	/// <returns>source =&gt; (source.Id != value)</returns>
	public static Expression<Func<TObject, bool>> PropertyNotEqual<TObject, TValue>(string propertyName, TValue value)
	{
		var parameter = Expression.Parameter(typeof(TObject), "source");
		var member = Expression.PropertyOrField(parameter, propertyName);
		var expression = Expression.NotEqual(member, Expression.Constant(value, member.Type));
		var predicate = Expression.Lambda<Func<TObject, bool>>(expression, parameter);
		return predicate;
	}

	/// <summary>
	/// Build property greater than expression.
	/// </summary>
	/// <param name="propertyName">The name of the property to compare.</param>
	/// <param name="value">The value to compare with property.</param>
	/// <typeparam name="TObject">The type of the object with property to be compared.</typeparam>
	/// <typeparam name="TValue">The type of the give value.</typeparam>
	/// <returns>source =&gt; (source.Id &gt; value)</returns>
	public static Expression<Func<TObject, bool>> PropertyGreaterThan<TObject, TValue>(string propertyName, TValue value)
	{
		var parameter = Expression.Parameter(typeof(TObject), "source");
		var member = Expression.PropertyOrField(parameter, propertyName);
		var expression = Expression.GreaterThan(member, Expression.Constant(value, member.Type));
		var predicate = Expression.Lambda<Func<TObject, bool>>(expression, parameter);
		return predicate;
	}

	/// <summary>
	/// Build property greater than or equal expression.
	/// </summary>
	/// <param name="propertyName">The name of the property to compare.</param>
	/// <param name="value">The value to compare with property.</param>
	/// <typeparam name="TObject">The type of the object with property to be compared.</typeparam>
	/// <typeparam name="TValue">The type of the give value.</typeparam>
	/// <returns>source =&gt; (source.Id &gt;= value)</returns>
	public static Expression<Func<TObject, bool>> GreaterThanOrEqual<TObject, TValue>(string propertyName, TValue value)
	{
		var parameter = Expression.Parameter(typeof(TObject), "source");
		var member = Expression.PropertyOrField(parameter, propertyName);
		var expression = Expression.GreaterThanOrEqual(member, Expression.Constant(value, member.Type));
		var predicate = Expression.Lambda<Func<TObject, bool>>(expression, parameter);
		return predicate;
	}

	/// <summary>
	/// Build property less than expression.
	/// </summary>
	/// <param name="propertyName">The name of the property to compare.</param>
	/// <param name="value">The value to compare with property.</param>
	/// <typeparam name="TObject">The type of the object with property to be compared.</typeparam>
	/// <typeparam name="TValue">The type of the give value.</typeparam>
	/// <returns>source =&gt; (source.Id &lt; value)</returns>
	public static Expression<Func<TObject, bool>> PropertyLessThan<TObject, TValue>(string propertyName, TValue value)
	{
		var parameter = Expression.Parameter(typeof(TObject), "source");
		var member = Expression.PropertyOrField(parameter, propertyName);
		var expression = Expression.LessThan(member, Expression.Constant(value, member.Type));
		var predicate = Expression.Lambda<Func<TObject, bool>>(expression, parameter);
		return predicate;
	}

	/// <summary>
	/// Build property less than or equal expression.
	/// </summary>
	/// <param name="propertyName">The name of the property to compare.</param>
	/// <param name="value">The value to compare with property.</param>
	/// <typeparam name="TObject">The type of the object with property to be compared.</typeparam>
	/// <typeparam name="TValue">The type of the give value.</typeparam>
	/// <returns>source =&gt; (source.Id &lt;= value)</returns>
	public static Expression<Func<TObject, bool>> PropertyLessThanOrEqual<TObject, TValue>(string propertyName, TValue value)
	{
		var parameter = Expression.Parameter(typeof(TObject), "source");
		var member = Expression.PropertyOrField(parameter, propertyName);
		var expression = Expression.LessThanOrEqual(member, Expression.Constant(value, member.Type));
		var predicate = Expression.Lambda<Func<TObject, bool>>(expression, parameter);
		return predicate;
	}

	/// <summary>
	/// Build property in range expression.
	/// </summary>
	/// <param name="propertyName"></param>
	/// <param name="value"></param>
	/// <typeparam name="TObject"></typeparam>
	/// <typeparam name="TValue"></typeparam>
	/// <returns></returns>
	/// <exception cref="MissingMethodException"></exception>
	public static Expression<Func<TObject, bool>> PropertyInRange<TObject, TValue>(string propertyName, params TValue[] value)
	{
		var method = Reflect.FindMethod(nameof(Enumerable.Contains), typeof(Enumerable), typeof(IEnumerable<TValue>), typeof(TValue))
		                    .MakeGenericMethod(typeof(TValue));

		if (method == null)
		{
			throw new MissingMethodException("The method of 'Contains' not found.");
		}
		
		var parameter = Expression.Parameter(typeof(TObject), "source");
		var member = Expression.PropertyOrField(parameter, propertyName);
		var expression = Expression.Call(method, Expression.Constant(value, typeof(IEnumerable<TValue>)), member);
		var predicate = Expression.Lambda<Func<TObject, bool>>(expression, parameter);
		return predicate;
	}
}