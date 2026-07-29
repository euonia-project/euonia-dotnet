using System.Linq.Expressions;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// The class used to implement the repository pattern for MongoDB.
/// </summary>
/// <typeparam name="TContext"></typeparam>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class MongoRepository<TContext, TEntity, TKey> : Repository<TContext, TEntity, TKey>
	where TKey : IEquatable<TKey>
	where TEntity : class, IEntity<TKey>
	where TContext : MongoDbContext, IRepositoryContext
{
	/// <summary>
	/// Initialize a new instance of <see cref="MongoRepository{TContext, TEntity, TKey}"/> with context.
	/// </summary>
	/// <param name="provider"></param>
	public MongoRepository(IContextProvider provider)
		: base(provider)
	{
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		// ignore.
	}

	/// <inheritdoc />
	public override IQueryable<TEntity> Queryable()
	{
		IQueryable<TEntity> query = Context.Collection<TEntity>().AsQueryable();
		if (Actions.Count > 0)
		{
			query = Actions.Aggregate(query, (current, action) => action(current));
		}

		return query;
	}

	/// <inheritdoc />
	public override IQueryable<TEntity> BuildQuery(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		IQueryable<TEntity> query = Context.Collection<TEntity>().AsQueryable();
		if (handle != null)
		{
			query = handle(query);
		}
		return query.Where(predicate);
	}

	/// <inheritdoc />
	public override async Task<TEntity> GetAsync(TKey key, CancellationToken cancellationToken = default)
	{
		var id = MongoDB.Bson.ObjectId.Parse(key.ToString());
		return await Context.FindAsync<TEntity>(id, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(predicate);
		var options = new FindOptions<TEntity> { Limit = 1 };
		var query = await Context.FindAsync(predicate, options, cancellationToken);
		return await query.FirstOrDefaultAsync(cancellationToken);
	}

	/// <inheritdoc />
	public override Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		return BuildQuery(predicate, handle).ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public override Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, int offset, int count, CancellationToken cancellationToken = default)
	{
		return BuildQuery(predicate, handle).Skip(offset).Take(count).ToListAsync(cancellationToken);
	}

	/// <inheritdoc />
	public override async Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		var result = await Context.Collection<TEntity>().CountDocumentsAsync(predicate, null, cancellationToken);
		return (int)result;
	}

	/// <inheritdoc />
	public override async Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		var result = await Context.Collection<TEntity>().CountDocumentsAsync(predicate, null, cancellationToken);
		return result;
	}

	/// <inheritdoc />
	public override Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		return BuildQuery(predicate, handle).AnyAsync(predicate, cancellationToken);
	}

	/// <inheritdoc />
	public override Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		return AnyAsync(predicate, handle, cancellationToken).ContinueWith(task => !task.Result, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		var document = await Context.InsertAsync(entity, cancellationToken);
		//var id = document["_id"];
		var result = BsonSerializer.Deserialize<TEntity>(document);
		return result;
	}

	/// <inheritdoc />
	public override async Task InsertAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		await Context.InsertAsync(entities, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		await Context.UpdateAsync(MongoDB.Bson.ObjectId.Parse(entity.Id.ToString()), entity, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task UpdateAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		await Context.UpdateAsync(entities, t => t.Id, cancellationToken);
	}

	/// <inheritdoc />
	public override async Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		await Context.DeleteAsync<TEntity>(MongoDB.Bson.ObjectId.Parse(entity.Id.ToString()), cancellationToken);
	}

	/// <inheritdoc />
	public override async Task DeleteAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		var keys = entities.Select(t => t.Id);
		await Context.DeleteAsync<TEntity>(t => keys.Contains(t.Id), cancellationToken);
	}
}