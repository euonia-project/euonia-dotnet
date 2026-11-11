using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Nerosoft.Euonia.MultiTenancy;
using Nerosoft.Euonia.Repository.Mongo;

namespace Nerosoft.Euonia.Repository.Tests.Tenancy;

/// <summary>
/// MongoDB 租户过滤器：字段名、空租户 fail-closed、全租户跳过。
/// </summary>
public class MongoTenantFilterTests
{
	/// <summary>
	/// 租户过滤器渲染为 <c>TenantId</c> 等值条件。
	/// </summary>
	[Fact]
	public void For_ShouldRenderTenantEquality()
	{
		var filter = MongoTenantFilters.For<MongoOrder>("t1", ignore: false);
		var document = Render(filter);

		Assert.Equal("t1", document["TenantId"].AsString);
	}

	/// <summary>
	/// 无租户时过滤到不存在的值，保证 fail-closed。
	/// </summary>
	[Fact]
	public void For_WithoutTenant_ShouldFailClosed()
	{
		var document = Render(MongoTenantFilters.For<MongoOrder>(null, ignore: false));

		Assert.Equal("__no_tenant__", document["TenantId"].AsString);
	}

	/// <summary>
	/// 全租户跨租户模式跳过过滤。
	/// </summary>
	[Fact]
	public void For_AllTenantScope_ShouldReturnEmpty()
	{
		var filter = MongoTenantFilters.For<MongoOrder>(null, ignore: true);

		Assert.Equal(FilterDefinition<MongoOrder>.Empty, filter);
	}

	/// <summary>
	/// 组合过滤器同时保留业务条件与租户条件。
	/// </summary>
	[Fact]
	public void AndTenant_ShouldCombineFilters()
	{
		var business = Builders<MongoOrder>.Filter.Eq(x => x.Name, "order");
		var document = Render(MongoTenantFilters.AndTenant(business, "t1", ignore: false));

		Assert.Equal("order", document["Name"].AsString);
		Assert.Equal("t1", document["TenantId"].AsString);
	}

	private static MongoDB.Bson.BsonDocument Render(FilterDefinition<MongoOrder> filter)
	{
		var serializer = BsonSerializer.SerializerRegistry.GetSerializer<MongoOrder>();
		return filter.Render(new RenderArgs<MongoOrder>(serializer, BsonSerializer.SerializerRegistry));
	}

	/// <summary>
	/// Mongo 测试实体。
	/// </summary>
	public sealed class MongoOrder : IMultiTenant
	{
		/// <summary>
		/// 获取或设置名称。
		/// </summary>
		public string Name { get; set; } = null!;

		/// <summary>
		/// 获取或设置租户标识。
		/// </summary>
		public string TenantId { get; set; } = null!;
	}
}
