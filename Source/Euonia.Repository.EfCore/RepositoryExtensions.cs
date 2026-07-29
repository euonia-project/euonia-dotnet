using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// The extensions for <see cref="IRepository{TEntity,TKey}"/>.
/// </summary>
public static class RepositoryExtensions
{
	/// <summary>
	/// Specifies related entities to include in the query results. The navigation property to be included is specified starting with the type of entity being queried (TEntity).
	/// </summary>
	/// <typeparam name="TEntity">The type of entity being queried.</typeparam>
	/// <typeparam name="TKey">The type of entity primary key.</typeparam>
	/// <param name="repository"></param>
	/// <param name="property"></param>
	/// <returns>Repository with the related data included.</returns>
	public static IRepository<TEntity, TKey> Include<TEntity, TKey>(this IRepository<TEntity, TKey> repository, string property)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
	{
		repository.Actions.Add(query => query.Include(property));
		return repository;
	}

	/// <summary>
	/// Specifies related entities to include in the query results. The navigation property to be included is specified starting with the type of entity being queried (TEntity).
	/// </summary>
	/// <typeparam name="TEntity">The type of entity being queried.</typeparam>
	/// <typeparam name="TKey">The type of entity primary key.</typeparam>
	/// <param name="repository"></param>
	/// <param name="condition"></param>
	/// <param name="property"></param>
	/// <returns>Repository with the related data included.</returns>
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
	/// Specifies related entities to include in the query results. The navigation properties to be included is specified starting with the type of entity being queried (TEntity).
	/// </summary>
	/// <typeparam name="TEntity">The type of entity being queried.</typeparam>
	/// <typeparam name="TKey">The type of entity primary key.</typeparam>
	/// <param name="repository"></param>
	/// <param name="properties"></param>
	/// <returns>Repository with the related data included.</returns>
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
	/// Specifies related entities to include in the query results. The navigation property to be included is specified starting with the type of entity being queried (TEntity).
	/// </summary>
	/// <typeparam name="TEntity">The type of entity being queried.</typeparam>
	/// <typeparam name="TKey">The type of entity primary key.</typeparam>
	/// <typeparam name="TProperty">The type of the related entity to be included.</typeparam>
	/// <param name="repository"></param>
	/// <param name="property">A lambda expression representing the navigation property to be included (t => t.Property1).</param>
	/// <returns>Repository with the related data included.</returns>
	public static IRepository<TEntity, TKey> Include<TEntity, TKey, TProperty>(this IRepository<TEntity, TKey> repository, Expression<Func<TEntity, TProperty>> property)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
	{
		repository.Actions.Add(query => query.Include(property));
		return repository;
	}

	/// <summary>
	/// Specifies related entities to include in the query results. The navigation property to be included is specified starting with the type of entity being queried (TEntity).
	/// </summary>
	/// <typeparam name="TEntity">The type of entity being queried.</typeparam>
	/// <typeparam name="TKey">The type of entity primary key.</typeparam>
	/// <typeparam name="TProperty">The type of the related entity to be included.</typeparam>
	/// <param name="repository"></param>
	/// <param name="condition"></param>
	/// <param name="property">A lambda expression representing the navigation property to be included (t => t.Property1).</param>
	/// <returns>Repository with the related data included.</returns>
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
	/// Set whether track data changes or not.
	/// </summary>
	/// <param name="repository"></param>
	/// <param name="tracking"><c>true</c> if track data changes; otherwise <c>false</c>.</param>
	/// <typeparam name="TEntity"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <returns></returns>
	public static IRepository<TEntity, TKey> Tracking<TEntity, TKey>(this IRepository<TEntity, TKey> repository, bool tracking = true)
		where TKey : IEquatable<TKey>
		where TEntity : class, IEntity<TKey>
	{
		repository.Actions.Add(query => tracking ? query.AsTracking() : query.AsNoTracking());
		return repository;
	}

	/// <summary>
	/// Attach an exists entity in context.
	/// </summary>
	/// <typeparam name="TContext"></typeparam>
	/// <typeparam name="TEntity"></typeparam>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="repository"></param>
	/// <param name="entity"></param>
	/// <returns></returns>
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