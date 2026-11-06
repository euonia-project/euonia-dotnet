using System.Diagnostics;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Nerosoft.Euonia.Domain;
using Nerosoft.Euonia.Linq;
using Nerosoft.Euonia.Repository;
using Nerosoft.Euonia.Repository.EfCore;

namespace Nerosoft.Euonia.Sample.Persist;

/// <summary>
/// 为 Entity Framework Core 提供基础仓储实现，继承
/// <see cref="EfCoreRepository{TContext, TEntity, TKey}"/> 并公开常用辅助方法。
/// </summary>
/// <typeparam name="TContext">派生自 <see cref="DataContextBase{TContext}"/> 的 DbContext 类型。</typeparam>
/// <typeparam name="TEntity">仓储所管理的实体类型，必须实现 <see cref="Euonia.Domain.IEntity{TKey}"/>。</typeparam>
/// <typeparam name="TKey">实体主键的类型，必须实现 <see cref="IEquatable{TKey}"/>。</typeparam>
internal abstract class BaseRepository<TContext, TEntity, TKey> : EfCoreRepository<TContext, TEntity, TKey>, IScopedDependency
	where TContext : DataContextBase<TContext>
	where TEntity : class, Repository.IEntity<TKey>
	where TKey : IEquatable<TKey>
{
	/// <summary>
	/// 初始化 <see cref="BaseRepository{TContext, TEntity, TKey}"/> 类的新实例。
	/// </summary>
	/// <param name="provider">用于获取 EF Core <see cref="DbContext"/> 实例的上下文提供程序。</param>
	protected BaseRepository(IContextProvider provider)
		: base(provider)
	{
	}

	/// <summary>
	/// 获取指定实体类型 <typeparamref name="T"/> 的可查询集合。
	/// </summary>
	/// <typeparam name="T">要查询的实体 CLR 类型，必须为类。</typeparam>
	/// <returns>针对所请求实体类型的 <see cref="IQueryable{T}"/>，可基于当前 <see cref="DbContext"/> 进行组合与执行。</returns>
	public IQueryable<T> SetOf<T>()
		where T : class
	{
		return Context.SetOf<T>();
	}

	/// <summary>
	/// 获取具有指定主键的第一个实体。
	/// </summary>
	/// <param name="id">要查找的主键值。</param>
	/// <param name="tracking">若为 <c>true</c>，返回的实体将由上下文跟踪；否则禁用跟踪。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>
	/// 表示异步操作的任务。若找到实体，任务结果包含该实体实例；否则为 <c>null</c>。
	/// </returns>
	public virtual Task<TEntity?> GetAsync(TKey id, bool tracking, CancellationToken cancellationToken = default)
	{
		//var lambda = predicate.Compile();
		return GetAsync(id, tracking, [], cancellationToken);
	}

	/// <summary>
	/// 获取具有指定主键的第一个实体，并可选择包含导航属性。
	/// </summary>
	/// <param name="id">要查找的主键值。</param>
	/// <param name="tracking">若为 <c>true</c>，返回的实体将由上下文跟踪；否则禁用跟踪。</param>
	/// <param name="properties">要包含在查询中的导航属性名称数组，可为空。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>
	/// 表示异步操作的任务。若找到实体，任务结果包含该实体实例；否则为 <c>null</c>。
	/// </returns>
	public virtual Task<TEntity?> GetAsync(TKey id, bool tracking, string[] properties, CancellationToken cancellationToken = default)
	{
		var predicate = PredicateBuilder.PropertyEqual<TEntity, TKey>(nameof(Euonia.Domain.IEntity<TKey>.Id), id);

		//var lambda = predicate.Compile();
		return GetAsync(predicate, tracking, properties, cancellationToken);
	}

	/// <summary>
	/// 获取与指定谓词匹配的第一个实体。
	/// </summary>
	/// <param name="predicate">用于筛选实体的谓词表达式。</param>
	/// <param name="tracking">若为 <c>true</c>，返回的实体将由上下文跟踪；否则禁用跟踪。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>
	/// 表示异步操作的任务。若找到实体，任务结果包含该实体实例；否则为 <c>null</c>。
	/// </returns>
	public virtual Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, bool tracking, CancellationToken cancellationToken = default)
	{
		return GetAsync(predicate, tracking, [], cancellationToken);
	}

	/// <summary>
	/// 获取与指定谓词匹配的第一个实体，并可选择包含导航属性。
	/// </summary>
	/// <param name="predicate">用于筛选实体的谓词表达式。</param>
	/// <param name="tracking">若为 <c>true</c>，返回的实体将由上下文跟踪；否则禁用跟踪。</param>
	/// <param name="properties">要包含在查询中的导航属性名称数组，可为空。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>
	/// 表示异步操作的任务。若找到实体，任务结果包含该实体实例；否则为 <c>null</c>。
	/// </returns>
	public virtual Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, bool tracking, string[] properties, CancellationToken cancellationToken = default)
	{
		return base.GetAsync(predicate, query => BuildQuery(query, tracking, properties), cancellationToken);
	}

	/// <summary>
	/// 判断是否存在与指定条件匹配的实体。
	/// </summary>
	/// <param name="expression">用于筛选实体的条件表达式。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>表示异步操作的任务。若存在匹配的实体，任务结果为 <c>true</c>；否则为 <c>false</c>。</returns>
	public virtual Task<bool> AnyAsync(Expression<Func<TEntity, bool>> expression, CancellationToken cancellationToken = default)
	{
		return Context.Set<TEntity>().AnyAsync(expression, cancellationToken);
	}

	/// <summary>
	/// 查找主键包含在指定集合中的实体。
	/// </summary>
	/// <param name="ids">要查找的主键值集合。</param>
	/// <param name="properties">要包含在查询中的导航属性名称数组，可为空。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>
	/// 表示异步操作的任务。任务结果包含匹配的实体列表（可能为空）。
	/// </returns>
	public virtual Task<List<TEntity>> FindAsync(IEnumerable<TKey> ids, string[] properties, CancellationToken cancellationToken = default)
	{
		var predicate = PredicateBuilder.PropertyInRange<TEntity, TKey>(nameof(Euonia.Domain.IEntity<TKey>.Id), ids.ToArray());
		return FindAsync(predicate, properties, cancellationToken);
	}

	/// <summary>
	/// 查找与指定谓词匹配的实体，并可选择包含导航属性。
	/// </summary>
	/// <param name="predicate">用于筛选实体的谓词表达式。</param>
	/// <param name="properties">要包含在查询中的导航属性名称数组，可为空。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>
	/// 表示异步操作的任务。任务结果包含匹配的实体列表（可能为空）。
	/// </returns>
	public virtual Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, string[] properties, CancellationToken cancellationToken = default)
	{
		return base.FindAsync(predicate, query => BuildQuery(query, false, properties), cancellationToken);
	}

	/// <summary>
	/// 使用指定的查询委托查找实体。
	/// </summary>
	/// <param name="handle">应用于无跟踪查询的委托，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>表示异步操作的任务。任务结果包含查询得到的实体列表（可能为空）。</returns>
	/// <remarks>
	/// <paramref name="handle"/> 的返回值不会被使用，因此通过该委托追加的筛选不会影响最终结果；需要筛选时请改用接受谓词的重载。
	/// </remarks>
	public virtual Task<List<TEntity>> FindAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		var query = Context.Set<TEntity>().AsNoTracking();
		if (handle != null)
		{
			_ = handle(query);
		}

		return query.ToListAsync(cancellationToken);
	}

	/// <summary>
	/// 使用指定的查询委托统计实体数量。
	/// </summary>
	/// <param name="handle">应用于无跟踪查询的委托，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>表示异步操作的任务。任务结果包含满足查询条件的实体数量。</returns>
	/// <remarks>
	/// <paramref name="handle"/> 的返回值不会被使用，因此通过该委托追加的筛选不会影响最终结果；需要筛选时请改用接受谓词的重载。
	/// </remarks>
	public virtual Task<int> CountAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		var query = Context.Set<TEntity>().AsNoTracking();
		if (handle != null)
		{
			_ = handle(query);
		}

		return query.CountAsync(cancellationToken);
	}

	/// <summary>
	/// 使用指定的查询委托获取单个实体。
	/// </summary>
	/// <param name="handle">应用于无跟踪查询的委托，可为 <c>null</c>。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>表示异步操作的任务。任务结果包含查询到的第一个实体；若无匹配结果则为 <c>null</c>。</returns>
	/// <remarks>
	/// <paramref name="handle"/> 的返回值不会被使用，因此通过该委托追加的筛选不会影响最终结果；需要筛选时请改用接受谓词的重载。
	/// </remarks>
	public virtual Task<TEntity?> GetAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, CancellationToken cancellationToken = default)
	{
		var query = Context.Set<TEntity>().AsNoTracking();
		if (handle != null)
		{
			_ = handle(query);
		}

		return query.FirstOrDefaultAsync(cancellationToken);
	}

	/// <summary>
	/// 按 ID 查找实体，并使用提供的选择器选取键值对。
	/// </summary>
	/// <param name="ids">要查找实体的主键值集合。</param>
	/// <param name="selector">将实体投影为键值对的选择器，键为返回字典的键，值为返回字典的值。</param>
	/// <param name="cancellationToken">在等待任务完成期间观察的 <see cref="CancellationToken"/>。</param>
	/// <returns>表示异步操作的任务。任务结果是以选择器返回的键为键、对应值为值的字典；无匹配实体时为空字典。</returns>
	/// <exception cref="ArgumentException">当 <paramref name="selector"/> 为多个实体返回相同的键时引发。</exception>
	public virtual async Task<Dictionary<TKey, string>> LookupAsync(IEnumerable<TKey> ids, Expression<Func<TEntity, KeyValuePair<TKey, string>>> selector, CancellationToken cancellationToken = default)
	{
		var predicate = PredicateBuilder.PropertyInRange<TEntity, TKey>(nameof(Euonia.Domain.IEntity<TKey>.Id), ids.ToArray());
		var query = Context.Set<TEntity>().AsNoTracking()
						   .Where(predicate)
						   .Select(selector);

		var items = await query.ToListAsync(cancellationToken: cancellationToken);

		var result = items.ToDictionary(x => x.Key, x => x.Value);

		return result;
	}

	/// <summary>
	/// 删除具有指定主键的实体。
	/// </summary>
	/// <param name="id">要删除实体的主键。</param>
	/// <param name="autoSave">若为 <c>true</c>，则在移除后调用 <see cref="SaveChangesAsync(CancellationToken)"/>。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>表示异步删除操作的任务。</returns>
	/// <exception cref="NotFoundException">当找不到具有指定 id 的实体时引发。</exception>
	public virtual async Task DeleteAsync(TKey id, bool autoSave = true, CancellationToken cancellationToken = default)
	{
		var set = Context.Set<TEntity>();

		var entity = await set.FindAsync([id], cancellationToken: cancellationToken);
		if (entity is null)
		{
			throw new NotFoundException();
		}

		set.Remove(entity);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}
	}

	/// <summary>
	/// 删除具有指定主键的实体，并引发由 <paramref name="eventFactory"/> 生成的领域事件。
	/// </summary>
	/// <typeparam name="TEvent">要引发的领域事件类型。</typeparam>
	/// <param name="id">要删除实体的主键。</param>
	/// <param name="eventFactory">在删除前根据实体创建领域事件的工厂。</param>
	/// <param name="autoSave">若为 <c>true</c>，则在移除后调用 <see cref="SaveChangesAsync(CancellationToken)"/>。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>表示异步删除操作的任务。</returns>
	/// <exception cref="NotFoundException">当找不到具有指定 id 的实体时引发。</exception>
	public virtual async Task DeleteAsync<TEvent>(TKey id, Func<TEvent> eventFactory, bool autoSave = true, CancellationToken cancellationToken = default)
		where TEvent : DomainEvent
	{
		var set = Context.Set<TEntity>();

		var entity = await set.FindAsync([id], cancellationToken: cancellationToken);
		switch (entity)
		{
			case null:
				throw new NotFoundException();
			case IHasDomainEvents aggregate:
				{
					var @event = eventFactory();
					aggregate.RaiseEvent(@event);
					break;
				}
		}

		set.Remove(entity);
		if (autoSave)
		{
			await SaveChangesAsync(cancellationToken);
		}
	}


	/// <summary>
	/// 删除指定的实体，并可选择引发由 <paramref name="eventFactory"/> 生成的领域事件。
	/// </summary>
	/// <typeparam name="TEvent">要引发的领域事件类型。</typeparam>
	/// <param name="entity">要删除的实体实例。</param>
	/// <param name="eventFactory">创建领域事件的工厂；仅当实体实现 <see cref="IHasDomainEvents"/> 时才会调用。</param>
	/// <param name="autoSave">若为 <c>true</c>，则在移除后调用 <see cref="SaveChangesAsync(CancellationToken)"/>。</param>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>表示异步删除操作的任务。</returns>
	public virtual Task DeleteAsync<TEvent>(TEntity entity, Func<TEvent> eventFactory, bool autoSave = true, CancellationToken cancellationToken = default)
		where TEvent : DomainEvent
	{
		if (entity is IHasDomainEvents aggregate)
		{
			var @event = eventFactory();
			aggregate.RaiseEvent(@event);
		}

		{
		}

		return DeleteAsync(entity, autoSave, cancellationToken);
	}

	/// <summary>
	/// 将当前 <see cref="DbContext"/> 中的更改持久化到基础数据库。
	/// 此方法重写基类实现，并将调用转发至 <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>。
	/// </summary>
	/// <param name="cancellationToken">用于取消异步操作的取消标记。</param>
	/// <returns>
	/// 表示异步保存操作的任务。任务结果包含写入数据库的状态条目数。
	/// </returns>
	public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return Context.SaveChangesAsync(cancellationToken);
	}

	/// <summary>
	/// 通过应用跟踪行为和包含路径，为仓储构建基础查询。
	/// </summary>
	/// <param name="query">要修改的基础可查询对象。</param>
	/// <param name="tracking">若为 <c>true</c>，返回的查询将通过 <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsTracking{TEntity}(IQueryable{TEntity})"/> 启用变更跟踪；否则应用 <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}(IQueryable{TEntity})"/>。</param>
	/// <param name="properties">要包含的导航属性名称数组。每个元素都会传递给 <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include{TEntity}(IQueryable{TEntity}, string)"/>。</param>
	/// <returns>已应用跟踪与包含设置的组合后的 <see cref="IQueryable{TEntity}"/>。</returns>
	protected virtual IQueryable<TEntity> BuildQuery(IQueryable<TEntity> query, bool tracking, string[] properties)
	{
		query = tracking ? query.AsTracking() : query.AsNoTracking();

		if (properties is { Length: > 0 })
		{
			query = properties.Aggregate(query, (current, property) => current.Include(property));
		}

		var sql = query.ToQueryString();
		Debug.WriteLine(sql);

		return query;
	}
}
