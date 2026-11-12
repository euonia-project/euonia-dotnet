using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 提供对象图脱敏能力：将可枚举/对象的敏感成员（标注 <see cref="SensitiveDataAttribute"/>
/// 的属性、字段，或对象自身类型标注了该特性）替换为掩码文本。
/// </summary>
/// <remarks>
/// 返回的是面向日志/审计输出的脱敏副本（字典/列表结构），不修改原对象。字符串与标量原样返回，
/// 类库/框架类型（命名空间以 <c>System</c>、<c>Microsoft</c>、<c>Newtonsoft</c>、<c>Castle</c> 等开头）
/// 不递归展开，而替换为「类型名 + 掩码」占位，既避免把常见的运行时结构炸成巨型日志，又保留可辨识信息。
/// </remarks>
public static class SensitiveDataMasker
{
	private const int DefaultMaxDepth = 8;

	private static readonly string[] _opaqueNamespacePrefixes =
	{
		"System", "Microsoft", "Newtonsoft", "Castle", "Nerosoft.Euonia.Modularity",
	};

	// 缓存各类型的成员反射信息，避免每次掩码时全量反射。
	private static readonly ConcurrentDictionary<Type, MemberDescriptor[]> _memberCache = new();

	/// <summary>
	/// 对 <paramref name="instance"/> 执行脱敏并返回脱敏副本。
	/// </summary>
	/// <param name="instance">要脱敏的对象。</param>
	/// <param name="mask">敏感值替换用的掩码文本。</param>
	/// <param name="maxDepth">递归深度上限；小于 1 时使用默认值 8。</param>
	/// <returns>脱敏后的对象（字符串/标量/<c>null</c> 原样返回；对象转为字典；序列转为列表）。</returns>
	public static object? Mask(object? instance, string mask = "***", int maxDepth = DefaultMaxDepth)
	{
		return MaskCore(instance, mask, new HashSet<object>(ReferenceEqualityComparer.Instance), 0, Math.Max(1, maxDepth));
	}

	private static object? MaskCore(object? instance, string mask, HashSet<object> visited, int depth, int maxDepth)
	{
		if (instance == null)
		{
			return null;
		}

		var type = instance.GetType();
		if (type == typeof(string))
		{
			return instance;
		}

		// 类型级敏感标记必须先于“值类型直接返回”，否则带 [SensitiveData] 的 struct 会泄漏。
		if (type.GetCustomAttribute<SensitiveDataAttribute>() != null)
		{
			return mask;
		}

		if (type.IsValueType && IsSafeValueType(type))
		{
			return instance;
		}

		// 深度受限时不再展开，直接返回掩码占位（对象不会原样泄漏）。
		if (depth >= maxDepth)
		{
			return mask;
		}

		// 惰性查询/异步序列：枚举会执行数据库查询，绝不能为了日志触发。
		// IAsyncEnumerable<T> 对 T 不变，不能靠 IAsyncEnumerable<object> 判断，需按接口定义检测。
		if (instance is IQueryable || IsAsyncStream(type))
		{
			return $"{type.Name}:{mask}";
		}

		// visited 表示“当前递归路径”，进入时加入、退出时移除：同一实例在兄弟分支出现不会被误判为循环。
		if (!visited.Add(instance))
		{
			return mask;
		}

		try
		{
			if (instance is IDictionary dictionaryType)
			{
				var dictionary = new Dictionary<object, object?>();
				foreach (DictionaryEntry entry in dictionaryType)
				{
					// 字典条目的键契约上非空（Hashtable/Dictionary 均拒绝 null 键）。
					dictionary[entry.Key!] = MaskCore(entry.Value, mask, visited, depth + 1, maxDepth);
				}

				return dictionary;
			}

			if (instance is IEnumerable items and not string)
			{
				// 限制集合宽度：误传大集合时只保留前 100 项并标记截断，避免拖垮请求。
				const int maxItems = 100;
				var result = new List<object?>();
				foreach (var item in items)
				{
					if (result.Count >= maxItems)
					{
						result.Add($"... (truncated, {type.Name})");
						break;
					}

					result.Add(MaskCore(item, mask, visited, depth + 1, maxDepth));
				}

				return result;
			}

			if (IsOpaque(type))
			{
				// 不透明类型：保留类型名 + 掩码，便于定位来源同时避免展开庞大的运行时结构。
				return $"{type.Name}:{mask}";
			}

			return MaskObject(instance, mask, visited, depth, maxDepth);
		}
		finally
		{
			visited.Remove(instance);
		}
	}

	private static bool IsSafeValueType(Type type)
	{
		return type.IsPrimitive
		       || type.IsEnum
		       || type == typeof(decimal)
		       || type == typeof(DateTime)
		       || type == typeof(DateTimeOffset)
		       || type == typeof(TimeSpan)
		       || type == typeof(DateOnly)
		       || type == typeof(TimeOnly)
		       || type == typeof(Guid);
	}

	private static bool IsAsyncStream(Type type)
	{
		return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)
		       || type.GetInterfaces().Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>));
	}

	private static bool IsOpaque(Type type)
	{
		var fullName = type.FullName;
		if (string.IsNullOrEmpty(fullName))
		{
			return true;
		}

		foreach (var prefix in _opaqueNamespacePrefixes)
		{
			if (fullName == prefix || fullName.StartsWith(prefix + ".", StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static Dictionary<string, object?> MaskObject(object instance, string mask, HashSet<object> visited, int depth, int maxDepth)
	{
		var result = new Dictionary<string, object?>(StringComparer.Ordinal);
		var members = _memberCache.GetOrAdd(instance.GetType(), ResolveMembers);

		foreach (var member in members)
		{
			try
			{
				var value = member.GetValue(instance);
				result[member.Name] = member.IsSensitive
					? (string.IsNullOrEmpty(member.Mask) ? mask : member.Mask)
					: MaskCore(value, mask, visited, depth + 1, maxDepth);
			}
			catch
			{
				result[member.Name] = mask;
			}
		}

		return result;
	}

	private static MemberDescriptor[] ResolveMembers(Type type)
	{
		var descriptors = new List<MemberDescriptor>();
		foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
		{
			if (!property.CanRead || property.GetIndexParameters().Length > 0)
			{
				continue;
			}

			var attribute = property.GetCustomAttribute<SensitiveDataAttribute>();
			descriptors.Add(new MemberDescriptor(property.Name, property.GetValue, attribute));
		}

		foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
		{
			var attribute = field.GetCustomAttribute<SensitiveDataAttribute>();
			descriptors.Add(new MemberDescriptor(field.Name, field.GetValue, attribute));
		}

		return descriptors.ToArray();
	}

	/// <summary>
	/// 单个可读成员的反射描述：名称、取值委托与敏感标记，用于缓存反射结果、避免每次脱敏重复读取特性。
	/// </summary>
	private sealed class MemberDescriptor
	{
		public MemberDescriptor(string name, Func<object?, object?> getter, SensitiveDataAttribute? attribute)
		{
			Name = name;
			Getter = getter;
			IsSensitive = attribute != null;
			Mask = attribute?.Mask;
		}

		public string Name { get; }

		public bool IsSensitive { get; }

		public string? Mask { get; }

		public object? GetValue(object instance)
		{
			return Getter(instance);
		}

		private readonly Func<object?, object?> Getter;
	}
}
