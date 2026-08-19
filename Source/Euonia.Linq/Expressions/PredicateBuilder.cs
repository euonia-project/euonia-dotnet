using System.Linq.Expressions;
using System.Reflection;
using Nerosoft.Euonia.Reflection;

namespace Nerosoft.Euonia.Linq;

/// <summary>
/// 支持高效、动态地组合查询谓词。
/// </summary>
/// <remarks>
/// 参见 http://petemontgomery.wordpress.com/2011/02/10/a-universal-predicatebuilder/
/// </remarks>
public static class PredicateBuilder
{
	/// <summary>
	/// 创建一个计算结果为 true 的谓词。
	/// </summary>
	public static Expression<Func<T, bool>> True<T>()
	{
		return param => true;
	}

	/// <summary>
	/// 创建一个计算结果为 false 的谓词。
	/// </summary>
	public static Expression<Func<T, bool>> False<T>()
	{
		return param => false;
	}

	/// <summary>
	/// 根据指定的 Lambda 表达式创建谓词表达式。
	/// </summary>
	/// <typeparam name="T">谓词参数的类型。</typeparam>
	/// <param name="predicate">要包装的 Lambda 表达式。</param>
	/// <returns>返回传入的谓词表达式本身。</returns>
	public static Expression<Func<T, bool>> Create<T>(Expression<Func<T, bool>> predicate)
	{
		return predicate;
	}

	/// <summary>
	/// 获取比较条件表达式。
	/// </summary>
	/// <typeparam name="T">源对象类型。</typeparam>
	/// <typeparam name="TValue">值类型。</typeparam>
	/// <param name="source">源对象。</param>
	/// <param name="propertyName">属性名，支持使用点号表示嵌套属性。</param>
	/// <param name="value">用于比较的值。</param>
	/// <param name="operator">比较运算符。</param>
	/// <returns>表示比较条件的谓词表达式。</returns>
	/// <exception cref="InvalidOperationException"><paramref name="operator"/> 不是支持的关系运算符时抛出。</exception>
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
	/// 获取包含（Contains）条件表达式。
	/// </summary>
	/// <typeparam name="T">源对象类型。</typeparam>
	/// <typeparam name="TValue">值类型。</typeparam>
	/// <param name="source">源对象。</param>
	/// <param name="propertyName">属性名，支持使用点号表示嵌套属性。</param>
	/// <param name="value">要判断是否包含的值列表。</param>
	/// <returns>表示包含条件的谓词表达式。</returns>
	/// <exception cref="MissingMethodException">未找到 <see cref="List{T}"/> 的 Contains 方法时抛出。</exception>
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
	/// 按属性名列表依次查找成员表达式。
	/// </summary>
	/// <param name="expression">起始表达式。</param>
	/// <param name="propertiesName">属性名列表。</param>
	/// <returns>最终得到的成员访问表达式。</returns>
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
	/// 从源对象获取指定属性的值。
	/// </summary>
	/// <typeparam name="TObject">源对象类型。</typeparam>
	/// <typeparam name="TProperty">属性类型。</typeparam>
	/// <param name="source">源对象。</param>
	/// <param name="propertyName">属性名。</param>
	/// <returns>属性值。</returns>
	public static TProperty GetProperty<TObject, TProperty>(TObject source, string propertyName)
	{
		var property = Expression.PropertyOrField(Expression.Constant(source), propertyName);
		var lambda = Expression.Lambda<Func<TObject, TProperty>>(property, Expression.Parameter(typeof(TObject), nameof(source))).Compile();
		return lambda(source);
	}

	/// <summary>
	/// 构建属性等于指定值的表达式。
	/// </summary>
	/// <param name="propertyName">要比较的属性名。</param>
	/// <param name="value">与属性比较的值。</param>
	/// <typeparam name="TObject">包含待比较属性的对象类型。</typeparam>
	/// <typeparam name="TValue">给定值的类型。</typeparam>
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
	/// 构建属性不等于指定值的表达式。
	/// </summary>
	/// <param name="propertyName">要比较的属性名。</param>
	/// <param name="value">与属性比较的值。</param>
	/// <typeparam name="TObject">包含待比较属性的对象类型。</typeparam>
	/// <typeparam name="TValue">给定值的类型。</typeparam>
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
	/// 构建属性大于指定值的表达式。
	/// </summary>
	/// <param name="propertyName">要比较的属性名。</param>
	/// <param name="value">与属性比较的值。</param>
	/// <typeparam name="TObject">包含待比较属性的对象类型。</typeparam>
	/// <typeparam name="TValue">给定值的类型。</typeparam>
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
	/// 构建属性大于或等于指定值的表达式。
	/// </summary>
	/// <param name="propertyName">要比较的属性名。</param>
	/// <param name="value">与属性比较的值。</param>
	/// <typeparam name="TObject">包含待比较属性的对象类型。</typeparam>
	/// <typeparam name="TValue">给定值的类型。</typeparam>
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
	/// 构建属性小于指定值的表达式。
	/// </summary>
	/// <param name="propertyName">要比较的属性名。</param>
	/// <param name="value">与属性比较的值。</param>
	/// <typeparam name="TObject">包含待比较属性的对象类型。</typeparam>
	/// <typeparam name="TValue">给定值的类型。</typeparam>
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
	/// 构建属性小于或等于指定值的表达式。
	/// </summary>
	/// <param name="propertyName">要比较的属性名。</param>
	/// <param name="value">与属性比较的值。</param>
	/// <typeparam name="TObject">包含待比较属性的对象类型。</typeparam>
	/// <typeparam name="TValue">给定值的类型。</typeparam>
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
	/// 构建属性值在给定范围内的表达式。
	/// </summary>
	/// <typeparam name="TObject">包含待比较属性的对象类型。</typeparam>
	/// <typeparam name="TValue">给定值的类型。</typeparam>
	/// <param name="propertyName">要比较的属性名。</param>
	/// <param name="value">值的集合。</param>
	/// <returns>source =&gt; value.Contains(source.Id)</returns>
	/// <exception cref="MissingMethodException">未找到 <see cref="Enumerable.Contains{TSource}(IEnumerable{TSource}, TSource)"/> 方法时抛出。</exception>
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