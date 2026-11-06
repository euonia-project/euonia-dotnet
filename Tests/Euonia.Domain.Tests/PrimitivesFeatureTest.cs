using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

/// <summary>
/// 测试用实体，表示拥有名称的商品。
/// </summary>
public class Product : Entity<Guid>
{
	/// <summary>
	/// 初始化未分配标识的商品实例，此时实体处于瞬态。
	/// </summary>
	public Product()
	{
	}

	/// <summary>
	/// 使用指定标识初始化商品实体。
	/// </summary>
	/// <param name="id">商品实体的标识。</param>
	public Product(Guid id)
	{
		Id = id;
	}

	/// <summary>
	/// 获取或设置商品名称。
	/// </summary>
	public string Name { get; set; } = string.Empty;
}

/// <summary>
/// 测试用实体，用于验证类型不同但标识相同的实体不相等。
/// </summary>
public class Category : Entity<Guid>
{
	/// <summary>
	/// 初始化未分配标识的类别实例。
	/// </summary>
	public Category()
	{
	}

	/// <summary>
	/// 使用指定标识初始化类别实体。
	/// </summary>
	/// <param name="id">类别实体的标识。</param>
	public Category(Guid id)
	{
		Id = id;
	}
}

/// <summary>
/// 测试用聚合根，用于验证同一事件类型的重复注册行为及按类型筛选事件。
/// </summary>
public class InventoryAggregate : Aggregate<Guid>
{
	private readonly List<string> _applied = [];

	/// <summary>
	/// 初始化聚合根并重复注册同一创建事件的处理程序，以验证后注册的处理程序生效。
	/// </summary>
	public InventoryAggregate()
	{
		Register<ItemCreatedEvent>(e => _applied.Add("A:" + e.Name));
		Register<ItemCreatedEvent>(e => _applied.Add("B:" + e.Name));
	}

	/// <summary>
	/// 触发创建与重命名事件，供聚合状态与事件筛选断言使用。
	/// </summary>
	/// <param name="name">目录项名称。</param>
	public void CreateItem(string name)
	{
		RaiseEvent(new ItemCreatedEvent(name));
		RaiseEvent(new ItemRenamedEvent(name));
	}

	/// <summary>
	/// 获取事件处理程序实际应用后的标记列表（只读）。
	/// </summary>
	public IReadOnlyList<string> Applied => _applied.AsReadOnly();
}

/// <summary>
/// 验证领域基元类型（值对象、实体与聚合根）的相等性、瞬态判断与事件处理行为。
/// </summary>
public class PrimitivesFeatureTest
{
	/// <summary>
	/// 验证值对象类型实现了 <see cref="IValueObject"/> 标记接口。
	/// </summary>
	[Fact]
	public void Test_ValueObject_Implements_IValueObject_Marker()
	{
		var descriptor = new PersonDescriptor { Name = "Alice", Age = 42 };

		Assert.IsAssignableFrom<IValueObject>(descriptor);
	}

	/// <summary>
	/// 验证同类型且标识相同的实体相等，名称等其他属性不影响相等性与哈希码。
	/// </summary>
	[Fact]
	public void Test_Entity_SameTypeSameId_AreEqual()
	{
		var id = Guid.NewGuid();
		var first = new Product(id) { Name = "alpha" };
		var second = new Product(id) { Name = "beta" };

		Assert.Equal(first, second);
		Assert.True(first == second);
		Assert.False(first != second);
		Assert.Equal(first.GetHashCode(), second.GetHashCode());
	}

	/// <summary>
	/// 验证同类型但标识不同的实体不相等。
	/// </summary>
	[Fact]
	public void Test_Entity_DifferentId_AreNotEqual()
	{
		var first = new Product(Guid.NewGuid());
		var second = new Product(Guid.NewGuid());

		Assert.NotEqual(first, second);
		Assert.False(first == second);
		Assert.True(first != second);
	}

	/// <summary>
	/// 验证标识相同但实体类型不同时不相等。
	/// </summary>
	[Fact]
	public void Test_Entity_DifferentType_SameId_AreNotEqual()
	{
		var id = Guid.NewGuid();
		var product = new Product(id);
		var category = new Category(id);

		Assert.False(product.Equals(category));
		Assert.False(product == category);
	}

	/// <summary>
	/// 验证 <c>IsTransient</c> 依据标识是否为默认值判断实体是否处于瞬态。
	/// </summary>
	[Fact]
	public void Test_Entity_IsTransient_ReflectsDefaultId()
	{
		var transient = new Product();
		var persisted = new Product(Guid.NewGuid());

		Assert.True(transient.IsTransient());
		Assert.False(persisted.IsTransient());
	}

	/// <summary>
	/// 验证重复注册同一事件类型时后注册的处理程序覆盖先前的，事件只被处理一次。
	/// </summary>
	[Fact]
	public void Test_Aggregate_Register_IsIdempotent_LastWins()
	{
		var aggregate = new InventoryAggregate();

		aggregate.CreateItem("sprocket");

		Assert.Equal(new[] { "B:sprocket" }, aggregate.Applied);
	}

	/// <summary>
	/// 验证 <c>GetEvents&lt;T&gt;</c> 按事件类型筛选，而未指定类型时返回全部待处理事件。
	/// </summary>
	[Fact]
	public void Test_Aggregate_GetEvents_OfType_Filters()
	{
		var aggregate = new InventoryAggregate();
		aggregate.CreateItem("sprocket");

		var created = aggregate.GetEvents<ItemCreatedEvent>();
		var renamed = aggregate.GetEvents<ItemRenamedEvent>();

		Assert.Single(created);
		Assert.IsType<ItemCreatedEvent>(created[0]);
		Assert.Single(renamed);
		Assert.IsType<ItemRenamedEvent>(renamed[0]);
		Assert.Equal(2, aggregate.GetEvents().Count);
	}
}