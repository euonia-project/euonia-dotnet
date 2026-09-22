using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

public class ItemCreatedEvent : DomainEvent
{
	public ItemCreatedEvent(string name)
	{
		Name = name;
	}

	public string Name { get; }
}

public class ItemRenamedEvent : DomainEvent
{
	public ItemRenamedEvent(string name)
	{
		Name = name;
	}

	public string Name { get; }
}

public class CatalogAggregate : Aggregate<Guid>
{
	private readonly List<string> _names = [];

	public CatalogAggregate()
	{
		Register<ItemCreatedEvent>(e => _names.Add(e.Name));
		Register<ItemRenamedEvent>(e =>
		{
			_names.Clear();
			_names.Add(e.Name);
		});
	}

	public void Create(string name)
	{
		RaiseEvent(new ItemCreatedEvent(name));
	}

	public void Rename(string name)
	{
		RaiseEvent(new ItemRenamedEvent(name));
	}

	public IReadOnlyList<string> Names => _names.AsReadOnly();
}

public class AggregatePatternTest
{
	[Fact]
	public void Test_GetAndClearEvents_ReturnsPending_ThenClears()
	{
		var aggregate = new CatalogAggregate();
		aggregate.Create("alpha");
		aggregate.Rename("beta");

		var events = aggregate.GetAndClearEvents();

		Assert.Equal(2, events.Count);
		Assert.Empty(aggregate.GetEvents());
		Assert.False(aggregate.HasEvents);
		Assert.Equal(0, aggregate.EventsCount);
	}

	[Fact]
	public void Test_GetAndClearEvents_Empty_WhenNonePending()
	{
		var aggregate = new CatalogAggregate();

		var events = aggregate.GetAndClearEvents();

		Assert.Empty(events);
		Assert.Empty(aggregate.GetEvents());
	}

	[Fact]
	public void Test_LoadFromHistory_AppliesHandlers_WithoutRecording()
	{
		var aggregate = new CatalogAggregate();

		aggregate.LoadFromHistory(
		[
			new ItemCreatedEvent("alpha"),
			new ItemRenamedEvent("beta"),
		]);

		Assert.Equal(new[] { "beta" }, aggregate.Names);
		Assert.Empty(aggregate.GetEvents());
		Assert.False(aggregate.HasEvents);
	}

	[Fact]
	public void Test_HasEvents_ReflectsPendingEvents()
	{
		var aggregate = new CatalogAggregate();

		Assert.False(aggregate.HasEvents);

		aggregate.Create("alpha");

		Assert.True(aggregate.HasEvents);
		Assert.Equal(1, aggregate.EventsCount);

		aggregate.ClearEvents();

		Assert.False(aggregate.HasEvents);
		Assert.Equal(0, aggregate.EventsCount);
	}
}