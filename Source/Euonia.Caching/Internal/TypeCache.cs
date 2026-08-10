using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 供序列化器用于查找值类型。
/// </summary>
public static class TypeCache
{
	/// <summary>
	/// 类型名称到 <see cref="Type"/> 的缓存字典。
	/// </summary>
	private static readonly ConcurrentDictionary<string, Type> _types = new();

	/// <summary>
	/// 用于保护类型缓存与解析器列表访问的锁对象。
	/// </summary>
	private static readonly object _typesLock = new();

	/// <summary>
	/// 自定义类型解析器列表。
	/// </summary>
	private static readonly List<Func<string, Type>> _resolvers = new();

	/// <summary>
	/// 获取 <c>typeof(object)</c>。
	/// </summary>
	public static Type ObjectType { get; } = typeof(object);

	/// <summary>
	/// 注册自定义类型解析器，用于在确实需要操控类型序列化方式时使用。
	/// <paramref name="resolve"/> 函数在无法解析请求的类型时允许返回 <c>null</c>。
	/// <paramref name="resolve"/> 函数可能抛出的任何异常都不会向上传播。
	/// </summary>
	/// <param name="resolve">解析器函数。</param>
	public static void RegisterResolveType(Func<string, Type> resolve)
	{
		lock (_typesLock)
		{
			_resolvers.Add(resolve);
		}
	}

	/// <summary>
	/// 按完整名称获取 <see cref="Type"/>（回退到仅使用第一部分）。
	/// </summary>
	/// <param name="type">类型名称。</param>
	/// <returns>有效的 <see cref="Type"/>。</returns>
	/// <exception cref="TypeLoadException">当 <paramref name="type"/> 不是有效类型时抛出。（也可能抛出其他类型加载相关的异常）。</exception>
	public static Type GetType(string type)
	{
		return _types.GetOrAdd(type, t =>
		{
			Type typeResult = null;
			if (_resolvers.Count > 0)
			{
				foreach (var resolver in _resolvers)
				{
					try
					{
						var result = resolver(t);
						if (result != null)
						{
							typeResult = result;
							break;
						}
					}
					catch
					{
						// 抑制错误。
					}
				}
			}

			if (typeResult == null)
			{
				try
				{
					typeResult = Type.GetType(t, false);
				}
				catch
				{
					/* 捕获看似会抛出但我们并不期望任何异常的文件加载异常…… */
				}

				if (typeResult == null)
				{
					try
					{
						var withoutVersion = Regex.Replace(t, @", Version=\d+.\d+.\d+.\d+", string.Empty);
						typeResult = Type.GetType(withoutVersion, false);
					}
					catch
					{
						// 抑制错误。
					}
				}

				if (typeResult == null)
				{
					var typeName = t.Split(',').FirstOrDefault();

					try
					{
						typeResult = Type.GetType(typeName!, false);
					}
					catch
					{
						// 抑制错误。
					}
				}
			}

			return typeResult ?? throw new InvalidOperationException($"Could not load type '{t}'. Try add TypeCache.RegisterResolveType to resolve your type if the resolving continues to fail.");
		});
	}
}