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
	                                                       "numbers": [1, 2, 3],
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

	[Fact]
	public void GetValue_TypeMismatch_ReturnsDefault()
	{
		// 回归：修复前类型不匹配抛 InvalidOperationException，违背“无法转换返回默认值”的文档契约。
		Assert.Equal(0, Node.GetValue<int>("user.name"));
		Assert.Equal(0, Node.GetValue<int>("active"));
		Assert.Null(Node.GetValue<List<int>>("user.name"));
		Assert.Null(Node.GetValue<List<int>>("user"));
	}

	[Fact]
	public void GetValue_JsonArray_Deserializes()
	{
		// 回归：修复前 JsonArray 终点落默认分支，静默返回 null。
		var numbers = Node.GetValue<List<int>>("numbers");

		Assert.NotNull(numbers);
		Assert.Equal(new[] { 1, 2, 3 }, numbers);
	}

	[Fact]
	public void GetValue_JsonArray_ParamsSegments_Deserializes()
	{
		var numbers = Node.GetValue<List<int>>(new[] { "numbers" });

		Assert.NotNull(numbers);
		Assert.Equal(new[] { 1, 2, 3 }, numbers);
	}

	[Fact]
	public void GetValue_EmptySegments_TypeMismatch_ReturnsDefault()
	{
		// 回归：修复前空路径分支直接调用 GetValue<TValue>()，类型不匹配抛 InvalidOperationException。
		var text = JsonValue.Create("hello")!;

		Assert.Equal(0, text.GetValue<int>(Array.Empty<string>()));
		Assert.Equal(0, Node.GetValue<int>(Array.Empty<string>()));
	}

	[Fact]
	public void GetValue_EmptySegments_DeserializesRootObject()
	{
		var root = Node.GetValue<Dictionary<string, JsonNode?>>(Array.Empty<string>());

		Assert.NotNull(root);
		Assert.True(root.ContainsKey("user"));
	}
}
