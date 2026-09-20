using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

public class Product : Entity<Guid>
{
	public Product()
	{
	}

	public Product(Guid id)
	{
		Id = id;
	}

	public string Name { get; set; }
}

public class Category : Entity<Guid>
{
	public Category()
	{
	}

	public Category(Guid id)
	{
		Id = id;
	}
}

public class InventoryAggregate : Aggregate<Guid>
{
	private readonly List<string> _applied = [];

	public InventoryAggregate()
	{
		Register<ItemCreatedEvent>(e => _applied.Add("A:" + e.Name));
		Register<ItemCreatedEvent>(e => _applied.Add("B:" + e.Name));
	}

	public void CreateItem(string name)
	{
		RaiseEvent(new ItemCreatedEvent(name));
		RaiseEvent(new ItemRenamedEvent(name));
	}

	public IReadOnlyList<string> Applied => _applied.AsReadOnly();
}

public class PrimitivesFeatureTest
{
	[Fact]
	public void Test_ValueObject_Implements_IValueObject_Marker()
	{
		var descriptor = new PersonDescriptor { Name = "Alice", Age = 42 };

		Assert.IsAssignableFrom<IValueObject>(descriptor);
	}

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

	[Fact]
	public void Test_Entity_DifferentId_AreNotEqual()
	{
		var first = new Product(Guid.NewGuid());
		var second = new Product(Guid.NewGuid());

		Assert.NotEqual(first, second);
		Assert.False(first == second);
		Assert.True(first != second);
	}

	[Fact]
	public void Test_Entity_DifferentType_SameId_AreNotEqual()
	{
		var id = Guid.NewGuid();
		var product = new Product(id);
		var category = new Category(id);

		Assert.False(product.Equals(category));
		Assert.False(product == category);
	}

	[Fact]
	public void Test_Entity_IsTransient_ReflectsDefaultId()
	{
		var transient = new Product();
		var persisted = new Product(Guid.NewGuid());

		Assert.True(transient.IsTransient());
		Assert.False(persisted.IsTransient());
	}

	[Fact]
	public void Test_Aggregate_Register_IsIdempotent_LastWins()
	{
		var aggregate = new InventoryAggregate();

		aggregate.CreateItem("sprocket");

		Assert.Equal(new[] { "B:sprocket" }, aggregate.Applied);
	}

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