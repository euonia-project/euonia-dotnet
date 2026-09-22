namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对约定缓存失效的测试。
/// </summary>
/// <remarks>
/// <see cref="BaseMessageConvention"/> 为「通道 + 类型 → 是否单播/多播/请求」的结果做了缓存。
/// 缓存必须在约定集合发生变化时失效，否则在首次判定之后再添加约定将**静默无效**——
/// 这正是配置期与运行期分离时最容易踩到的一类陷阱。
/// </remarks>
public class ConventionCacheInvalidationTests
{
	[Fact]
	public void IsMulticast_AfterAddingConvention_ReflectsNewConvention()
	{
		var convention = new BaseMessageConvention();
		var messageType = typeof(PlainMessage);

		// 首次判定：普通类默认不是多播，结果进入缓存。
		Assert.False(convention.IsMulticast("plain.channel", messageType));

		convention.Add(new MulticastConvention());

		// 新加入的约定应生效；若缓存未失效，这里仍会读到 false。
		Assert.True(convention.IsMulticast("plain.channel", messageType));
	}

	[Fact]
	public void IsUnicast_AfterAddingConvention_ReflectsNewConvention()
	{
		var convention = new BaseMessageConvention();
		var messageType = typeof(PlainMessage);

		Assert.False(convention.IsUnicast("plain.channel", messageType));

		convention.Add(new UnicastConvention());

		Assert.True(convention.IsUnicast("plain.channel", messageType));
	}

	[Fact]
	public void IsRequest_AfterAddingConvention_ReflectsNewConvention()
	{
		var convention = new BaseMessageConvention();
		var messageType = typeof(PlainMessage);

		Assert.False(convention.IsRequest("plain.channel", messageType));

		convention.Add(new RequestConvention());

		Assert.True(convention.IsRequest("plain.channel", messageType));
	}

	[Fact]
	public void IsMulticast_AfterRedefiningConvention_ReflectsNewConvention()
	{
		var convention = new BaseMessageConvention();
		var messageType = typeof(PlainMessage);

		Assert.False(convention.IsMulticast("plain.channel", messageType));

		convention.DefineMulticastTypeConvention((_, _) => true);

		Assert.True(convention.IsMulticast("plain.channel", messageType));
	}

	/// <summary>
	/// 未被任何约定覆盖的普通类。
	/// </summary>
	private sealed class PlainMessage
	{
	}

	/// <summary>
	/// 把所有消息都判定为多播的约定。
	/// </summary>
	private sealed class MulticastConvention : IMessageConvention
	{
		public string Name => "AlwaysMulticast";

		public bool IsUnicast(string channel, Type type) => false;

		public bool IsMulticast(string channel, Type type) => true;

		public bool IsRequest(string channel, Type type) => false;
	}

	/// <summary>
	/// 把所有消息都判定为单播的约定。
	/// </summary>
	private sealed class UnicastConvention : IMessageConvention
	{
		public string Name => "AlwaysUnicast";

		public bool IsUnicast(string channel, Type type) => true;

		public bool IsMulticast(string channel, Type type) => false;

		public bool IsRequest(string channel, Type type) => false;
	}

	/// <summary>
	/// 把所有消息都判定为请求的约定。
	/// </summary>
	private sealed class RequestConvention : IMessageConvention
	{
		public string Name => "AlwaysRequest";

		public bool IsUnicast(string channel, Type type) => false;

		public bool IsMulticast(string channel, Type type) => false;

		public bool IsRequest(string channel, Type type) => true;
	}
}
