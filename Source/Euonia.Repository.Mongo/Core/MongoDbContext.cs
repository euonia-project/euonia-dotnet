using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Nerosoft.Euonia.Repository.Mongo;

/// <summary>
/// The abstract mongo database context.
/// </summary>
public abstract class MongoDbContext
{
    /// <summary>
    /// Gets the mongo database.
    /// </summary>
    protected IMongoDatabase Database { get; }

    private readonly ModelProfileContainer _container;

    // 必须允许驱动在插入时分配 Id：ModelBuilder 注册的 Id 生成器只会在此开关为 true 时生效。
    // 手动赋值的 Id 不会被覆盖，驱动仅在 Id 为默认值时生成。
    private readonly MongoCollectionSettings _collectionSettings = new() { AssignIdOnInsert = true };

    /// <summary>
    /// Initializes a new instance of the <see cref="MongoDbContext"/> class.
    /// </summary>
    /// <param name="database"></param>
    protected MongoDbContext(IMongoDatabase database)
    {
        _container = ModelProfileContainer.GetInstance(GetType(), OnModelCreating);
        Database = database;
    }

    /// <summary>
    /// The logic to build model profile.
    /// </summary>
    /// <param name="builder"></param>
    protected virtual void OnModelCreating(ModelBuilder builder)
    {
    }

    /// <summary>
    /// Gets the mongo collection of specified type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IMongoCollection<T> Collection<T>()
        where T : class
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        return Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
    }

    /// <summary>
    /// Gets the mongo collection of specified type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public IMongoCollection<BsonDocument> Collection(Type type)
    {
        var profile = _container.GetProfile(type);
        return Database.GetCollection<BsonDocument>(profile.CollectionName ?? type.Name, _collectionSettings);
    }

    /// <summary>
    /// Gets a single model of specified type by id.
    /// </summary>
    /// <param name="id"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public T Find<T>(MongoDB.Bson.ObjectId id)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name);
        var filter = Builders<T>.Filter.Eq("_id", id);
        return collection.FindSync(filter).FirstOrDefault();
    }

    /// <summary>
    /// Gets a single model of specified type by id.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// Gets models of specified type by filter.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<IAsyncCursor<T>> FindAsync<T>(Expression<Func<T, bool>> filter, FindOptions<T> options, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        var query = await collection.FindAsync(filter, options, cancellationToken: cancellationToken);
        return query;
    }

    /// <summary>
    /// Gets models of specified type by filter.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// Finds models of specified type by filter.
    /// </summary>
    /// <param name="filter"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IFindFluent<T, T> Find<T>(Expression<Func<T, bool>> filter)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        return collection.Find(filter);
    }

    /// <summary>
    /// Finds models of specified type by filter.
    /// </summary>
    /// <param name="filter"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public IFindFluent<T, T> Find<T>(FilterDefinition<T> filter)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        return collection.Find(filter);
    }

    /// <summary>
    /// Finds documents of specified type by filter.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public BsonDocument Find(string collectionName, MongoDB.Bson.ObjectId id)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        var query = collection.FindSync(filter);
        return query.FirstOrDefault();
    }

    /// <summary>
    /// Finds documents of specified type by filter.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<BsonDocument> FindAsync(string collectionName, MongoDB.Bson.ObjectId id)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        var query = await collection.FindAsync(filter);
        return await query.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Inserts a single model.
    /// </summary>
    /// <param name="model"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// Inserts a single model.
    /// </summary>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// Inserts multiple models.
    /// </summary>
    /// <param name="models"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
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
    /// Inserts document to specified collection.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    public BsonDocument Insert(string collectionName, BsonDocument document)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        collection.InsertOne(document);
        return document;
    }

    /// <summary>
    /// Inserts document to specified collection.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="document"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<BsonDocument> InsertAsync(string collectionName, BsonDocument document, CancellationToken cancellationToken = default)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        await collection.InsertOneAsync(document, new InsertOneOptions { BypassDocumentValidation = true }, cancellationToken);
        return document;
    }

    /// <summary>
    /// Updates a single model.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// Updates a single model.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="model"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// Updates specified document.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="id"></param>
    /// <param name="document"></param>
    /// <returns></returns>
    public int Update(string collectionName, MongoDB.Bson.ObjectId id, BsonDocument document)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        document.Remove("_id");
        var result = collection.UpdateOne(filter, new BsonDocument("$set", document));
        return (int)result.ModifiedCount;
    }

    /// <summary>
    /// Updates specified document.
    /// </summary>
    /// <param name="collectionName"></param>
    /// <param name="id"></param>
    /// <param name="document"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> UpdateAsync(string collectionName, MongoDB.Bson.ObjectId id, BsonDocument document, CancellationToken cancellationToken = default)
    {
        var collection = Database.GetCollection<BsonDocument>(collectionName, _collectionSettings);
        var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
        document.Remove("_id");
        var result = await collection.UpdateOneAsync(filter, new BsonDocument("$set", document), cancellationToken: cancellationToken);
        return (int)result.ModifiedCount;
    }

    /// <summary>
    /// Updates models.
    /// </summary>
    /// <param name="models"></param>
    /// <param name="keyGetter"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// Deletes a single model.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
    /// Deletes models by filter.
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async Task<int> DeleteAsync<T>(Expression<Func<T, bool>> filter, CancellationToken cancellationToken = default)
    {
        var type = typeof(T);
        var profile = _container.GetProfile<T>();
        var collection = Database.GetCollection<T>(profile.CollectionName ?? type.Name, _collectionSettings);
        var result = await collection.DeleteManyAsync(filter, null, cancellationToken);
        return (int)result.DeletedCount;
    }
}