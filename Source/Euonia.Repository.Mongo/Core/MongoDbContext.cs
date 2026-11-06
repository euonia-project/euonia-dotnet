using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// 抽象的 Mongo 数据库上下文。
/// </summary>
public abstract class MongoDbContext
{
    /// <summary>
    /// 获取 Mongo 数据库。
    /// </summary>
    protected IMongoDatabase Database { get; }

    private readonly ModelProfileContainer _container;

    // 必须允许驱动在插入时分配 Id：ModelBuilder 注册的 Id 生成器只会在此开关为 true 时生效。
    // 手动赋值的 Id 不会被覆盖，驱动仅在 Id 为默认值时生成。
    private readonly MongoCollectionSettings _collectionSettings = new() { AssignIdOnInsert = true };

    /// <summary>
    /// 初始化 <see cref="MongoDbContext"/> 类的新实例。
    /// </summary>
    /// <param name="database">该上下文使用的 Mongo 数据库实例，所有集合操作均基于此数据库。</param>
    /// <remarks>
    /// 构造时按当前上下文的运行时类型获取模型配置，<see cref="OnModelCreating"/> 对每个上下文类型只执行一次。
    /// </remarks>
    protected MongoDbContext(IMongoDatabase database)
    {
        _container = ModelProfileContainer.GetInstance(GetType(), OnModelCreating);
        Database = database;
    }

    /// <summary>
    /// 构建模型配置的逻辑。
    /// </summary>
    /// <param name="builder">用于注册集合名称、主键与属性映射的模型构建器。</param>
    /// <remarks>派生类重写此方法以配置模型；默认实现不添加任何配置。</remarks>
    protected virtual void OnModelCreating(ModelBuilder builder)
    {
    }

    /// <summary>
    /// 获取指定类型的 Mongo 集合。
    /// </summary>
    /// <typeparam name="T">集合的文档类型，必须为引用类型。</typeparam>
    /// <returns>集合名称取自 <typeparamref name="T"/> 的模型配置，未配置时使用类型名；该集合启用了插入时自动分配 Id。</returns>
    public IMongoCollection<T> Collection<T>()
        where T : class
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        return Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
    }

    /// <summary>
    /// 获取指定类型的 Mongo 集合。
    /// </summary>
    /// <param name="type">要获取集合的模型类型，其集合名取自该类型的模型配置，未配置时使用类型名。</param>
    /// <returns>以 <see cref="BsonDocument"/> 为文档类型的集合，启用了插入时自动分配 Id。</returns>
    public IMongoCollection<BsonDocument> Collection(Type type)
    {
        var profile = _container.GetProfile(type);
        return Database.GetCollection<BsonDocument>(profile.CollectionName ?? type.Name, _collectionSettings);
    }

    /// <summary>
    /// 按 Id 获取指定类型的单个模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="id">目标文档的主键值。</param>
    /// <returns>匹配的模型；未找到时返回 <typeparamref name="T"/> 的默认值，即引用类型为 <see langword="null"/>。</returns>
    public T Find<T>(MongoDB.Bson.ObjectId id)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name);
        var filter = Builders<T>.Filter.Eq("_id", id);
        return collection.FindSync(filter).FirstOrDefault();
    }

    /// <summary>
    /// 按 Id 获取指定类型的单个模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="id">目标文档的主键值。</param>
    /// <param name="cancellationToken">用于取消查询的取消令牌。</param>
    /// <returns>表示异步查询的任务；结果为匹配的模型，未找到时为 <typeparamref name="T"/> 的默认值。</returns>
    public async Task<T> FindAsync<T>(MongoDB.Bson.ObjectId id, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        var filter = Builders<T>.Filter.Eq("_id", id);
        var query = await collection.FindAsync(filter, cancellationToken: cancellationToken);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// 按筛选器获取指定类型的模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="filter">用于筛选文档的表达式，由驱动翻译为 Mongo 查询条件。</param>
    /// <param name="options">查询选项，如排序、分页、投影等。</param>
    /// <param name="cancellationToken">用于取消查询的取消令牌。</param>
    /// <returns>表示异步查询的任务；结果为可逐条读取匹配文档的游标，需由调用方释放。</returns>
    public async Task<IAsyncCursor<T>> FindAsync<T>(Expression<Func<T, bool>> filter, FindOptions<T> options, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        var query = await collection.FindAsync(filter, options, cancellationToken: cancellationToken);
        return query;
    }

    /// <summary>
    /// 按筛选器获取指定类型的模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="filter">预先构造的筛选定义，可组合多个查询条件。</param>
    /// <param name="options">查询选项，如排序、分页、投影等。</param>
    /// <param name="cancellationToken">用于取消查询的取消令牌。</param>
    /// <returns>表示异步查询的任务；结果为可逐条读取匹配文档的游标，需由调用方释放。</returns>
    public async Task<IAsyncCursor<T>> FindAsync<T>(FilterDefinition<T> filter, FindOptions<T> options, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        return await collection.FindAsync(filter, options, cancellationToken);
    }

    // /// <summary>
    // /// Gets model of specified type by filter.
    // /// </summary>
    // /// <param name="filter"></param>
    // /// <param name="options"></param>
    // /// <param name="cancellationToken"></param>
    // /// <typeparam name="T"></typeparam>
    // /// <returns></returns>
    // public async Task<IAsyncCursor<T>> FindAsync<T>(QueryDocument filter, FindOptions<T> options, CancellationToken cancellationToken = default)
    // {
    //     var type = typeof(T);
    //     var profile = _container.GetProfile<T>();
    //     var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
    //     return await collection.FindAsync(filter, options, cancellationToken);
    // }

    /// <summary>
    /// 按筛选器查找指定类型的模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="filter">用于筛选文档的表达式，由驱动翻译为 Mongo 查询条件。</param>
    /// <returns>尚未执行的查询对象，可继续追加排序、分页、投影等操作。</returns>
    public IFindFluent<T, T> Find<T>(Expression<Func<T, bool>> filter)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        return collection.Find(filter);
    }

    /// <summary>
    /// 按筛选器查找指定类型的模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="filter">预先构造的筛选定义，可组合多个查询条件。</param>
    /// <returns>尚未执行的查询对象，可继续追加排序、分页、投影等操作。</returns>
    public IFindFluent<T, T> Find<T>(FilterDefinition<T> filter)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        return collection.Find(filter);
    }

    /// <summary>
    /// 按 Id 查找指定集合中的文档。
    /// </summary>
    /// <param name="collectionName">目标集合名称，直接使用该名称，不读取模型配置。</param>
    /// <param name="id">目标文档的主键值。</param>
    /// <returns>匹配的文档；不存在时返回 <see langword="null"/>。</returns>
    public BsonDocument Find(string collectionName, MongoDB.Bson.ObjectId id)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        var query = collection.FindSync(filter);
        return query.FirstOrDefault();
    }

    /// <summary>
    /// 按 Id 异步查找指定集合中的文档。
    /// </summary>
    /// <param name="collectionName">目标集合名称，直接使用该名称，不读取模型配置。</param>
    /// <param name="id">目标文档的主键值。</param>
    /// <returns>表示异步查询的任务；结果为匹配的文档，不存在时为 <see langword="null"/>。</returns>
    public async Task<BsonDocument> FindAsync(string collectionName, MongoDB.Bson.ObjectId id)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        var query = await collection.FindAsync(filter);
        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// 插入单个模型。
    /// </summary>
    /// <typeparam name="T">模型类型，必须为引用类型。</typeparam>
    /// <param name="model">要插入的模型实例，将按其 BSON 序列化结果写入集合。</param>
    /// <returns>写入集合的 BSON 文档；若模型未提供主键，则由驱动生成并补充 <c>_id</c> 后写入。</returns>
    /// <remarks>集合名取自模型配置，未配置时使用类型名；是否跳过文档验证由模型的验证配置决定。</remarks>
    public BsonDocument Insert<T>(T model)
        where T : class
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<BsonDocument>(profile.CollectionName ?? type.Name, _collectionSettings);
        var document = model.ToBsonDocument();
        collection.InsertOne(document, new InsertOneOptions { BypassDocumentValidation = profile.BypassDocumentValidation });
        return document;
    }

    /// <summary>
    /// 插入单个模型。
    /// </summary>
    /// <typeparam name="T">模型类型，必须为引用类型。</typeparam>
    /// <param name="model">要插入的模型实例，将按其 BSON 序列化结果写入集合。</param>
    /// <param name="cancellationToken">用于取消插入操作的取消令牌。</param>
    /// <returns>表示异步插入的任务；结果为写入集合的 BSON 文档，若模型未提供主键则由驱动生成并补充 <c>_id</c>。</returns>
    public async Task<BsonDocument> InsertAsync<T>(T model, CancellationToken cancellationToken = default)
        where T : class
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<BsonDocument>(profile.CollectionName ?? type.Name, _collectionSettings);
        var document = model.ToBsonDocument();
        await collection.InsertOneAsync(document, new InsertOneOptions { BypassDocumentValidation = profile.BypassDocumentValidation }, cancellationToken);
        return document;
    }

    /// <summary>
    /// 插入多个模型。
    /// </summary>
    /// <typeparam name="T">模型类型，必须为引用类型。</typeparam>
    /// <param name="models">要插入的模型集合，会被逐个序列化后一次性批量写入。</param>
    /// <param name="cancellationToken">用于取消插入操作的取消令牌。</param>
    /// <remarks>集合名与文档验证设置取自模型配置；未提供主键的模型由驱动生成并补充 <c>_id</c>。</remarks>
    public async Task InsertAsync<T>(IEnumerable<T> models, CancellationToken cancellationToken = default)
        where T : class
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<BsonDocument>(profile.CollectionName ?? type.Name, _collectionSettings);
        var documents = models.Select(model => model.ToBsonDocument());
        await collection.InsertManyAsync(documents, new InsertManyOptions { BypassDocumentValidation = profile.BypassDocumentValidation }, cancellationToken);
    }

    /// <summary>
    /// 将文档插入指定集合。
    /// </summary>
    /// <param name="collectionName">目标集合名称，直接使用该名称，不读取模型配置。</param>
    /// <param name="document">要插入的 BSON 文档。</param>
    /// <returns>写入集合的文档；若原本不含 <c>_id</c>，驱动会生成并补充到该文档中。</returns>
    /// <remarks>插入时不跳过文档验证；由于启用了插入时自动分配 Id，传入的 <paramref name="document"/> 会因此被修改。</remarks>
    public BsonDocument Insert(string collectionName, BsonDocument document)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        collection.InsertOne(document);
        return document;
    }

    /// <summary>
    /// 将文档插入指定集合。
    /// </summary>
    /// <param name="collectionName">目标集合名称，直接使用该名称，不读取模型配置。</param>
    /// <param name="document">要插入的 BSON 文档。</param>
    /// <param name="cancellationToken">用于取消插入操作的取消令牌。</param>
    /// <returns>表示异步插入的任务；结果为写入集合的文档，若原本不含 <c>_id</c>，驱动会生成并补充到该文档中。</returns>
    /// <remarks>插入时跳过文档验证；由于启用了插入时自动分配 Id，传入的 <paramref name="document"/> 会因此被修改。</remarks>
    public async Task<BsonDocument> InsertAsync(string collectionName, BsonDocument document, CancellationToken cancellationToken = default)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        await collection.InsertOneAsync(document, new InsertOneOptions { BypassDocumentValidation = true }, cancellationToken);
        return document;
    }

    /// <summary>
    /// 更新单个模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="id">目标文档的主键值，用于构造匹配 <c>_id</c> 的过滤器。</param>
    /// <param name="model">提供新字段值的模型实例，其序列化结果中除 <c>_id</c> 外的字段都会以 <c>$set</c> 写入。</param>
    /// <returns>实际被修改的文档数，0 表示未找到匹配文档或字段值未发生变化。</returns>
    /// <remarks><c>_id</c> 为不可变字段，因此模型中的 <c>_id</c> 不会参与更新。</remarks>
    public int Update<T>(MongoDB.Bson.ObjectId id, T model)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name);
        var filter = Builders<T>.Filter.Eq("_id", id);
        var document = model.ToBsonDocument();
        // _id 是不可变字段，不能出现在 $set 中；其值已用于构造过滤器。
        document.Remove("_id");
        var result = collection.UpdateOne(filter, new BsonDocument("$set", document));
        return (int)result.ModifiedCount;
    }

    /// <summary>
    /// 更新单个模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="id">目标文档的主键值，用于构造匹配 <c>_id</c> 的过滤器。</param>
    /// <param name="model">提供新字段值的模型实例，其序列化结果中除 <c>_id</c> 外的字段都会以 <c>$set</c> 写入。</param>
    /// <param name="cancellationToken">用于取消更新操作的取消令牌。</param>
    /// <returns>表示异步更新的任务；结果为实际被修改的文档数，0 表示未找到匹配文档或字段值未发生变化。</returns>
    /// <remarks><c>_id</c> 为不可变字段，因此模型中的 <c>_id</c> 不会参与更新。</remarks>
    public async Task<int> UpdateAsync<T>(MongoDB.Bson.ObjectId id, T model, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        var filter = Builders<T>.Filter.Eq("_id", id);
        var document = model.ToBsonDocument();
        // _id 是不可变字段，不能出现在 $set 中；其值已用于构造过滤器。
        document.Remove("_id");
        var result = await collection.UpdateOneAsync(filter, new BsonDocument("$set", document), cancellationToken: cancellationToken);
        return (int)result.ModifiedCount;
    }

    /// <summary>
    /// 更新指定文档。
    /// </summary>
    /// <param name="collectionName">目标集合名称，直接使用该名称，不读取模型配置。</param>
    /// <param name="id">目标文档的主键值，用于构造匹配 <c>_id</c> 的过滤器。</param>
    /// <param name="document">提供新字段值的 BSON 文档，其中的 <c>_id</c> 会在更新前被移除。</param>
    /// <returns>实际被修改的文档数，0 表示未找到匹配文档或字段值未发生变化。</returns>
    /// <remarks>传入的 <paramref name="document"/> 会因移除 <c>_id</c> 而被直接修改，其余字段以 <c>$set</c> 写入。</remarks>
    public int Update(string collectionName, MongoDB.Bson.ObjectId id, BsonDocument document)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        document.Remove("_id");
        var result = collection.UpdateOne(filter, new BsonDocument("$set", document));
        return (int)result.ModifiedCount;
    }

    /// <summary>
    /// 更新指定文档。
    /// </summary>
    /// <param name="collectionName">目标集合名称，直接使用该名称，不读取模型配置。</param>
    /// <param name="id">目标文档的主键值，用于构造匹配 <c>_id</c> 的过滤器。</param>
    /// <param name="document">提供新字段值的 BSON 文档，其中的 <c>_id</c> 会在更新前被移除。</param>
    /// <param name="cancellationToken">用于取消更新操作的取消令牌。</param>
    /// <returns>表示异步更新的任务；结果为实际被修改的文档数，0 表示未找到匹配文档或字段值未发生变化。</returns>
    /// <remarks>传入的 <paramref name="document"/> 会因移除 <c>_id</c> 而被直接修改，其余字段以 <c>$set</c> 写入。</remarks>
    public async Task<int> UpdateAsync(string collectionName, MongoDB.Bson.ObjectId id, BsonDocument document, CancellationToken cancellationToken = default)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        document.Remove("_id");
        var result = await collection.UpdateOneAsync(filter, new BsonDocument("$set", document), cancellationToken: cancellationToken);
        return (int)result.ModifiedCount;
    }

    /// <summary>
    /// 更新多个模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="models">待更新的模型集合，会逐个执行更新。</param>
    /// <param name="keyGetter">当模型序列化结果不含 <c>_id</c> 时，用于获取主键值的委托。</param>
    /// <param name="cancellationToken">用于取消更新操作的取消令牌。</param>
    /// <returns>所有模型累计被修改的文档数。</returns>
    /// <remarks>过滤器优先使用模型序列化结果中的 <c>_id</c>，缺失时才使用 <paramref name="keyGetter"/> 的结果；每个模型单独发起一次更新请求。</remarks>
    public async Task<int> UpdateAsync<T>(IEnumerable<T> models, Func<T, object> keyGetter, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<BsonDocument>(profile.CollectionName ?? type.Name, _collectionSettings);

        var count = 0;
        foreach (var model in models)
        {
            var document = model.ToBsonDocument();

            // 以序列化后的 _id 构造过滤器：调用方传入的 key 可能是 string，而库中存的是 ObjectId。
            var id = document.TryGetValue("_id", out var idValue) ? idValue : BsonValue.Create(keyGetter(model));
            document.Remove("_id");

            var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
            var result = await collection.UpdateOneAsync(filter, new BsonDocument("$set", document), cancellationToken: cancellationToken);
            count += (int)result.ModifiedCount;
        }

        return count;
    }

    /// <summary>
    /// 删除单个模型。
    /// </summary>
    /// <typeparam name="T">模型类型，用于解析集合名称。</typeparam>
    /// <param name="id">要删除的文档的主键值。</param>
    /// <param name="cancellationToken">用于取消删除操作的取消令牌。</param>
    /// <returns>表示异步删除的任务；结果为被删除的文档数，0 表示未找到匹配文档。</returns>
    public async Task<int> DeleteAsync<T>(MongoDB.Bson.ObjectId id, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<BsonDocument>(profile.CollectionName ?? type.Name, _collectionSettings);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        var result = await collection.DeleteOneAsync(filter: filter, cancellationToken: cancellationToken);
        return (int)result.DeletedCount;
    }

    /// <summary>
    /// 删除所有匹配筛选器的模型。
    /// </summary>
    /// <typeparam name="T">模型类型。</typeparam>
    /// <param name="filter">用于筛选待删除文档的表达式，由驱动翻译为 Mongo 查询条件。</param>
    /// <param name="cancellationToken">用于取消删除操作的取消令牌。</param>
    /// <returns>表示异步删除的任务；结果为被删除的文档总数。</returns>
    public async Task<int> DeleteAsync<T>(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        var result = await collection.DeleteManyAsync(filter, null, cancellationToken);
        return (int)result.DeletedCount;
    }
}