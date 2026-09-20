using System.Collections;
using System.Reflection;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 提供对象图脱敏能力：将可枚举/对象的敏感成员（标注 <see cref="SensitiveDataAttribute"/>
/// 的属性、字段，或对象自身类型标注了该特性）替换为掩码文本。
/// </summary>
/// <remarks>
/// 返回的是面向日志输出的脱敏副本（字典/列表结构），不修改原对象。字符串与标量原样返回，
/// 类库/框架类型（命名空间以 <c>System</c>、<c>Microsoft</c>、<c>Newtonsoft</c>、<c>Castle</c> 等开头）
/// 不再递归展开，避免把常见的运行时结构炸成巨型日志。
/// </remarks>
public static class SensitiveDataMasker
{
	private const int MaxDepth = 8;

	private static readonly string[] _opaqueNamespacePrefixes =
	{
		"System", "Microsoft", "Newtonsoft", "Castle", "Nerosoft.Euonia.Modularity",
	};

	/// <summary>
	/// 对 <paramref name="instance"/> 执行脱敏并返回脱敏副本。
	/// </summary>
	/// <param name="instance">要脱敏的对象。</param>
	/// <param name="mask">敏感值替换用的掩码文本。</param>
	/// <returns>脱敏后的对象（字符串/标量/<c>null</c> 原样返回；对象转为字典；序列转为列表）。</returns>
	public static object Mask(object instance, string mask = "***")
	{
		return MaskCore(instance, mask, new HashSet<object>(ReferenceEqualityComparer.Instance), 0);
	}

	private static object MaskCore(object instance, string mask, HashSet<object> visited, int depth)
	{
		if (instance == null)
		{
			return null;
		}

		var type = instance.GetType();
		if (type.IsValueType || type == typeof(string))
		{
			return instance;
		}

		// 深度受限或已访问过（循环引用）时不再展开，直接返回掩码占位（对象不会原样泄漏）。
		if (depth >= MaxDepth || !visited.Add(instance))
		{
			return mask;
		}

		if (instance is IDictionary dictionaryType)
		{
			var dictionary = new Dictionary<object, object>();
			foreach (DictionaryEntry entry in dictionaryType)
			{
				dictionary[entry.Key] = MaskCore(entry.Value, mask, visited, depth + 1);
			}

			return dictionary;
		}

		if (instance is IEnumerable items and not string)
		{
			return items.Cast<object>()
			            .Select(item => MaskCore(item, mask, visited, depth + 1))
			            .ToList();
		}

		if (type.GetCustomAttribute<SensitiveDataAttribute>() != null
		    || (!string.IsNullOrEmpty(type.FullName) && _opaqueNamespacePrefixes.Any(prefix => type.FullName.StartsWith(prefix, StringComparison.Ordinal))))
		{
			return mask;
		}

		return MaskObject(instance, mask, visited, depth);
	}

	private static Dictionary<string, object> MaskObject(object instance, string mask, HashSet<object> visited, int depth)
	{
		var result = new Dictionary<string, object>(StringComparer.Ordinal);

		foreach (var property in instance.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
		{
			if (!property.CanRead || property.GetIndexParameters().Length > 0)
			{
				continue;
			}

			try
			{
				var value = property.GetValue(instance);
				var attribute = property.GetCustomAttribute<SensitiveDataAttribute>();
				result[property.Name] = attribute != null
					? (string.IsNullOrEmpty(attribute.Mask) ? mask : attribute.Mask)
					: MaskCore(value, mask, visited, depth + 1);
			}
			catch
			{
				result[property.Name] = mask;
			}
		}

		foreach (var field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
		{
			try
			{
				var value = field.GetValue(instance);
				var attribute = field.GetCustomAttribute<SensitiveDataAttribute>();
				result[field.Name] = attribute != null
					? (string.IsNullOrEmpty(attribute.Mask) ? mask : attribute.Mask)
					: MaskCore(value, mask, visited, depth + 1);
			}
			catch
			{
				result[field.Name] = mask;
			}
		}

		return result;
	}
}