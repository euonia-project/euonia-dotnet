# Euonia.Repository 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Repository |
| 公共类型 | 26 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddContextProvider(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

## 命名空间 Nerosoft.Euonia.Repository

### Nerosoft.Euonia.Repository.ConnectionStringAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Repository.ConnectionStringAttribute()
- System.String Name { get; set; }
- System.String Value { get; set; }

### Nerosoft.Euonia.Repository.DefaultContextFactory

**类型**：class
接口 Nerosoft.Euonia.Repository.IContextFactory、System.ITransientDependency

- new Nerosoft.Euonia.Repository.DefaultContextFactory(System.IServiceProvider provider)
- System.Int32 Order { get; }
- TContext GetContext<TContext>()

### Nerosoft.Euonia.Repository.DefaultContextProvider

**类型**：class
接口 Nerosoft.Euonia.Repository.IContextProvider

- new Nerosoft.Euonia.Repository.DefaultContextProvider(System.IServiceProvider provider)
- TContext GetContext<TContext>()
- void SetFactory<TContext>(Func<TContext> factory)

### Nerosoft.Euonia.Repository.Entity

**类型**：class
接口 Nerosoft.Euonia.Repository.IEntity

- System.Object[] GetKeys()
- System.String ToString()

### Entity<TKey>

**类型**：class
基类 Nerosoft.Euonia.Repository.Entity；接口 Nerosoft.Euonia.Repository.IEntity、IEntity<TKey>

- TKey Id { get; set; }
- System.Object[] GetKeys()
- System.String ToString()

### IAuditable<TUser>

**类型**：interface
接口 Nerosoft.Euonia.Repository.IHasCreateTime、Nerosoft.Euonia.Repository.IHasUpdateTime

- TUser CreatedBy { get; set; }
- TUser UpdatedBy { get; set; }

### IConcurrentEntity<T>

**类型**：interface

- T Version { get; set; }

### IConnectionStringResolver<TContext>

**类型**：interface

- Task<System.String> GetConnectionStringAsync(System.Threading.CancellationToken cancellation = default)

### Nerosoft.Euonia.Repository.IContextFactory

**类型**：interface
接口 System.ITransientDependency

- System.Int32 Order { get; }
- TContext GetContext<TContext>()

### Nerosoft.Euonia.Repository.IContextProvider

**类型**：interface

- TContext GetContext<TContext>()
- void SetFactory<TContext>(Func<TContext> factory)

### Nerosoft.Euonia.Repository.IEntity

**类型**：interface

- System.Object[] GetKeys()

### IEntity<TKey>

**类型**：interface
接口 Nerosoft.Euonia.Repository.IEntity

- TKey Id { get; set; }

### Nerosoft.Euonia.Repository.IHasCreateTime

**类型**：interface

- System.DateTime CreatedAt { get; set; }

### Nerosoft.Euonia.Repository.IHasDeleteTime

**类型**：interface

- Nullable<System.DateTime> DeletedAt { get; set; }

### Nerosoft.Euonia.Repository.IHasSnowflakeId

**类型**：interface

- System.Int64 Id { get; set; }

### Nerosoft.Euonia.Repository.IHasUpdateTime

**类型**：interface

- System.DateTime UpdatedAt { get; set; }

### IRepository<TEntity>

**类型**：interface
接口 System.IDisposable

- List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> Actions { get; }
- Task<System.Boolean> AllAsync(Expression<Func<TEntity, System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Boolean> AllAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Boolean> AnyAsync(Expression<Func<TEntity, System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Boolean> AnyAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<TEntity> BuildQuery(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle)
- Task<System.Int32> CountAsync(Expression<Func<TEntity, System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int32> CountAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(TEntity entity, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(IEnumerable<TEntity> entities, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(Expression<Func<TEntity, System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(Expression<Func<TEntity, System.Boolean>> predicate, System.Int32 offset, System.Int32 count, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Int32 offset, System.Int32 count, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> GetAsync(Expression<Func<TEntity, System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> GetAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> InsertAsync(TEntity entity, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task InsertAsync(IEnumerable<TEntity> entities, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int64> LongCountAsync(Expression<Func<TEntity, System.Boolean>> predicate, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int64> LongCountAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<TEntity> Queryable()
- Task<System.Int32> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(TEntity entity, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(IEnumerable<TEntity> entities, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)

### IRepository<TEntity, TKey>

**类型**：interface
接口 IRepository<TEntity>、System.IDisposable

- Task<List<TEntity>> FindAsync(IEnumerable<TKey> keys, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(IEnumerable<TKey> keys, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> GetAsync(TKey key, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> GetAsync(TKey key, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)

### IRepository<TContext, TEntity, TKey>

**类型**：interface
接口 IRepository<TEntity, TKey>、IRepository<TEntity>、System.IDisposable

- TContext Context { get; }

### Nerosoft.Euonia.Repository.IRepositoryContext

**类型**：interface
接口 System.IDisposable

- System.Guid Id { get; }
- System.String Provider { get; }
- System.Threading.Tasks.Task BeginTransactionAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Data.IDbConnection GetConnection()
- IEnumerable<System.Object> GetTrackedEntries()
- System.Data.IDbTransaction GetTransaction()
- System.Threading.Tasks.Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int32> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
- IQueryable<TEntity> SetOf<TEntity>()

### Nerosoft.Euonia.Repository.ITombstone

**类型**：interface

- System.Boolean IsDeleted { get; set; }

### Repository<TContext, TEntity, TKey>

**类型**：class
基类 System.DisposableObject；接口 System.IDisposable、IRepository<TContext, TEntity, TKey>、IRepository<TEntity, TKey>、IRepository<TEntity>

- List<Func<IQueryable<TEntity>, IQueryable<TEntity>>> Actions { get; }
- TContext Context { get; }
- Task<System.Boolean> AllAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Boolean> AnyAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<TEntity> BuildQuery(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle)
- Task<System.Int32> CountAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(TEntity entity, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task DeleteAsync(IEnumerable<TEntity> entities, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<List<TEntity>> FindAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Int32 offset, System.Int32 count, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> GetAsync(TKey key, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> GetAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- Task<TEntity> InsertAsync(TEntity entity, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task InsertAsync(IEnumerable<TEntity> entities, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int64> LongCountAsync(Expression<Func<TEntity, System.Boolean>> predicate, Func<IQueryable<TEntity>, IQueryable<TEntity>> handle, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<TEntity> Queryable()
- Task<System.Int32> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(TEntity entity, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(IEnumerable<TEntity> entities, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Repository.RepositoryExtensions

**类型**：static class

- static Task<PageableCollection<TEntity>> GetPagedCollectionAsync<TEntity>(IQueryable<TEntity> source, Func<IQueryable<TEntity>, System.Threading.CancellationToken, Task<IList<TEntity>>> action, Nullable<System.Int32> page, Nullable<System.Int32> size, System.Threading.CancellationToken cancellationToken = default)
- static Task<PageableCollection<TEntity>> GetPagedCollectionAsync<TEntity>(IQueryable<TEntity> source, Func<IQueryable<TEntity>, System.Threading.CancellationToken, Task<IList<TEntity>>> action, Action<Orderable<TEntity>> order, Nullable<System.Int32> page, Nullable<System.Int32> size, System.Threading.CancellationToken cancellationToken = default)
- static IQueryable<TEntity> Paginate<TEntity>(IQueryable<TEntity> source, System.Int32 page, System.Int32 size, Func<IQueryable<TEntity>, IQueryable<TEntity>> sort)
- static IQueryable<TEntity> Paginate<TEntity>(IQueryable<TEntity> source, System.Int32 page, System.Int32 size, System.String[] sorts)
- static IQueryable<TEntity> Paginate<TEntity>(IQueryable<TEntity> source, System.Int32 page, System.Int32 size, IDictionary<System.String, Nerosoft.Euonia.Linq.SortType> sorts)
- static IQueryable<TEntity> Paginate<TEntity>(IQueryable<TEntity> source, System.Int32 page, System.Int32 size)
- static IQueryable<TEntity> Sort<TEntity>(IQueryable<TEntity> source, Func<IQueryable<TEntity>, IQueryable<TEntity>> sort)
- static IQueryable<TEntity> Sort<TEntity>(IQueryable<TEntity> source, System.String[] sorts)
- static IQueryable<TEntity> Sort<TEntity>(IQueryable<TEntity> source, IDictionary<System.String, Nerosoft.Euonia.Linq.SortType> sorts)

### Nerosoft.Euonia.Repository.RepositoryModule

**类型**：class
基类 Nerosoft.Euonia.Modularity.ModuleContextBase；接口 Nerosoft.Euonia.Modularity.IModuleContext

- new Nerosoft.Euonia.Repository.RepositoryModule()
- void ConfigureServices(Nerosoft.Euonia.Modularity.ServiceConfigurationContext context)

### Nerosoft.Euonia.Repository.RepositoryTelemetry

**类型**：static class

- static const System.String DurationMetric = euonia.repository.duration
- static const System.String ExecuteActivity = euonia.repository.execute
- static readonly System.Diagnostics.ActivitySource Source
- static const System.String SourceName = Nerosoft.Euonia.Repository
- static void Complete(System.Diagnostics.Activity activity, System.String operation, System.Boolean success, System.Diagnostics.Stopwatch stopwatch)
- static System.Diagnostics.Activity Start(System.String operation, System.String entityName)
