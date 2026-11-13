# Euonia.Repository.Mongo 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Repository.Mongo |
| 公共类型 | 18 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMongoDbContext<TContextService, TContextImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Repository.Mongo.MongoDbOptions> configure, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMongoDbContext<TContext>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Repository.Mongo.MongoDbOptions> configure, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMongoRepository<TContext>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Repository.Mongo.MongoDbOptions> configure, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMongoRepository<TContextService, TContextImplementation>(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Repository.Mongo.MongoDbOptions> configure, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddMongoRepository(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Microsoft.Extensions.DependencyInjection.ServiceLifetime contextLifeTime = 1)

## 命名空间 Nerosoft.Euonia.Repository.Mongo

### DataContextBase<TContext>

**类型**：class
基类 Nerosoft.Euonia.Repository.Mongo.MongoDbContext；接口 Nerosoft.Euonia.Repository.IRepositoryContext、System.IDisposable

- System.Guid Id { get; }
- System.String Provider { get; }
- System.Threading.Tasks.Task BeginTransactionAsync(System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task CommitAsync(System.Threading.CancellationToken cancellationToken = default)
- void Dispose()
- System.Data.IDbConnection GetConnection()
- IEnumerable<System.Object> GetTrackedEntries()
- System.Data.IDbTransaction GetTransaction()
- System.Threading.Tasks.Task RollbackAsync(System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int32> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
- IQueryable<TEntity> SetOf<TEntity>()

### Nerosoft.Euonia.Repository.Mongo.IValueGenerator

**类型**：interface

- System.Object Generate()

### IValueGenerator<TValue>

**类型**：interface
接口 Nerosoft.Euonia.Repository.Mongo.IValueGenerator

- TValue Generate()

### Nerosoft.Euonia.Repository.Mongo.ModelBuilder

**类型**：class

- new Nerosoft.Euonia.Repository.Mongo.ModelBuilder()
- Nerosoft.Euonia.Repository.Mongo.ModelBuilder For<T>(Action<ModelProfile<T>> buildAction)

### Nerosoft.Euonia.Repository.Mongo.ModelKeyBuilder

**类型**：class

- Nerosoft.Euonia.Repository.Mongo.ModelKeyBuilder GenerateOnInsert(System.Boolean value)
- Nerosoft.Euonia.Repository.Mongo.ModelKeyBuilder UseValueGenerator<TGenerator>()

### Nerosoft.Euonia.Repository.Mongo.ModelProfile

**类型**：class

- new Nerosoft.Euonia.Repository.Mongo.ModelProfile()
- Nerosoft.Euonia.Repository.Mongo.ModelProfile AutoMap(System.Boolean value)
- Nerosoft.Euonia.Repository.Mongo.ModelProfile BypassValidation(System.Boolean value)
- Nerosoft.Euonia.Repository.Mongo.ModelKeyBuilder HasKey(System.String key, System.Type type = null)
- Nerosoft.Euonia.Repository.Mongo.ModelProfile ToCollection(System.String name)

### ModelProfile<TModel>

**类型**：class
基类 Nerosoft.Euonia.Repository.Mongo.ModelProfile

- new ModelProfile<TModel>()
- Nerosoft.Euonia.Repository.Mongo.ModelKeyBuilder HasKey(Expression<Func<TModel, System.Object>> selector)
- Nerosoft.Euonia.Repository.Mongo.ModelPropertyBuilder HasProperty(Expression<Func<TModel, System.Object>> selector)
- void Map(Action<BsonClassMap<TModel>> map)

### Nerosoft.Euonia.Repository.Mongo.ModelPropertyBuilder

**类型**：class

- Nerosoft.Euonia.Repository.Mongo.ModelPropertyBuilder HasElementName(System.String elementName)
- Nerosoft.Euonia.Repository.Mongo.ModelPropertyBuilder HasValueGenerator<TGenerator>()
- Nerosoft.Euonia.Repository.Mongo.ModelPropertyBuilder HasValueGenerator(Nerosoft.Euonia.Repository.Mongo.IValueGenerator generator)
- Nerosoft.Euonia.Repository.Mongo.ModelPropertyBuilder Required(System.Boolean value)

### Nerosoft.Euonia.Repository.Mongo.MongoDbContext

**类型**：class

- IMongoCollection<T> Collection<T>()
- IMongoCollection<MongoDB.Bson.BsonDocument> Collection(System.Type type)
- Task<System.Int32> DeleteAsync<T>(MongoDB.Bson.ObjectId id, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int32> DeleteAsync<T>(Expression<Func<T, System.Boolean>> filter, System.Threading.CancellationToken cancellationToken = default)
- T Find<T>(MongoDB.Bson.ObjectId id)
- IFindFluent<T, T> Find<T>(Expression<Func<T, System.Boolean>> filter)
- IFindFluent<T, T> Find<T>(FilterDefinition<T> filter)
- MongoDB.Bson.BsonDocument Find(System.String collectionName, MongoDB.Bson.ObjectId id)
- Task<T> FindAsync<T>(MongoDB.Bson.ObjectId id, System.Threading.CancellationToken cancellationToken = default)
- Task<IAsyncCursor<T>> FindAsync<T>(Expression<Func<T, System.Boolean>> filter, FindOptions<T> options, System.Threading.CancellationToken cancellationToken = default)
- Task<IAsyncCursor<T>> FindAsync<T>(FilterDefinition<T> filter, FindOptions<T> options, System.Threading.CancellationToken cancellationToken = default)
- Task<MongoDB.Bson.BsonDocument> FindAsync(System.String collectionName, MongoDB.Bson.ObjectId id)
- MongoDB.Bson.BsonDocument Insert<T>(T model)
- MongoDB.Bson.BsonDocument Insert(System.String collectionName, MongoDB.Bson.BsonDocument document)
- Task<MongoDB.Bson.BsonDocument> InsertAsync<T>(T model, System.Threading.CancellationToken cancellationToken = default)
- System.Threading.Tasks.Task InsertAsync<T>(IEnumerable<T> models, System.Threading.CancellationToken cancellationToken = default)
- Task<MongoDB.Bson.BsonDocument> InsertAsync(System.String collectionName, MongoDB.Bson.BsonDocument document, System.Threading.CancellationToken cancellationToken = default)
- System.Int32 Update<T>(MongoDB.Bson.ObjectId id, T model)
- System.Int32 Update(System.String collectionName, MongoDB.Bson.ObjectId id, MongoDB.Bson.BsonDocument document)
- Task<System.Int32> UpdateAsync<T>(MongoDB.Bson.ObjectId id, T model, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int32> UpdateAsync(System.String collectionName, MongoDB.Bson.ObjectId id, MongoDB.Bson.BsonDocument document, System.Threading.CancellationToken cancellationToken = default)
- Task<System.Int32> UpdateAsync<T>(IEnumerable<T> models, Func<T, System.Object> keyGetter, System.Threading.CancellationToken cancellationToken = default)

### Nerosoft.Euonia.Repository.Mongo.MongoDbOptions

**类型**：class

- new Nerosoft.Euonia.Repository.Mongo.MongoDbOptions()
- System.String ConnectionString { get; set; }
- System.String Database { get; set; }
- Nerosoft.Euonia.Repository.Mongo.MongoDbOptions UseConnection(System.String connection)
- Nerosoft.Euonia.Repository.Mongo.MongoDbOptions UseDatabase(System.String database)

### MongoRepository<TContext, TEntity, TKey>

**类型**：class
基类 Repository<TContext, TEntity, TKey>；接口 System.IDisposable、IRepository<TContext, TEntity, TKey>、IRepository<TEntity, TKey>、IRepository<TEntity>

- new MongoRepository<TContext, TEntity, TKey>(Nerosoft.Euonia.Repository.IContextProvider provider)
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

### Nerosoft.Euonia.Repository.Mongo.MongoTenantFilters

**类型**：static class

- static FilterDefinition<TEntity> AndTenant<TEntity>(FilterDefinition<TEntity> filter, System.String tenantId, System.Boolean ignore)
- static FilterDefinition<TEntity> For<TEntity>(System.String tenantId, System.Boolean ignore)

### MultiTenantMongoDataContextBase<TContext>

**类型**：class
基类 DataContextBase<TContext>；接口 Nerosoft.Euonia.Repository.IRepositoryContext、System.IDisposable、Nerosoft.Euonia.Tenancy.ITenantFilterState

- System.String CurrentTenantId { get; }
- System.Boolean IgnoreTenantFilter { get; }
- FilterDefinition<TEntity> ApplyTenant<TEntity>(FilterDefinition<TEntity> filter)
- System.String StampTenant<TEntity>(TEntity entity)
- FilterDefinition<TEntity> TenantFilter<TEntity>()

### Nerosoft.Euonia.Repository.Mongo.ObjectIdSerializer

**类型**：class
接口 MongoDB.Bson.Serialization.IBsonSerializer

- new Nerosoft.Euonia.Repository.Mongo.ObjectIdSerializer(System.Type valueType)
- System.Type ValueType { get; }
- System.Object Deserialize(MongoDB.Bson.Serialization.BsonDeserializationContext context, MongoDB.Bson.Serialization.BsonDeserializationArgs args)
- void Serialize(MongoDB.Bson.Serialization.BsonSerializationContext context, MongoDB.Bson.Serialization.BsonSerializationArgs args, System.Object value)

### ObjectIdSerializer<T>

**类型**：class
基类 Nerosoft.Euonia.Repository.Mongo.ObjectIdSerializer；接口 MongoDB.Bson.Serialization.IBsonSerializer、IBsonSerializer<T>

- new ObjectIdSerializer<T>()
- T Deserialize(MongoDB.Bson.Serialization.BsonDeserializationContext context, MongoDB.Bson.Serialization.BsonDeserializationArgs args)
- void Serialize(MongoDB.Bson.Serialization.BsonSerializationContext context, MongoDB.Bson.Serialization.BsonSerializationArgs args, T value)

### Nerosoft.Euonia.Repository.Mongo.SequentialGuidValueGenerator

**类型**：class
接口 MongoDB.Bson.Serialization.IIdGenerator、IValueGenerator<System.Guid>、Nerosoft.Euonia.Repository.Mongo.IValueGenerator

- new Nerosoft.Euonia.Repository.Mongo.SequentialGuidValueGenerator()
- System.Guid Generate()
- System.Object GenerateId(System.Object container, System.Object document)
- System.Boolean IsEmpty(System.Object id)

### Nerosoft.Euonia.Repository.Mongo.SnowflakeIdValueGenerator

**类型**：class
接口 MongoDB.Bson.Serialization.IIdGenerator、IValueGenerator<System.Int64>、Nerosoft.Euonia.Repository.Mongo.IValueGenerator

- new Nerosoft.Euonia.Repository.Mongo.SnowflakeIdValueGenerator()
- System.Int64 Generate()
- System.Object GenerateId(System.Object container, System.Object document)
- System.Boolean IsEmpty(System.Object id)
