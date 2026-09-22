using Apache.NMS;
using Nerosoft.Euonia.Bus.ActiveMq;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="ActiveMqDelivery"/> 的纯逻辑测试（无需 broker）。
/// </summary>
/// <remarks>
/// 该传输此前没有任何测试工程，这些用例同时充当它的第一份覆盖。
/// </remarks>
public class ActiveMqDeliveryTests
{
	[Fact]
	public void ResolveQueueName_WithOverride_UsesOverride()
	{
		Assert.Equal("explicit.queue", ActiveMqDelivery.ResolveQueueName("orders.created", "explicit.queue"));
	}

	[Fact]
	public void ResolveQueueName_WithoutOverride_UsesChannel()
	{
		Assert.Equal("orders.created", ActiveMqDelivery.ResolveQueueName("orders.created"));
	}

	/// <summary>
	/// 空白覆盖值应被视为未设置，而不是生成一个空队列名。
	/// </summary>
	[Fact]
	public void ResolveQueueName_WithBlankOverride_FallsBackToChannel()
	{
		Assert.Equal("orders.created", ActiveMqDelivery.ResolveQueueName("orders.created", "   "));
	}

	[Theory]
	[InlineData(1, MsgPriority.VeryLow)]
	[InlineData(5, MsgPriority.Normal)]
	[InlineData(9, MsgPriority.Highest)]
	[InlineData(0, MsgPriority.Normal)]
	[InlineData(-3, MsgPriority.Normal)]
	[InlineData(20, MsgPriority.Highest)]
	public void ResolvePriority_MapsToNmsPriority(int priority, MsgPriority expected)
	{
		Assert.Equal(expected, ActiveMqDelivery.ResolvePriority(priority));
	}

	/// <summary>
	/// 未声明优先级时应保持 NMS 的默认优先级（Normal），而不是降到 Lowest。
	/// </summary>
	[Fact]
	public void ResolvePriority_WithoutDeclaredPriority_UsesNormal()
	{
		Assert.Equal(MsgPriority.Normal, ActiveMqDelivery.ResolvePriority(null));
	}
}
