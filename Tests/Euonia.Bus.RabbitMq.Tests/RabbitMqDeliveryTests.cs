using Nerosoft.Euonia.Bus.RabbitMq;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对 <see cref="RabbitMqDelivery"/> 的纯逻辑测试（无需 broker）。
/// </summary>
/// <remarks>
/// 这些计算此前散落在发布端与消费端各自实现，且回退值不同：
/// 发布端用 <c>Assembly.GetEntryAssembly()?.FullName</c>（形如 <c>MyApp, Version=1.0.0.0, …</c>），
/// 消费端用 <c>GetName().Name</c>（<c>MyApp</c>）。未配置 <c>SubscriptionId</c> 时两端队列名不一致，
/// 发布端的队列存在性检查会直接 404，Send/Call 全部失败。
/// </remarks>
public class RabbitMqDeliveryTests
{
	/// <summary>
	/// 验证显式传入的队列名覆盖配置与通道推导结果。
	/// </summary>
	[Fact]
	public void ResolveQueueName_WithOverride_UsesOverride()
	{
		var options = new RabbitMqBusOptions { SubscriptionId = "sub" };

		Assert.Equal("explicit.queue", RabbitMqDelivery.ResolveQueueName(options, "orders.created", "explicit.queue"));
	}

	/// <summary>
	/// 验证未指定队列名覆盖时，队列名由通道名与订阅标识拼接而成。
	/// </summary>
	[Fact]
	public void ResolveQueueName_WithoutOverride_CombinesChannelAndSubscriptionId()
	{
		var options = new RabbitMqBusOptions { SubscriptionId = "sub" };

		Assert.Equal("orders.created@sub", RabbitMqDelivery.ResolveQueueName(options, "orders.created"));
	}

	/// <summary>
	/// 空白覆盖值应被视为未设置，而不是生成一个空队列名。
	/// </summary>
	[Fact]
	public void ResolveQueueName_WithBlankOverride_FallsBackToChannelDerivedName()
	{
		var options = new RabbitMqBusOptions { SubscriptionId = "sub" };

		Assert.Equal("orders.created@sub", RabbitMqDelivery.ResolveQueueName(options, "orders.created", "   "));
	}

	/// <summary>
	/// 验证配置队列名前缀后，推导出的队列名带有该前缀。
	/// </summary>
	[Fact]
	public void ResolveQueueName_WithPrefix_AppliesPrefix()
	{
		var options = new RabbitMqBusOptions { SubscriptionId = "sub", QueueNamePrefix = "tenant-a" };

		Assert.Equal("tenant-a:orders.created@sub", RabbitMqDelivery.ResolveQueueName(options, "orders.created"));
	}

	/// <summary>
	/// 验证配置交换机名前缀后，推导出的交换机名带有该前缀。
	/// </summary>
	[Fact]
	public void ResolveExchangeName_WithPrefix_AppliesPrefix()
	{
		var options = new RabbitMqBusOptions { ExchangeNamePrefix = "tenant-a" };

		Assert.Equal("tenant-a:orders.created", RabbitMqDelivery.ResolveExchangeName(options, "orders.created"));
	}

	/// <summary>
	/// 验证未配置前缀时交换机名直接使用通道名。
	/// </summary>
	[Fact]
	public void ResolveExchangeName_WithoutPrefix_UsesChannel()
	{
		var options = new RabbitMqBusOptions();

		Assert.Equal("orders.created", RabbitMqDelivery.ResolveExchangeName(options, "orders.created"));
	}

	/// <summary>
	/// 订阅标识的回退链：显式配置 → 入口程序集简单名称 → 通道名称。
	/// 关键在于必须用**简单名称**，否则发布端与消费端的队列名不一致。
	/// </summary>
	[Fact]
	public void ResolveSubscriptionId_WithoutConfiguredValue_UsesEntryAssemblySimpleName()
	{
		var options = new RabbitMqBusOptions();

		var subscriptionId = RabbitMqDelivery.ResolveSubscriptionId(options, "orders.created");

		// 入口程序集的简单名称不含版本/区域信息；FullName 会包含逗号与 Version=。
		Assert.DoesNotContain("Version=", subscriptionId);
		Assert.DoesNotContain(",", subscriptionId);
	}

	/// <summary>
	/// 验证显式配置订阅标识时优先使用配置值，而不是回退到程序集名称。
	/// </summary>
	[Fact]
	public void ResolveSubscriptionId_WithConfiguredValue_UsesIt()
	{
		var options = new RabbitMqBusOptions { SubscriptionId = "configured" };

		Assert.Equal("configured", RabbitMqDelivery.ResolveSubscriptionId(options, "orders.created"));
	}

	/// <summary>
	/// 验证声明的优先级被收敛到受支持范围，任一侧无效时返回 <c>null</c>。
	/// </summary>
	/// <param name="priority">声明的消息优先级。</param>
	/// <param name="maxPriority">队列声明的最大优先级。</param>
	/// <param name="expected">期望的解析结果，<c>null</c> 表示不启用优先级。</param>
	[Theory]
	[InlineData(5, 9, 5)]
	[InlineData(20, 9, 9)]
	[InlineData(20, 3, 3)]
	[InlineData(1, 9, 1)]
	[InlineData(-1, 9, null)]
	[InlineData(0, 9, null)]
	[InlineData(5, 0, null)]
	[InlineData(5, -1, null)]
	public void ResolvePriority_ClampsToSupportedRange(int priority, int maxPriority, int? expected)
	{
		var resolved = RabbitMqDelivery.ResolvePriority(priority, maxPriority);

		Assert.Equal(expected is null ? null : (byte)expected.Value, resolved);
	}

	/// <summary>
	/// 验证未声明优先级时返回 <c>null</c>，不向 broker 声明优先级参数。
	/// </summary>
	[Fact]
	public void ResolvePriority_WithoutDeclaredPriority_ReturnsNull()
	{
		Assert.Null(RabbitMqDelivery.ResolvePriority(null, 9));
	}

	/// <summary>
	/// 未启用优先级时不附加 <c>x-max-priority</c>，避免声明与实际能力不符的队列。
	/// </summary>
	[Fact]
	public void BuildQueueArguments_WithoutPriority_ReturnsNullWhenNoOtherArguments()
	{
		Assert.Null(RabbitMqDelivery.BuildQueueArguments(0));
	}

	/// <summary>
	/// 验证声明了最大优先级时，队列参数包含 <c>x-max-priority</c> 且取值与声明一致。
	/// </summary>
	[Fact]
	public void BuildQueueArguments_WithPriority_AddsMaxPriority()
	{
		var arguments = RabbitMqDelivery.BuildQueueArguments(5);

		Assert.NotNull(arguments);
		Assert.Equal(5, arguments["x-max-priority"]);
	}

	/// <summary>
	/// 超出支持范围的最大优先级应被收敛到 9，而不是原样声明给 broker。
	/// </summary>
	[Fact]
	public void BuildQueueArguments_WithExcessiveMaxPriority_ClampsToNine()
	{
		var arguments = RabbitMqDelivery.BuildQueueArguments(200);

		Assert.NotNull(arguments);
		Assert.Equal(RabbitMqDelivery.MaxSupportedPriority, arguments["x-max-priority"]);
	}

	/// <summary>
	/// 必须保留调用方传入的其他参数（例如死信配置），否则会静默丢掉 DLX 设置。
	/// </summary>
	[Fact]
	public void BuildQueueArguments_MergesAdditionalArguments()
	{
		var additional = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "orders.dlx" };

		var arguments = RabbitMqDelivery.BuildQueueArguments(5, additional);

		Assert.NotNull(arguments);
		Assert.Equal("orders.dlx", arguments["x-dead-letter-exchange"]);
		Assert.Equal(5, arguments["x-max-priority"]);

		// 不应修改调用方传入的字典。
		Assert.False(additional.ContainsKey("x-max-priority"));
	}

	/// <summary>
	/// 验证未启用优先级但传入其他参数时，仅返回调用方提供的参数集合。
	/// </summary>
	[Fact]
	public void BuildQueueArguments_WithoutPriorityButWithAdditional_ReturnsAdditional()
	{
		var additional = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = "orders.dlx" };

		var arguments = RabbitMqDelivery.BuildQueueArguments(0, additional);

		Assert.NotNull(arguments);
		Assert.Single(arguments);
	}
}
