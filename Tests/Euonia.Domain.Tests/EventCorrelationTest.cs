using Nerosoft.Euonia.Domain;

namespace Nerosoft.Euonia.Domain.Tests;

public class ProductAddedEvent : DomainEvent
{
}

public class EventCorrelationTest
{
	[Fact]
	public void Test_CorrelationId_DefaultsToEventId()
	{
		var @event = new ProductAddedEvent();

		Assert.False(string.IsNullOrEmpty(@event.EventId));
		Assert.Equal(@event.EventId, @event.CorrelationId);
	}

	[Fact]
	public void Test_CorrelationId_CanBeSet()
	{
		const string correlation = "x-abc-123";
		var @event = new ProductAddedEvent { CorrelationId = correlation };

		Assert.Equal(correlation, @event.CorrelationId);
	}

	[Fact]
	public void Test_EventAggregate_PropagatesCorrelationId()
	{
		const string correlation = "x-agg-999";
		var @event = new ProductAddedEvent { CorrelationId = correlation };

		var eventAggregate = @event.GetEventAggregate();

		Assert.Equal(correlation, eventAggregate.CorrelationId);
	}
}