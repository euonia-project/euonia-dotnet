namespace Nerosoft.Euonia.Bus.Tests;

public class FixRegressionTests
{
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

	[Fact]
	public void MessageMetadata_Add_DuplicateKeyThrows()
	{
		var metadata = new MessageMetadata();
		metadata.Add("key", "value");

		Assert.Throws<ArgumentException>(() => metadata.Add("key", "other"));
	}

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