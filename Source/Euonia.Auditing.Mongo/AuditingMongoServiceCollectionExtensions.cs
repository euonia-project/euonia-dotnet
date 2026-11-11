using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using Nerosoft.Euonia.Auditing;
using Nerosoft.Euonia.Auditing.Mongo;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 审计 MongoDB 存储的注册扩展。
/// </summary>
public static class AuditingMongoServiceCollectionExtensions
{
	/// <summary>
	/// 注册 MongoDB 审计存储。
	/// </summary>
	/// <param name="services">服务集合。</param>
	/// <param name="database">审计库。</param>
	/// <param name="collectionName">集合名称。</param>
	/// <returns>注册完成后的服务集合。</returns>
	/// <remarks>宿主启动后应调用 <see cref="MongoAuditingStore.EnsureIndexesAsync"/> 创建唯一索引。</remarks>
	public static IServiceCollection AddAuditingMongo(
		this IServiceCollection services,
		IMongoDatabase database,
		string collectionName = MongoAuditingStore.DefaultCollectionName)
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(database);

		services.TryAddSingleton<IAuditingStore>(_ => new MongoAuditingStore(database, collectionName));
		return services;
	}
}
