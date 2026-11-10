namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 提供 HTTP 方法名称的校验与规范化。
/// </summary>
internal static class WebApiHttpMethods
{
	private static readonly HashSet<string> _knownMethods = new(StringComparer.OrdinalIgnoreCase)
	{
		HttpMethod.Connect.Method,
		HttpMethod.Delete.Method,
		HttpMethod.Get.Method,
		HttpMethod.Head.Method,
		HttpMethod.Options.Method,
		HttpMethod.Patch.Method,
		HttpMethod.Post.Method,
		HttpMethod.Put.Method,
		HttpMethod.Trace.Method
	};

	/// <summary>
	/// 规范化并校验 HTTP 方法数组。
	/// </summary>
	/// <param name="methods">待校验的方法名称集合。</param>
	/// <param name="description">用于错误信息的方法描述。</param>
	/// <returns>去重并转为大写的方法名称数组。</returns>
	/// <exception cref="InvalidOperationException">数组为空或包含不支持的方法时抛出。</exception>
	public static IReadOnlyList<string> Normalize(IEnumerable<string>? methods, string description)
	{
		if (methods == null)
		{
			throw new InvalidOperationException($"HTTP method array is required for '{description}'.");
		}

		var result = new List<string>();
		foreach (var method in methods)
		{
			if (string.IsNullOrWhiteSpace(method))
			{
				throw new InvalidOperationException($"HTTP method cannot be empty for '{description}'.");
			}

			var value = method.Trim().ToUpperInvariant();
			if (!_knownMethods.Contains(value))
			{
				throw new InvalidOperationException($"Unsupported HTTP method '{method}' for '{description}'.");
			}

			if (!result.Contains(value, StringComparer.Ordinal))
			{
				result.Add(value);
			}
		}

		if (result.Count == 0)
		{
			throw new InvalidOperationException($"At least one HTTP method is required for '{description}'.");
		}

		return result;
	}
}
