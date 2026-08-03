using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 内置的消息约定实现。
/// </summary>
public class BaseMessageConvention : IMessageConvention
{
	/// <summary>
	/// 默认的消息约定实例，基于 <see cref="DefaultMessageConvention"/> 并支持覆盖。
	/// </summary>
	private readonly OverridableMessageConvention _defaultConvention = new(new DefaultMessageConvention());

	/// <summary>
	/// 已注册的消息约定实例列表。
	/// </summary>
	private readonly List<IMessageConvention> _conventions = [];

	/// <summary>
	/// 多播消息约定的判断结果缓存。
	/// </summary>
	private readonly ConventionCache _multicastConventionCache = new();

	/// <summary>
	/// 单播消息约定的判断结果缓存。
	/// </summary>
	private readonly ConventionCache _unicastConventionCache = new();

	/// <summary>
	/// 请求消息约定的判断结果缓存。
	/// </summary>
	private readonly ConventionCache _requestConventionCache = new();

	/// <summary>
	/// 初始化 <see cref="BaseMessageConvention"/> 类的新实例。
	/// </summary>
	public BaseMessageConvention()
	{
		_conventions.Add(_defaultConvention);
	}

	/// <summary>
	/// 判断指定的消息通道是否为单播消息。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是单播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="channel"/> 为 <c>null</c> 时抛出。</exception>
	public bool IsUnicast(string channel, Type type)
	{
		ArgumentNullException.ThrowIfNull(channel);

		return _unicastConventionCache.Apply(channel, handle =>
		{
			return _conventions.Any(x => x.IsUnicast(handle, type));
		});
	}

	/// <summary>
	/// 判断指定的消息通道是否为多播消息。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是多播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="channel"/> 为 <c>null</c> 时抛出。</exception>
	public bool IsMulticast(string channel, Type type)
	{
		ArgumentNullException.ThrowIfNull(channel);

		return _multicastConventionCache.Apply(channel, handle =>
		{
			return _conventions.Any(x => x.IsMulticast(handle, type));
		});
	}

	/// <summary>
	/// 判断指定的消息通道是否为请求消息。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是请求消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="channel"/> 为 <c>null</c> 时抛出。</exception>
	public bool IsRequest(string channel, Type type)
	{
		ArgumentNullException.ThrowIfNull(channel);

		return _requestConventionCache.Apply(channel, handle =>
		{
			return _conventions.Any(x => x.IsRequest(handle, type));
		});
	}

	/// <summary>
	/// 定义单播消息类型的约定。
	/// </summary>
	/// <param name="convention">用于判断消息是否为单播消息的约定函数。</param>
	internal void DefineUnicastTypeConvention(Func<string, Type, bool> convention)
	{
		_defaultConvention.DefineUnicast(convention);
	}

	/// <summary>
	/// 定义多播消息类型的约定。
	/// </summary>
	/// <param name="convention">用于判断消息是否为多播消息的约定函数。</param>
	internal void DefineMulticastTypeConvention(Func<string, Type, bool> convention)
	{
		_defaultConvention.DefineMulticast(convention);
	}

	/// <summary>
	/// 定义请求消息类型的约定。
	/// </summary>
	/// <param name="convention">用于判断消息是否为请求消息的约定函数。</param>
	internal void DefineRequestTypeConvention(Func<string, Type, bool> convention)
	{
		_defaultConvention.DefineRequest(convention);
	}

	/// <summary>
	/// 定义消息类型约定，根据返回的 <see cref="MessageConventionType"/> 将消息分类为单播、多播或请求类型。
	/// </summary>
	/// <param name="convention">用于评估消息约定类型的函数。</param>
	internal void DefineTypeConvention(Func<string, Type, MessageConventionType> convention)
	{
		ArgumentNullException.ThrowIfNull(convention);

		DefineUnicastTypeConvention((channel, type) => convention(channel, type) == MessageConventionType.Unicast);
		DefineMulticastTypeConvention((channel, type) => convention(channel, type) == MessageConventionType.Multicast);
		DefineRequestTypeConvention((channel, type) => convention(channel, type) == MessageConventionType.Request);
	}

	/// <summary>
	/// 添加一个或多个消息约定实例。
	/// </summary>
	/// <param name="conventions">要添加的消息约定实例数组。</param>
	/// <exception cref="ArgumentException">当 <paramref name="conventions"/> 为 <c>null</c> 或空时抛出。</exception>
	internal void Add(params IMessageConvention[] conventions)
	{
		if (conventions == null || conventions.Length == 0)
		{
			throw new ArgumentException(@"At least one convention must be provided.", nameof(conventions));
		}

		_conventions.AddRange(conventions);
	}

	/// <summary>
	/// 获取已注册的约定名称列表。
	/// </summary>
	internal string[] RegisteredConventions => _conventions.Select(x => x.Name).ToArray();

	/// <summary>
	/// 获取消息约定的名称。
	/// </summary>
	public string Name => "Default";

	/// <summary>
	/// 约定缓存，用于缓存消息通道的约定判断结果。
	/// </summary>
	private class ConventionCache
	{
		/// <summary>
		/// 应用指定的约定函数并缓存结果。
		/// </summary>
		/// <param name="channel">消息通道名称。</param>
		/// <param name="convention">用于评估约定的函数。</param>
		/// <returns>约定的判断结果。</returns>
		public bool Apply(string channel, Func<string, bool> convention)
		{
			return _cache.GetOrAdd(channel, convention);
		}

		// ReSharper disable once UnusedMember.Local

		/// <summary>
		/// 重置缓存。
		/// </summary>
		public void Reset()
		{
			_cache.Clear();
		}

		private readonly ConcurrentDictionary<string, bool> _cache = new();
	}
}