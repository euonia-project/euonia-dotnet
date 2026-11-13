using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

/// <summary>
/// 测试用领域事件，表示目录项已被创建。
/// </summary>
public class ItemCreatedEvent : DomainEvent
{
	/// <summary>
	/// 初始化 <see cref="ItemCreatedEvent"/> 并记录新目录项的名称。
	/// </summary>
	/// <param name="name">新目录项的名称。</param>
	public ItemCreatedEvent(string name)
	{
		Name = name;
	}

	/// <summary>
	/// 获取目录项创建时的名称。
	/// </summary>
	public string Name { get; }
}

/// <summary>
/// 测试用领域事件，表示目录项已被重命名。
/// </summary>
public class ItemRenamedEvent : DomainEvent
{
	/// <summary>
	/// 初始化 <see cref="ItemRenamedEvent"/> 并记录重命名后的名称。
	/// </summary>
	/// <param name="name">重命名后的名称。</param>
	public ItemRenamedEvent(string name)
	{
		Name = name;
	}

	/// <summary>
	/// 获取重命名后的名称。
	/// </summary>
	public string Name { get; }
}

/// <summary>
/// 测试用聚合根，通过维护名称列表验证事件注册、状态变更与历史重放。
/// </summary>
public class CatalogAggregate : Aggregate<Guid>
{
	private readonly List<string> _names = [];

	/// <summary>
	/// 初始化聚合根并注册目录项创建与重命名事件的处理程序。
	/// </summary>
	public CatalogAggregate()
	{
		Register<ItemCreatedEvent>(e => _names.Add(e.Name));
		Register<ItemRenamedEvent>(e =>
		{
			_names.Clear();
			_names.Add(e.Name);
		});
	}

	/// <summary>
	/// 触发目录项创建事件，从而把名称加入聚合状态。
	/// </summary>
	/// <param name="name">新目录项的名称。</param>
	public void Create(string name)
	{
		RaiseEvent(new ItemCreatedEvent(name));
	}

	/// <summary>
	/// 触发目录项重命名事件，以新名称替换聚合状态中已有的名称。
	/// </summary>
	/// <param name="name">重命名后的名称。</param>
	public void Rename(string name)
	{
		RaiseEvent(new ItemRenamedEvent(name));
	}

	/// <summary>
	/// 获取当前聚合状态中的名称列表（只读）。
	/// </summary>
	public IReadOnlyList<string> Names => _names.AsReadOnly();
}

/// <summary>
/// 验证 <see cref="Aggregate{T}"/> 的事件收集、清空与历史重放行为。
/// </summary>
public class AggregatePatternTest
{
	/// <summary>
	/// 验证 <c>GetAndClearEvents</c> 返回全部待处理事件，并在调用后清空事件集合、<c>HasEvents</c> 与事件计数。
	/// </summary>
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

	/// <summary>
	/// 验证没有待处理事件时 <c>GetAndClearEvents</c> 返回空集合，且事件集合保持为空。
	/// </summary>
	[Fact]
	public void Test_GetAndClearEvents_Empty_WhenNonePending()
	{
		var aggregate = new CatalogAggregate();

		var events = aggregate.GetAndClearEvents();

		Assert.Empty(events);
		Assert.Empty(aggregate.GetEvents());
	}

	/// <summary>
	/// 验证 <c>LoadFromHistory</c> 会依次应用事件处理程序重建状态，且不会把历史事件记录为待处理事件。
	/// </summary>
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

	/// <summary>
	/// 验证 <c>HasEvents</c> 与 <c>EventsCount</c> 能随事件产生和调用 <c>ClearEvents</c> 正确变化。
	/// </summary>
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