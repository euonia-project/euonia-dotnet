using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 提供 <see cref="IRepository{TEntity,TKey}"/> 的 EF Core 相关扩展方法（预加载、跟踪控制与附加实体）。
/// </summary>
/// <remarks>
/// <see cref="Include{TEntity,TKey}(IRepository{TEntity,TKey},string)"/> 等方法会将查询处理委托追加到仓储的
/// <see cref="IRepository{TEntity}.Actions"/> 集合，从而在构造查询时生效。
/// </remarks>
public static class RepositoryExtensions
{
	/// <summary>
	/// 指定查询结果中需要预加载的关联实体，以导航属性名称表示。
	/// </summary>
	/// <typeparam name="TEntity">被查询的实体类型。</typeparam>
	/// <typeparam name="TKey">实体主键类型。</typeparam>
	/// <param name="repository">目标仓储。</param>
	/// <param name="property">要预加载的导航属性名称。</param>
	/// <returns>已添加预加载行为的仓储实例，以便链式调用。</returns>
	public static IRepository<TEntity, TKey> Include<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string property)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
	{
		repository.Actions.Add(query => query.Include(property));
		return repository;
	}

	/// <summary>
	/// 在指定条件成立时，才将关联实体的预加载行为添加到查询中。
	/// </summary>
	/// <typeparam name="TEntity">被查询的实体类型。</typeparam>
	/// <typeparam name="TKey">实体主键类型。</typeparam>
	/// <param name="repository">目标仓储。</param>
	/// <param name="condition">是否应用预加载。</param>
	/// <param name="property">要预加载的导航属性名称。</param>
	/// <returns>仓储实例，以便链式调用。</returns>
	public static IRepository<TEntity, TKey> IncludeIf<TEntity, TKey>(this IRepository<TEntity, TKey> repository, bool condition, string property)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
	{
		if (!condition)
		{
			return repository;
		}

		repository.Actions.Add(query => query.Include(property));
		return repository;
	}

	/// <summary>
	/// 指定查询结果中需要预加载的多个关联实体，以导航属性名称表示。
	/// </summary>
	/// <typeparam name="TEntity">被查询的实体类型。</typeparam>
	/// <typeparam name="TKey">实体主键类型。</typeparam>
	/// <param name="repository">目标仓储。</param>
	/// <param name="properties">要预加载的导航属性名称集合。</param>
	/// <returns>已添加预加载行为的仓储实例，以便链式调用。</returns>
	public static IRepository<TEntity, TKey> Include<TEntity, TKey>(this IRepository<TEntity, TKey> repository, params string[] properties)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
	{
		foreach (var property in properties)
		{
			repository.Include(property);
		}

		return repository;
	}

	/// <summary>
	/// 指定查询结果中需要预加载的关联实体，以强类型表达式表示。
	/// </summary>
	/// <typeparam name="TEntity">被查询的实体类型。</typeparam>
	/// <typeparam name="TKey">实体主键类型。</typeparam>
	/// <typeparam name="TProperty">要预加载的关联实体的类型。</typeparam>
	/// <param name="repository">目标仓储。</param>
	/// <param name="property">表示要预加载的导航属性的表达式，例如 <c>t =&gt; t.Property1</c>。</param>
	/// <returns>已添加预加载行为的仓储实例，以便链式调用。</returns>
	public static IRepository<TEntity, TKey> Include<TEntity, TKey, TProperty>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, TProperty>> property)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
	{
		repository.Actions.Add(query => query.Include(property));
		return repository;
	}

	/// <summary>
	/// 在指定条件成立时，才将关联实体的预加载行为（强类型表达式形式）添加到查询中。
	/// </summary>
	/// <typeparam name="TEntity">被查询的实体类型。</typeparam>
	/// <typeparam name="TKey">实体主键类型。</typeparam>
	/// <typeparam name="TProperty">要预加载的关联实体的类型。</typeparam>
	/// <param name="repository">目标仓储。</param>
	/// <param name="condition">是否应用预加载。</param>
	/// <param name="property">表示要预加载的导航属性的表达式，例如 <c>t =&gt; t.Property1</c>。</param>
	/// <returns>仓储实例，以便链式调用。</returns>
	public static IRepository<TEntity, TKey> IncludeIf<TEntity, TKey, TProperty>(this IRepository<TEntity, TKey> repository, bool condition, Expression<Func<TEntity, TProperty>> property)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
	{
		if (!condition)
		{
			return repository;
		}

		repository.Actions.Add(query => query.Include(property));
		return repository;
	}

	/// <summary>
	/// 设置查询是否跟踪实体的变更。
	/// </summary>
	/// <param name="repository">目标仓储。</param>
	/// <param name="tracking"><c>true</c> 表示跟踪数据变更（<c>AsTracking</c>），<c>false</c> 表示不跟踪（<c>AsNoTracking</c>）。</param>
	/// <typeparam name="TEntity">被查询的实体类型。</typeparam>
	/// <typeparam name="TKey">实体主键类型。</typeparam>
	/// <returns>已添加跟踪行为的仓储实例，以便链式调用。</returns>
	public static IRepository<TEntity, TKey> Tracking<TEntity, TKey>(this IRepository<TEntity, TKey> repository, bool tracking = true)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
	{
		repository.Actions.Add(query => tracking ? query.AsTracking() : query.AsNoTracking());
		return repository;
	}

	/// <summary>
	/// 将已存在的实体以未修改状态附加到上下文的变更跟踪器中。
	/// </summary>
	/// <typeparam name="TContext">仓储上下文类型。</typeparam>
	/// <typeparam name="TEntity">实体类型。</typeparam>
	/// <typeparam name="TKey">实体主键类型。</typeparam>
	/// <param name="repository">目标仓储。</param>
	/// <param name="entity">要附加的实体。</param>
	/// <returns>附加后的实体。</returns>
	/// <exception cref="InvalidOperationException">仓储的 <see cref="IRepository{TContext,TEntity,TKey}.Context"/> 不是 <see cref="DbContext"/> 时抛出。</exception>
	public static TEntity Attach<TContext, TEntity, TKey>(this IRepository<TContext, TEntity, TKey> repository, TEntity entity)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
		where TContext : class, IRepositoryContext
	{
		if (repository.Context is not DbContext context)
		{
			throw new InvalidOperationException("The repository context is not a DbContext.");
		}

		var entry = context.Attach(entity);
		return entry.Entity;
	}
}