using System.Linq.Expressions;
using Nerosoft.Euonia.Linq;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 定义面向实体 <typeparamref name="TEntity"/> 的仓储契约，提供异步的查询、统计与增删改操作。
/// </summary>
/// <typeparam name="TEntity">仓储所管理的实体类型。</typeparam>
public interface IRepository<TEntity> : IDisposable
	where TEntity : class
{
	/// <summary>
	/// 获取在构造查询时依次应用的查询处理委托集合。
	/// </summary>
	/// <value>查询处理委托列表，由 <see cref="Queryable"/> 按顺序作用于初始查询。</value>
	List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> Actions { get; }

	/// <summary>
	/// 创建实体 <typeparamref name="TEntity"/> 的可查询对象，并依次应用 <see cref="Actions"/>。
	/// </summary>
	/// <returns>应用 <see cref="Actions"/> 之后的实体查询对象。</returns>
	IQueryable<TEntity> Queryable();

	/// <summary>
	/// 基于指定条件构建查询对象。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <returns>经过 <paramref name="handle"/> 处理并附加了 <paramref name="predicate"/> 条件的查询对象。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="predicate"/> 为 <c>null</c> 时抛出。</exception>
	IQueryable<TEntity> BuildQuery(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle);

	/// <summary>
	/// 异步获取满足指定条件的单个实体。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>第一个满足条件的实体；若不存在则为 <c>null</c>。</returns>
	Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return GetAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// 异步获取满足指定条件的单个实体，并允许对查询进行附加处理。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>第一个满足条件的实体；若不存在则为 <c>null</c>。</returns>
	Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步查找序列中所有满足指定条件的实体。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>满足条件的实体列表；无匹配项时为空列表。</returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return FindAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// 异步查找序列中所有满足指定条件的实体，并允许对查询进行附加处理。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>满足条件的实体列表；无匹配项时为空列表。</returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步分页查找序列中满足指定条件的实体。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="offset">跳过的实体数量。</param>
	/// <param name="count">最多返回的实体数量。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>当前分页内满足条件的实体列表；无匹配项时为空列表。</returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, int offset, int count, CancellationToken cancellationToken = default)
	{
		return FindAsync(predicate, null, offset, count, cancellationToken);
	}

	/// <summary>
	/// 异步分页查找序列中满足指定条件的实体，并允许对查询进行附加处理。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <param name="offset">跳过的实体数量。</param>
	/// <param name="count">最多返回的实体数量。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>当前分页内满足条件的实体列表；无匹配项时为空列表。</returns>
	Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, int offset, int count, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步获取序列中满足指定条件的实体数量。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>满足条件的实体数量。</returns>
	Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return CountAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// 异步获取序列中满足指定条件的实体数量，并允许对查询进行附加处理。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>满足条件的实体数量。</returns>
	Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步获取序列中满足指定条件的实体数量，并以 <see cref="long"/> 返回。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>满足条件的实体数量。</returns>
	Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return LongCountAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// 异步获取序列中满足指定条件的实体数量，并以 <see cref="long"/> 返回，同时允许对查询进行附加处理。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>满足条件的实体数量。</returns>
	Task<long> LongCountAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步判断序列中是否存在满足指定条件的实体。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>若存在满足条件的实体则为 <c>true</c>，否则为 <c>false</c>。</returns>
	Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return AnyAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// 异步判断序列中是否存在满足指定条件的实体，并允许对查询进行附加处理。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>若存在满足条件的实体则为 <c>true</c>，否则为 <c>false</c>。</returns>
	Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步判断序列中的所有实体是否都满足指定条件。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>若所有实体都满足条件则为 <c>true</c>，否则为 <c>false</c>。</returns>
	Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return AllAsync(predicate, null, cancellationToken);
	}

	/// <summary>
	/// 异步判断序列中的所有实体是否都满足指定条件，并允许对查询进行附加处理。
	/// </summary>
	/// <param name="predicate">用于筛选实体的条件表达式。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>若所有实体都满足条件则为 <c>true</c>，否则为 <c>false</c>。</returns>
	Task<bool> AllAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步新增单个实体。
	/// </summary>
	/// <param name="entity">要新增的实体。</param>
	/// <param name="autoSave">指示是否在新增后自动调用 <see cref="SaveChangesAsync"/>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>新增后的实体（可能包含由数据存储生成的标识等信息）。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="entity"/> 为 <c>null</c> 时抛出。</exception>
	Task<TEntity> InsertAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步批量新增实体。
	/// </summary>
	/// <param name="entities">要新增的实体集合。</param>
	/// <param name="autoSave">指示是否在新增后自动调用 <see cref="SaveChangesAsync"/>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="entities"/> 为 <c>null</c> 时抛出。</exception>
	Task InsertAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步更新已存在的单个实体。
	/// </summary>
	/// <param name="entity">要更新的实体。</param>
	/// <param name="autoSave">指示是否在更新后自动调用 <see cref="SaveChangesAsync"/>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="entity"/> 为 <c>null</c> 时抛出。</exception>
	Task UpdateAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步批量更新已存在的实体。
	/// </summary>
	/// <param name="entities">要更新的实体集合。</param>
	/// <param name="autoSave">指示是否在更新后自动调用 <see cref="SaveChangesAsync"/>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="entities"/> 为 <c>null</c> 时抛出。</exception>
	Task UpdateAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步删除指定的单个实体。
	/// </summary>
	/// <param name="entity">要删除的实体。</param>
	/// <param name="autoSave">指示是否在删除后自动调用 <see cref="SaveChangesAsync"/>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="entity"/> 为 <c>null</c> 时抛出。</exception>
	Task DeleteAsync(TEntity entity, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步批量删除指定的实体。
	/// </summary>
	/// <param name="entities">要删除的实体集合。</param>
	/// <param name="autoSave">指示是否在删除后自动调用 <see cref="SaveChangesAsync"/>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="entities"/> 为 <c>null</c> 时抛出。</exception>
	Task DeleteAsync(IEnumerable<TEntity> entities, bool autoSave = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步将所有挂起的变更保存到数据存储。
	/// </summary>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>表示异步操作的任务，其结果为实现保存而写入的记录数。</returns>
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 定义以 <typeparamref name="TKey"/> 为主键的实体仓储契约。
/// </summary>
/// <typeparam name="TEntity">仓储所管理的实体类型，须实现 <see cref="IEntity{TKey}"/>。</typeparam>
/// <typeparam name="TKey">实体主键的类型。</typeparam>
public interface IRepository<TEntity, in TKey> : IRepository<TEntity>
	where TKey : IEquatable<TKey>
	where TEntity : class, IEntity<TKey>
{
	/// <summary>
	/// 异步获取具有指定主键值的实体。
	/// </summary>
	/// <param name="key">实体的主键值。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>具有指定主键的实体；若不存在则为 <c>null</c>。</returns>
	Task<TEntity> GetAsync(TKey key, CancellationToken cancellationToken = default)
	{
		return GetAsync(key, null, cancellationToken);
	}

	/// <summary>
	/// 异步获取具有指定主键值的实体，并允许对查询进行附加处理。
	/// </summary>
	/// <param name="key">实体的主键值。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>具有指定主键的实体；若不存在则为 <c>null</c>。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="key"/> 为 <c>null</c> 时抛出。</exception>
	Task<TEntity> GetAsync(TKey key, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(key);
		return GetAsync(PredicateBuilder.PropertyEqual<TEntity, TKey>(nameof(IEntity<TKey>.Id), key), handle, cancellationToken);
	}

	/// <summary>
	/// 异步查找主键值包含在指定集合中的所有实体。
	/// </summary>
	/// <param name="keys">实体主键值集合。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>主键匹配的实体列表；<paramref name="keys"/> 为空时返回空列表。</returns>
	Task<List<TEntity>> FindAsync(IEnumerable<TKey> keys, CancellationToken cancellationToken = default)
	{
		return FindAsync(keys, null, cancellationToken);
	}

	/// <summary>
	/// 异步查找主键值包含在指定集合中的所有实体，并允许对查询进行附加处理。
	/// </summary>
	/// <param name="keys">实体主键值集合。</param>
	/// <param name="handle">在附加条件之前对查询进行的附加处理，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>主键匹配的实体列表；<paramref name="keys"/> 为空时返回空列表。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="keys"/> 为 <c>null</c> 时抛出。</exception>
	Task<List<TEntity>> FindAsync(IEnumerable<TKey> keys, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(keys);
		var array = keys as TKey[] ?? [.. keys];
		if (array.Length == 0)
		{
			return Task.FromResult(new List<TEntity>());
		}

		{
		}

		return FindAsync(PredicateBuilder.PropertyInRange<TEntity, TKey>(nameof(IEntity<>.Id), array), handle, cancellationToken);
	}
}

/// <summary>
/// 定义可访问指定仓储上下文 <typeparamref name="TContext"/> 的实体仓储契约。
/// </summary>
/// <typeparam name="TEntity">仓储所管理的实体类型，须实现 <see cref="IEntity{TKey}"/>。</typeparam>
/// <typeparam name="TKey">实体主键的类型。</typeparam>
/// <typeparam name="TContext">仓储所使用的数据持久化上下文类型。</typeparam>
public interface IRepository<out TContext, TEntity, in TKey> : IRepository<TEntity, TKey>
	where TKey : IEquatable<TKey>
	where TEntity : class, IEntity<TKey>
	where TContext : class, IRepositoryContext
{
	/// <summary>
	/// 获取仓储所使用的数据持久化上下文。
	/// </summary>
	/// <value>当前仓储关联的 <typeparamref name="TContext"/> 实例。</value>
	TContext Context { get; }
}