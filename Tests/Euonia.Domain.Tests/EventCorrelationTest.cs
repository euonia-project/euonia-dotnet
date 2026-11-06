using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

/// <summary>
/// 测试用领域事件，用于验证事件标识与关联标识的默认值及传播行为。
/// </summary>
public class ProductAddedEvent : DomainEvent
{
}

/// <summary>
/// 验证 <see cref="DomainEvent"/> 的关联标识默认值与事件聚合根的传播行为。
/// </summary>
public class EventCorrelationTest
{
	/// <summary>
	/// 验证新建事件的 <c>EventId</c> 非空，且 <c>CorrelationId</c> 默认与其相同。
	/// </summary>
	[Fact]
	public void Test_CorrelationId_DefaultsToEventId()
	{
		var @event = new ProductAddedEvent();

		Assert.False(string.IsNullOrEmpty(@event.EventId));
		Assert.Equal(@event.EventId, @event.CorrelationId);
	}

	/// <summary>
	/// 验证可以显式设置 <c>CorrelationId</c>，且读取到的值与设置值一致。
	/// </summary>
	[Fact]
	public void Test_CorrelationId_CanBeSet()
	{
		const string correlation = "x-abc-123";
		var @event = new ProductAddedEvent { CorrelationId = correlation };

		Assert.Equal(correlation, @event.CorrelationId);
	}

	/// <summary>
	/// 验证 <c>GetEventAggregate</c> 返回的事件聚合根会携带原事件的 <c>CorrelationId</c>。
	/// </summary>
	[Fact]
	public void Test_EventAggregate_PropagatesCorrelationId()
	{
		const string correlation = "x-agg-999";
		var @event = new ProductAddedEvent { CorrelationId = correlation };

		var eventAggregate = @event.GetEventAggregate();

		Assert.Equal(correlation, eventAggregate.CorrelationId);
	}
}