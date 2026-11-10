using System.Reflection;
using System.Text;

namespace Nerosoft.Euonia.Endpoint.Http;

/// <summary>
/// 提供 HTTP 端点默认前缀、默认路由与路径拼接规则。
/// </summary>
internal static class WebApiNaming
{
	private static readonly string[] _serviceSuffixes = ["ApplicationService", "AppService", "Service"];

	/// <summary>
	/// 根据服务类型推导默认服务名称。
	/// </summary>
	/// <param name="serviceType">服务接口类型。</param>
	/// <returns>kebab-case 形式的服务名称。</returns>
	public static string GetDefaultServiceName(Type serviceType)
	{
		var name = serviceType.Name;
		if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
		{
			name = name[1..];
		}

		foreach (var suffix in _serviceSuffixes)
		{
			if (name.Length > suffix.Length && name.EndsWith(suffix, StringComparison.Ordinal))
			{
				name = name[..^suffix.Length];
				break;
			}
		}

		return ToKebabCase(name);
	}

	/// <summary>
	/// 根据方法名推导默认路由。
	/// </summary>
	/// <param name="method">服务方法。</param>
	/// <returns>kebab-case 形式的路由。</returns>
	public static string GetDefaultRouteName(MethodInfo method)
	{
		var name = method.Name;
		if (name.Length > "Async".Length && name.EndsWith("Async", StringComparison.Ordinal))
		{
			name = name[..^"Async".Length];
		}

		return ToKebabCase(name);
	}

	/// <summary>
	/// 拼接前缀与路由模板。
	/// </summary>
	/// <param name="prefix">路由前缀。</param>
	/// <param name="route">路由模板。</param>
	/// <returns>去掉首尾斜杠后拼接的路由模板。</returns>
	public static string CombinePath(string? prefix, string? route)
	{
		var left = (prefix ?? string.Empty).Trim().Trim('/');
		var right = (route ?? string.Empty).Trim().Trim('/');
		if (left.Length == 0)
		{
			return right;
		}

		if (right.Length == 0)
		{
			return left;
		}

		return $"{left}/{right}";
	}

	/// <summary>
	/// 将 PascalCase 名称转换为 kebab-case。
	/// </summary>
	/// <param name="value">待转换的名称。</param>
	/// <returns>kebab-case 名称。</returns>
	public static string ToKebabCase(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		var builder = new StringBuilder(value.Length + 8);
		for (var index = 0; index < value.Length; index++)
		{
			var current = value[index];
			if (char.IsUpper(current))
			{
				var hasPrevious = index > 0;
				var previousIsLowerOrDigit = hasPrevious && (char.IsLower(value[index - 1]) || char.IsDigit(value[index - 1]));
				var nextIsLower = index + 1 < value.Length && char.IsLower(value[index + 1]);
				if (hasPrevious && (previousIsLowerOrDigit || nextIsLower) && builder.Length > 0 && builder[builder.Length - 1] != '-')
				{
					builder.Append('-');
				}

				builder.Append(char.ToLowerInvariant(current));
				continue;
			}

			if (current is '_' or ' ')
			{
				if (builder.Length > 0 && builder[builder.Length - 1] != '-')
				{
					builder.Append('-');
				}

				continue;
			}

			builder.Append(current);
		}

		return builder.ToString().Trim('-');
	}
}
