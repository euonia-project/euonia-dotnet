namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 针对消息字符串化与元数据操作等已修复缺陷的回归测试。
/// </summary>
public class FixRegressionTests
{
	/// <summary>
	/// 验证路由消息的 <c>ToString()</c> 输出以消息标识加冒号开头，并插入实际类型名而不是未展开的占位文本。
	/// </summary>
	[Fact]
	public void RoutedMessage_ToString_InterpolatesTypeName()
	{
		var message = new RoutedMessage<object>(42, "test-channel");
		var text = message.ToString();

		Assert.StartsWith(message.MessageId + ":", text);
		Assert.NotNull(message.GetTypeName());
		Assert.Contains(message.GetTypeName(), text);
		Assert.DoesNotContain("{GetTypeName()}", text);
	}

	/// <summary>
	/// 验证 <see cref="MessageMetadata"/> 在添加已存在的键时抛出 <see cref="ArgumentException"/>。
	/// </summary>
	[Fact]
	public void MessageMetadata_Add_DuplicateKeyThrows()
	{
		var metadata = new MessageMetadata();
		metadata.Add("key", "value");

		Assert.Throws<ArgumentException>(() => metadata.Add("key", "other"));
	}

	/// <summary>
	/// 验证按键值对移除元数据时，仅当键与值都匹配才删除条目。
	/// </summary>
	[Fact]
	public void MessageMetadata_Remove_KeyValuePair_OnlyRemovesExactMatch()
	{
		var metadata = new MessageMetadata();
		metadata.Add("key", "value");

		var removed = metadata.Remove(new KeyValuePair<string, object>("key", "other"));

		Assert.False(removed);
		Assert.True(metadata.ContainsKey("key"));

		removed = metadata.Remove(new KeyValuePair<string, object>("key", "value"));

		Assert.True(removed);
		Assert.False(metadata.ContainsKey("key"));
	}
}