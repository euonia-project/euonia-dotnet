# Euonia.Repository.EfCore 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Repository.EfCore |
| 公共类型 | 22 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDataContextFactory<TContext>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, System.String connectionString = null, Func<Microsoft.EntityFrameworkCore.DbContext, System.Boolean, System.Threading.CancellationToken, System.Threading.Tasks.Task> seeding = null, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddDataContextFactory<TContext>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Func<System.IServiceProvider, System.String> connectionStringFactory, Func<Microsoft.EntityFrameworkCore.DbContext, System.Boolean, System.Threading.CancellationToken, System.Threading.Tasks.Task> seeding = null, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEfCoreRepository<TContext>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> options, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEfCoreRepository<TContextService, TContextImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> options, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEfCoreRepository<TContext>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<System.IServiceProvider, Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> options, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEfCoreRepository<TContextService, TContextImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<System.IServiceProvider, Microsoft.EntityFrameworkCore.DbContextOptionsBuilder> options, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddEfCoreRepository(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)

### Microsoft.Extensions.DependencyInjection.TenancyEfCoreServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTenantEfCore<TContext>(Microsoft.Extensions.DependencyInjection.IServiceCollection services)

## 命名空间 Nerosoft.Euonia.Repository.EfCore

### Nerosoft.Euonia.Repository.EfCore.ConnectionConfigurator

**类型**：delegate
接口 System.ICloneable、System.Runtime.Serialization.ISerializable

- new Nerosoft.Euonia.Repository.EfCore.ConnectionConfigurator(System.Object object, System.IntPtr method)
- System.IAsyncResult BeginInvoke(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder, System.String connectionString, System.AsyncCallback callback, System.Object object)
- Microsoft.EntityFrameworkCore.DbContextOptionsBuilder EndInvoke(System.IAsyncResult result)
- Microsoft.EntityFrameworkCore.DbContextOptionsBuilder Invoke(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder builder, System.String connectionString)

### DataContextBase<TContext>

**类型**：class
基类 Microsoft.EntityFrameworkCore.DbContext；接口 IInfrastructure<System.IServiceProvider>、Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies、Microsoft.EntityFrameworkCore.Internal.IDbSetCache、Microsoft.EntityFrameworkCore.Internal.IDbContextPoolable、Microsoft.EntityFrameworkCore.Infrastructure.IResettableService、System.IDisposable、System.IAsyncDisposable、Nerosoft.Euonia.Repository.IRepositoryContext

- System.Guid Id { get; }
- System.String Provider { get; }
- System.Threading.Tasks.Task BeginTransactionAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Data.IDbConnection GetConnection()
- IEnumerable<System.Object> GetTrackedEntries()
- System.Data.IDbTransaction GetTransaction()
- System.Threading.Tasks.Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Int32 SaveChanges()
- System.Int32 SaveChanges(System.Boolean acceptAllChangesOnSuccess)
- Task<System.Int32> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int32> SaveChangesAsync(System.Boolean acceptAllChangesOnSuccess, System.Threading.CancellationToken cancellationToken = default)
- IQueryable<TEntity> SetOf<TEntity>()

### Nerosoft.Euonia.Repository.EfCore.DataContextExtensions

**类型**：static class

- static System.String GetForeignKey(System.String entityName)
- static System.String GetTableName(System.Type type)
- static System.String GetTableName(System.String entityName)
- static void SetTombstoneQueryFilter(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
- static void SetTombstoneQueryFilter(Microsoft.EntityFrameworkCore.Metadata.IMutableEntityType entity)

### EfCoreRepository<TContext, TEntity, TKey>

**类型**：class
基类 Repository<TContext, TEntity, TKey>；接口 System.IDisposable、IRepository<TContext, TEntity, TKey>、IRepository<TEntity, TKey>、IRepository<TEntity>

- new EfCoreRepository<TContext, TEntity, TKey>(Nerosoft.Euonia.Repository.IContextProvider provider)
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
- System.Threading.Tasks.Task UpdateAsync(TEntity entity, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task UpdateAsync(IEnumerable<TEntity> entities, System.Boolean autoSave = true, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Repository.EfCore.GuidVersion7ValueGenerator

**类型**：class
基类 ValueGenerator<System.Guid>

- new Nerosoft.Euonia.Repository.EfCore.GuidVersion7ValueGenerator()
- System.Boolean GeneratesTemporaryValues { get; }
- System.Guid Next(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)

### Nerosoft.Euonia.Repository.EfCore.ITenantModelCacheKeySource

**类型**：interface

- Nerosoft.Euonia.Tenancy.ITenantInfo CurrentTenant { get; }

### MultiTenantDataContextBase<TContext>

**类型**：class
基类 DataContextBase<TContext>；接口 IInfrastructure<System.IServiceProvider>、Microsoft.EntityFrameworkCore.Internal.IDbContextDependencies、Microsoft.EntityFrameworkCore.Internal.IDbSetCache、Microsoft.EntityFrameworkCore.Internal.IDbContextPoolable、Microsoft.EntityFrameworkCore.Infrastructure.IResettableService、System.IDisposable、System.IAsyncDisposable、Nerosoft.Euonia.Repository.IRepositoryContext、Nerosoft.Euonia.Tenancy.ITenantFilterState、Nerosoft.Euonia.Repository.EfCore.ITenantModelCacheKeySource

- Nerosoft.Euonia.Tenancy.ITenantInfo CurrentTenant { get; }
- System.String CurrentTenantId { get; }
- System.Boolean IgnoreTenantFilter { get; }
- System.Boolean IsCrossTenant { get; }
- System.String TargetTenantId { get; }

### Nerosoft.Euonia.Repository.EfCore.RepositoryExtensions

**类型**：static class

- static TEntity Attach<TContext, TEntity, TKey>(IRepository<TContext, TEntity, TKey> repository, TEntity entity)
- static IRepository<TEntity, TKey> Include<TEntity, TKey>(IRepository<TEntity, TKey> repository, System.String property)
- static IRepository<TEntity, TKey> Include<TEntity, TKey>(IRepository<TEntity, TKey> repository, System.String[] properties)
- static IRepository<TEntity, TKey> Include<TEntity, TKey, TProperty>(IRepository<TEntity, TKey> repository, Expression<Func<TEntity, TProperty>> property)
- static IRepository<TEntity, TKey> IncludeIf<TEntity, TKey>(IRepository<TEntity, TKey> repository, System.Boolean condition, System.String property)
- static IRepository<TEntity, TKey> IncludeIf<TEntity, TKey, TProperty>(IRepository<TEntity, TKey> repository, System.Boolean condition, Expression<Func<TEntity, TProperty>> property)
- static IRepository<TEntity, TKey> Tracking<TEntity, TKey>(IRepository<TEntity, TKey> repository, System.Boolean tracking = true)

### Nerosoft.Euonia.Repository.EfCore.RowVersionValueGenerator

**类型**：class
基类 ValueGenerator<System.Byte[]>

- new Nerosoft.Euonia.Repository.EfCore.RowVersionValueGenerator()
- System.Boolean GeneratesTemporaryValues { get; }
- System.Byte[] Next(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)

### Nerosoft.Euonia.Repository.EfCore.SequentialGuidValueGenerator

**类型**：class
基类 ValueGenerator<System.Guid>

- new Nerosoft.Euonia.Repository.EfCore.SequentialGuidValueGenerator()
- System.Boolean GeneratesTemporaryValues { get; }
- System.Guid Next(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)

### Nerosoft.Euonia.Repository.EfCore.ShortUniqueIdValueGenerator

**类型**：class
基类 ValueGenerator<System.String>

- new Nerosoft.Euonia.Repository.EfCore.ShortUniqueIdValueGenerator()
- System.Boolean GeneratesTemporaryValues { get; }
- System.String Next(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)

### Nerosoft.Euonia.Repository.EfCore.SnowflakeIdValueGenerator

**类型**：class
基类 ValueGenerator<System.Int64>

- new Nerosoft.Euonia.Repository.EfCore.SnowflakeIdValueGenerator()
- System.Boolean GeneratesTemporaryValues { get; }
- System.Int64 Next(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)

### TenantConnectionStringResolver<TContext>

**类型**：class
接口 ITenantConnectionStringResolver<TContext>

- new TenantConnectionStringResolver<TContext>(IOptionsMonitor<Nerosoft.Euonia.Tenancy.MultiTenancyOptions> options)
- System.String Resolve(Nerosoft.Euonia.Tenancy.ITenantInfo tenant)

### Nerosoft.Euonia.Repository.EfCore.TenantDbContextServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddTenantDbContext<TContext>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Microsoft.EntityFrameworkCore.DbContextOptionsBuilder, System.String> configureProvider, Microsoft.Extensions.DependencyInjection.ServiceLifetime lifetime = 1)

### Nerosoft.Euonia.Repository.EfCore.TenantModelBuilderExtensions

**类型**：static class

- static Microsoft.EntityFrameworkCore.ModelBuilder ApplyTenantModel<TContext>(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder, TContext context)

### Nerosoft.Euonia.Repository.EfCore.TenantModelCacheKeyContext

**类型**：static class

- static ValueTuple<Nerosoft.Euonia.Tenancy.TenantIsolation, System.String> Current { get; }
- static void Clear()
- static void Set(Nerosoft.Euonia.Tenancy.TenantIsolation isolation, System.String schema)

### Nerosoft.Euonia.Repository.EfCore.TenantModelCacheKeyFactory

**类型**：class
接口 Microsoft.EntityFrameworkCore.Infrastructure.IModelCacheKeyFactory

- new Nerosoft.Euonia.Repository.EfCore.TenantModelCacheKeyFactory()
- System.Object Create(Microsoft.EntityFrameworkCore.DbContext context, System.Boolean designTime)

### Nerosoft.Euonia.Repository.EfCore.UlidValueGenerator

**类型**：class
基类 ValueGenerator<System.String>

- new Nerosoft.Euonia.Repository.EfCore.UlidValueGenerator()
- System.Boolean GeneratesTemporaryValues { get; }
- System.String Next(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)

### Nerosoft.Euonia.Repository.EfCore.UniversalTimeConverter

**类型**：class
基类 ValueConverter<System.DateTime, System.DateTime>

- new Nerosoft.Euonia.Repository.EfCore.UniversalTimeConverter()

### Nerosoft.Euonia.Repository.EfCore.UtcTimeValueGenerator

**类型**：class
基类 ValueGenerator<System.DateTime>

- new Nerosoft.Euonia.Repository.EfCore.UtcTimeValueGenerator()
- System.Boolean GeneratesTemporaryValues { get; }
- System.DateTime Next(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
