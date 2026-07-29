using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The built-in message convention.
/// </summary>
public class MessageConvention : IMessageConvention
{
	private readonly OverridableMessageConvention _defaultConvention = new(new DefaultMessageConvention());
	private readonly List<IMessageConvention> _conventions = [];
	private readonly ConventionCache _multicastConventionCache = new();
	private readonly ConventionCache _unicastConventionCache = new();
	private readonly ConventionCache _requestConventionCache = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="MessageConvention"/> class.
	/// </summary>
	public MessageConvention()
	{
		_conventions.Add(_defaultConvention);
	}

	/// <summary>
	/// Determines whether the specified type is a command.
	/// </summary>
	/// <param name="messageType"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public bool IsUnicastType(Type messageType)
	{
		ArgumentNullException.ThrowIfNull(messageType);

		return _unicastConventionCache.Apply(messageType, handle =>
		{
			var t = Type.GetTypeFromHandle(handle);
			return _conventions.Any(x => x.IsUnicastType(t));
		});
	}

	/// <summary>
	/// Determines whether the specified type is an event.
	/// </summary>
	/// <param name="messageType"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	public bool IsMulticastType(Type messageType)
	{
		ArgumentNullException.ThrowIfNull(messageType);

		return _multicastConventionCache.Apply(messageType, handle =>
		{
			var t = Type.GetTypeFromHandle(handle);
			return _conventions.Any(x => x.IsMulticastType(t));
		});
	}

	/// <inheritdoc />
	public bool IsRequestType(Type messageType)
	{
		ArgumentNullException.ThrowIfNull(messageType);

		return _requestConventionCache.Apply(messageType, handle =>
		{
			var t = Type.GetTypeFromHandle(handle);
			return _conventions.Any(x => x.IsRequestType(t));
		});
	}

	internal void DefineUnicastTypeConvention(Func<Type, bool> convention)
	{
		_defaultConvention.DefineUnicastType(convention);
	}

	internal void DefineMulticastTypeConvention(Func<Type, bool> convention)
	{
		_defaultConvention.DefineMulticastType(convention);
	}

	internal void DefineRequestTypeConvention(Func<Type, bool> convention)
	{
		_defaultConvention.DefineRequestType(convention);
	}

	internal void DefineTypeConvention(Func<Type, MessageConventionType> convention)
	{
		ArgumentNullException.ThrowIfNull(convention);

		DefineUnicastTypeConvention(type => convention(type) == MessageConventionType.Unicast);
		DefineMulticastTypeConvention(type => convention(type) == MessageConventionType.Multicast);
		DefineRequestTypeConvention(type => convention(type) == MessageConventionType.Request);
	}

	internal void Add(params IMessageConvention[] conventions)
	{
		if (conventions == null || conventions.Length == 0)
		{
			throw new ArgumentException(@"At least one convention must be provided.", nameof(conventions));
		}

		_conventions.AddRange(conventions);
	}

	/// <summary>
	/// Gets the registered conventions.
	/// </summary>
	internal string[] RegisteredConventions => _conventions.Select(x => x.Name).ToArray();

	/// <inheritdoc/>
	public string Name => "Default";

	private class ConventionCache
	{
		public bool Apply(Type type, Func<RuntimeTypeHandle, bool> convention)
		{
			return _cache.GetOrAdd(type.TypeHandle, convention);
		}

		// ReSharper disable once UnusedMember.Local

		/// <summary>
		/// Reset cache.
		/// </summary>
		public void Reset()
		{
			_cache.Clear();
		}

		private readonly ConcurrentDictionary<RuntimeTypeHandle, bool> _cache = new();
	}
}