using System.Text.Json.Nodes;

namespace Nerosoft.Euonia.Core.Tests.Extensions;

/// <summary>
/// 针对 JSON 路径读取扩展方法的测试。
/// </summary>
public class ExtensionsJsonTests
{
	private static readonly JsonNode Node = JsonNode.Parse("""
	                                                     {
	                                                       "user": {
	                                                         "name": "Alice",
	                                                         "address": { "city": "Paris" }
	                                                       },
	                                                       "items": [
	                                                         { "id": 1 },
	                                                         { "id": 2 }
	                                                       ],
	                                                       "active": true
	                                                     }
	                                                     """)!;

	[Fact]
	public void GetValue_DotPath_ReturnsValue()
	{
		Assert.Equal("Alice", Node.GetValue<string>("user.name"));
		Assert.Equal("Paris", Node.GetValue<string>("user.address.city"));
	}

	[Fact]
	public void GetValue_SlashPath_ReturnsValue()
	{
		Assert.Equal("Alice", Node.GetValue<string>("user/name"));
		Assert.Equal(2, Node.GetValue<int>("items/1/id"));
	}

	[Fact]
	public void GetValue_ParamsSegments_ReturnsValue()
	{
		Assert.Equal("Paris", Node.GetValue<string>("user", "address", "city"));
		Assert.Equal(1, Node.GetValue<int>("items", "0", "id"));
	}

	[Fact]
	public void GetValue_MissingPath_ReturnsDefault()
	{
		Assert.Equal(0, Node.GetValue<int>("user.missing.id"));
		Assert.Null(Node.GetValue<string>("missing"));
	}

	[Fact]
	public void GetValue_JsonObject_Deserializes()
	{
		var user = Node.GetValue<Dictionary<string, JsonNode?>>("user");

		Assert.NotNull(user);
		Assert.Equal("Alice", user["name"]!.GetValue<string>());
	}

	[Fact]
	public void GetValue_JsonValue_Converts()
	{
		Assert.True(Node.GetValue<bool>("active"));
	}

	[Fact]
	public void GetValue_EmptySegments_ReadsRootValue()
	{
		var value = JsonValue.Create(42)!;

		Assert.Equal(42, value.GetValue<int>(Array.Empty<string>()));
	}

	[Fact]
	public void GetValue_NullNode_Throws()
	{
		JsonNode? node = null;

		Assert.Throws<ArgumentNullException>(() => node!.GetValue<string>("user.name"));
	}

	[Fact]
	public void GetValue_EmptyName_Throws()
	{
		Assert.Throws<ArgumentException>(() => Node.GetValue<string>(string.Empty));
	}
}
