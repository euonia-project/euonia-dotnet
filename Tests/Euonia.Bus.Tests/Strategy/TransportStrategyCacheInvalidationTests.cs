namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对传输策略判定缓存的测试。
/// </summary>
/// <remarks>
/// <see cref="BaseTransportStrategy"/> 为「通道 + 类型 → 是否允许传出/传入」的结果做了缓存。
/// 与约定缓存同理：策略集合发生变化后必须清空缓存，否则在首次判定之后
/// 再添加策略或重定义判定函数将**静默无效**。
/// </remarks>
public class TransportStrategyCacheInvalidationTests
{
	[Fact]
	public void Outgoing_AfterDefiningStrategy_ReflectsNewStrategy()
	{
		var strategy = new BaseTransportStrategy();
		var messageType = typeof(PlainMessage);

		// 默认策略不允许该类型的传出，结果进入缓存。
		Assert.False(strategy.Outgoing("plain.channel", messageType));

		strategy.DefineOutgoingStrategy((_, _) => true);

		Assert.True(strategy.Outgoing("plain.channel", messageType));
	}

	[Fact]
	public void Incoming_AfterDefiningStrategy_ReflectsNewStrategy()
	{
		var strategy = new BaseTransportStrategy();
		var messageType = typeof(PlainMessage);

		Assert.False(strategy.Incoming("plain.channel", messageType));

		strategy.DefineIncomingStrategy((_, _) => true);

		Assert.True(strategy.Incoming("plain.channel", messageType));
	}

	[Fact]
	public void Outgoing_AfterAddingStrategy_ReflectsNewStrategy()
	{
		var strategy = new BaseTransportStrategy();
		var messageType = typeof(PlainMessage);

		Assert.False(strategy.Outgoing("plain.channel", messageType));

		strategy.Add(new AlwaysOutgoingStrategy());

		Assert.True(strategy.Outgoing("plain.channel", messageType));
	}

	private sealed class PlainMessage
	{
	}

	/// <summary>
	/// 允许一切传出的传输策略。
	/// </summary>
	private sealed class AlwaysOutgoingStrategy : ITransportStrategy
	{
		public string Name => "AlwaysOutgoing";

		public bool Outgoing(string channel, Type type) => true;

		public bool Incoming(string channel, Type type) => false;
	}
}
