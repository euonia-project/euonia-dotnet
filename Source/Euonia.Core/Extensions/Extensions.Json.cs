using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// 为 <see cref="JsonNode"/> 提供按路径检索类型化值的扩展方法。
/// </summary>
/// <remarks>
/// 此静态类是无状态的，提供便捷辅助方法，使用分隔路径（点或斜杠）
/// 或路径段序列来导航 <see cref="JsonObject"/> 与 <see cref="JsonArray"/> 结构。
/// 当路径无法解析或值无法转换为指定类型时，方法返回该类型的默认值。
/// </remarks>
public static partial class Extensions
{
	/// <param name="node">要搜索的根 <see cref="JsonNode"/>。不能为 <c>null</c>。</param>
	extension(JsonNode node)
	{
		/// <summary>
		/// 使用单个分隔路径字符串从提供的 <see cref="JsonNode"/> 中检索类型为
		/// <typeparamref name="TValue"/> 的值。路径可使用 '.' 或 '/' 作为分隔符。
		/// </summary>
		/// <typeparam name="TValue">预期的返回类型。</typeparam>
		/// <param name="name">
		/// 指定属性/数组遍历的分隔路径，例如 "user.address.street" 或 "items/0/name"。
		/// 不能为 <c>null</c> 或空。
		/// </param>
		/// <returns>
		/// 若路径存在且转换成功，返回转换为 <typeparamref name="TValue"/> 的值；
		/// 否则返回 <typeparamref name="TValue"/> 的默认值。
		/// </returns>
		/// <exception cref="ArgumentNullException"><paramref name="node"/> 为 <c>null</c>。</exception>
		/// <exception cref="ArgumentException"><paramref name="name"/> 为 <c>null</c> 或空。</exception>
		public TValue GetValue<TValue>(string name)
		{
			if (node is null)
			{
				throw new ArgumentNullException(nameof(node), "JsonNode cannot be null.");
			}

			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException("Name cannot be null or empty.", nameof(name));
			}

			var names = name.Split('.', '/');
			return node.GetValue<TValue>(names);
		}

		/// <summary>
		/// 使用提供的路径段遍历 JSON 结构，从提供的 <see cref="JsonNode"/> 中
		/// 检索类型为 <typeparamref name="TValue"/> 的值。
		/// </summary>
		/// <typeparam name="TValue">预期的返回类型。</typeparam>
		/// <param name="names">
		/// 路径段的有序数组。每个段可以是：
		/// - 当前节点为 <see cref="JsonObject"/> 时的属性名（字符串），或
		/// - 当前节点为 <see cref="JsonArray"/> 时的数组索引（整数或可解析为 int 的字符串）。
		/// </param>
		/// <returns>
		/// 若最终节点为 <see cref="JsonValue"/>，或最终的 <see cref="JsonObject"/> / <see cref="JsonArray"/>
		/// 可反序列化为 <typeparamref name="TValue"/>，则返回转换为 <typeparamref name="TValue"/> 的值。
		/// 若路径不存在、最终节点无法转换或转换过程抛出异常，则返回 <typeparamref name="TValue"/> 的默认值。
		/// </returns>
		/// <exception cref="ArgumentNullException"><paramref name="node"/> 为 <c>null</c>。</exception>
		/// <remarks>
		/// 遍历算法：
		/// 1. 从 <paramref name="node"/> 开始。
		/// 2. 对于 <paramref name="names"/> 中的每个段：
		///    a. 若当前节点为 <see cref="JsonObject"/>，尝试用该段的字符串表示获取属性。
		///    b. 否则若当前节点为 <see cref="JsonArray"/>，尝试将该段解析为整数索引并访问元素。
		///    c. 若上述步骤均未成功，返回 <typeparamref name="TValue"/> 的默认值。
		/// 3. 成功遍历后，尝试返回：
		///    - 通过 <see cref="JsonNode.GetValue{TValue}()"/> 转换的 <see cref="JsonValue"/>，或
		///    - 通过 <see cref="JsonSerializer.Deserialize{TValue}(JsonNode,JsonSerializerOptions)"/> 反序列化的 <see cref="JsonObject"/> / <see cref="JsonArray"/>。
		/// 4. 否则返回默认值。
		/// <para>
		/// <paramref name="names"/> 为 <c>null</c> 或空数组时，直接对根节点执行同样的转换。
		/// 转换过程中的异常（如 <see cref="InvalidOperationException"/>、<see cref="JsonException"/>、<see cref="FormatException"/>）
		/// 会被捕获并返回默认值，不向调用方抛出。
		/// </para>
		/// </remarks>
		public TValue GetValue<TValue>(params string[] names)
		{
			ArgumentNullException.ThrowIfNull(node);
			if (names is null || names.Length == 0)
			{
				return ConvertNode<TValue>(node)!;
			}

			var currentNode = node;
			foreach (var name in names)
			{
				switch (currentNode)
				{
					case JsonObject jsonObject when jsonObject.TryGetPropertyValue(name, out var nextNode):
						currentNode = nextNode;
						break;
					case JsonArray jsonArray when int.TryParse(name, out var index) && index >= 0 && index < jsonArray.Count:
						currentNode = jsonArray[index];
						break;
					default:
						return default!;
				}
			}

			return ConvertNode<TValue>(currentNode)!;
		}
	}

	private static TValue? ConvertNode<TValue>(JsonNode? node)
	{
		try
		{
			return node switch
			{
				JsonValue jsonValue => jsonValue.GetValue<TValue>(),
				JsonObject jsonObject => jsonObject.Deserialize<TValue>(),
				JsonArray jsonArray => jsonArray.Deserialize<TValue>(),
				_ => default
			};
		}
		catch (Exception exception) when (exception is InvalidOperationException or JsonException or FormatException or NotSupportedException or InvalidCastException or OverflowException)
		{
			// 契约：路径存在但值无法转换为 TValue 时返回默认值，而不是抛出异常。
			return default;
		}
	}
}
