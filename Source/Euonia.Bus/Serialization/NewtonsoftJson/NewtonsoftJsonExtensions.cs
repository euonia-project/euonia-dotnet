using Newtonsoft.Json.Linq;

namespace Nerosoft.Euonia.Bus.NewtonsoftJson;

/// <summary>
/// Newtonsoft.Json 相关的扩展方法。
/// </summary>
internal static class NewtonsoftJsonExtensions
{
	/// <summary>
	/// 从 <see cref="JObject"/> 中获取指定属性的值，并使用转换器将其转换为目标类型。
	/// 如果属性不存在则返回 <typeparamref name="T"/> 的默认值。
	/// </summary>
	/// <typeparam name="T">目标类型。</typeparam>
	/// <param name="jsonObject">要读取的 JSON 对象。</param>
	/// <param name="propertyName">属性名称（忽略大小写）。</param>
	/// <param name="converter">用于将 <see cref="JToken"/> 转换为 <typeparamref name="T"/> 的转换函数。</param>
	/// <returns>转换后的值；如果属性不存在则返回默认值。</returns>
	public static T GetValue<T>(this JObject jsonObject, string propertyName, Func<JToken, T> converter)
	{
		if (!jsonObject.TryGetValue(propertyName, StringComparison.OrdinalIgnoreCase, out var token))
		{
			return default;
		}

		{
		}

		return converter(token);
	}
}