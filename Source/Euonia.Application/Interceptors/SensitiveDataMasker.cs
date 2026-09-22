using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 提供对象图脱敏能力：将可枚举/对象的敏感成员（标注 <see cref="SensitiveDataAttribute"/>
/// 的属性、字段，或对象自身类型标注了该特性）替换为掩码文本。
/// </summary>
/// <remarks>
/// 返回的是面向日志输出的脱敏副本（字典/列表结构），不修改原对象。字符串与标量原样返回，
/// 类库/框架类型（命名空间以 <c>System</c>、<c>Microsoft</c>、<c>Newtonsoft</c>、<c>Castle</c> 等开头）
/// 不递归展开，而替换为「类型名 + 掩码」占位，既避免把常见的运行时结构炸成巨型日志，又保留可辨识信息。
/// </remarks>
public static class SensitiveDataMasker
{
	private const int MaxDepth = 8;

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

		if (type.GetCustomAttribute<SensitiveDataAttribute>() != null)
		{
			return mask;
		}

		if (IsOpaque(type))
		{
			// 不透明类型：保留类型名 + 掩码，便于定位来源同时避免展开庞大的运行时结构。
			return $"{type.Name}:{mask}";
		}

		return MaskObject(instance, mask, visited, depth);
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
			if (fullName.StartsWith(prefix, StringComparison.Ordinal))
			{
				return true;
			}
		}

		return false;
	}

	private static Dictionary<string, object> MaskObject(object instance, string mask, HashSet<object> visited, int depth)
	{
		var result = new Dictionary<string, object>(StringComparer.Ordinal);
		var members = _memberCache.GetOrAdd(instance.GetType(), ResolveMembers);

		foreach (var member in members)
		{
			try
			{
				var value = member.GetValue(instance);
				result[member.Name] = member.IsSensitive
					? (string.IsNullOrEmpty(member.Mask) ? mask : member.Mask)
					: MaskCore(value, mask, visited, depth + 1);
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

	private sealed class MemberDescriptor
	{
		public MemberDescriptor(string name, Func<object, object> getter, SensitiveDataAttribute attribute)
		{
			Name = name;
			Getter = getter;
			IsSensitive = attribute != null;
			Mask = attribute?.Mask;
		}

		public string Name { get; }

		public bool IsSensitive { get; }

		public string Mask { get; }

		public object GetValue(object instance)
		{
			return Getter(instance);
		}

		private readonly Func<object, object> Getter;
	}
}