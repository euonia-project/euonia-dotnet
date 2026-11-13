namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 判断服务类型是否落在配置的扫描范围内。
/// </summary>
internal static class HttpExposureScope
{
	/// <summary>
	/// 判断类型是否被 <see cref="HttpExposureOptions.ScanTypes"/> 或
	/// <see cref="HttpExposureOptions.ScanNamespaces"/> 允许。
	/// </summary>
	/// <param name="serviceType">服务接口类型。</param>
	/// <param name="options">HTTP 端点暴露选项。</param>
	/// <returns>类型在扫描范围内时返回 <c>true</c>。</returns>
	public static bool IsIncluded(Type serviceType, HttpExposureOptions options)
	{
		var types = options.ScanTypes;
		if (types is { Length: > 0 }
		    && !types.Contains(serviceType.FullName ?? serviceType.Name, StringComparer.OrdinalIgnoreCase))
		{
			return false;
		}

		var namespaces = options.ScanNamespaces;
		if (namespaces is { Length: > 0 })
		{
			var ns = serviceType.Namespace;
			if (string.IsNullOrEmpty(ns)
			    || !namespaces.Any(prefix => IsNamespaceMatch(ns, prefix)))
			{
				return false;
			}
		}

		return true;
	}

	private static bool IsNamespaceMatch(string ns, string prefix)
	{
		return ns.Equals(prefix, StringComparison.Ordinal)
		       || ns.StartsWith(prefix + ".", StringComparison.Ordinal);
	}
}
