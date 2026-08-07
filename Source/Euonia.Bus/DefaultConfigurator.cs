using System.Collections.Concurrent;
using System.Reflection;

// ReSharper disable MemberCanBePrivate.Global

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 默认的消息总线配置器，用于注册处理器、设置约定、分配传输策略和配置身份提供程序。
/// </summary>
internal sealed class DefaultConfigurator : IConfigurator
{
	/// <summary>
	/// 用于管理事件订阅者的弱事件管理器，避免事件订阅导致的内存泄漏。
	/// </summary>
	private readonly WeakEventManager _events = new();

	/// <summary>
	/// 以传输类型为键的传输策略构建器的线程安全字典。
	/// </summary>
	private readonly ConcurrentDictionary<string, ITransportStrategyBuilder> _strategyBuilders = new();

	/// <summary>
	/// 通道注册器，负责注册通道处理器并在注册时触发 <see cref="ChannelRegistered"/> 事件。
	/// </summary>
	private readonly ChannelRegistrar _registrar;

	/// <summary>
	/// 私有构造函数，阻止外部直接实例化，强制使用 <see cref="Instance"/> 单例模式。
	/// </summary>
	public DefaultConfigurator()
	{
		_registrar = new ChannelRegistrar((channel, type, handler) =>
		{
			_events.HandleEvent(this, new ChannelRegisteredEventArgs(channel, type, handler), nameof(ChannelRegistered));
		});
	}

	/// <summary>
	/// 用于配置消息命名和发现约定的构建器。
	/// </summary>
	public IMessageConventionBuilder ConventionBuilder { get; } = new DefaultMessageConventionBuilder();

	/// <summary>
	/// 以传输类型为键的传输策略构建器字典。
	/// </summary>
	public IDictionary<string, ITransportStrategyBuilder> StrategyBuilders => _strategyBuilders;

	/// <summary>
	/// 获取已注册的通道注册信息字典。
	/// </summary>
	public IDictionary<string, ChannelRegistration> Registrations => _registrar.Registrations;

	/// <summary>
	/// 当有通道被注册时触发的事件。
	/// </summary>
	public event EventHandler<ChannelRegisteredEventArgs> ChannelRegistered
	{
		add => _events.AddEventHandler(value);
		remove => _events.RemoveEventHandler(value);
	}

	/// <summary>
	/// 获取 <see cref="DefaultConfigurator"/> 的单例实例。
	/// </summary>
	public static IConfigurator Instance => Singleton<DefaultConfigurator>.Get(() => new DefaultConfigurator());

	/// <summary>
	/// 使用指定的委托配置消息约定。
	/// </summary>
	/// <param name="conventionConfigurator">用于配置 <see cref="IMessageConventionBuilder"/> 的委托。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public IConfigurator SetConvention(Action<IMessageConventionBuilder> conventionConfigurator)
	{
		ArgumentNullException.ThrowIfNull(conventionConfigurator);
		conventionConfigurator(ConventionBuilder);
		return this;
	}

	/// <summary>
	/// 为指定的传输器名称配置传输策略。
	/// </summary>
	/// <param name="name">传输器名称。</param>
	/// <param name="strategyConfigurator">用于配置 <see cref="ITransportStrategyBuilder"/> 的委托。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public IConfigurator SetStrategy(string name, Action<ITransportStrategyBuilder> strategyConfigurator)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(name);
		ArgumentNullException.ThrowIfNull(strategyConfigurator);
		var builder = _strategyBuilders.GetOrAdd(name, _ => new DefaultTransportStrategyBuilder());
		strategyConfigurator(builder);
		return this;
	}

	/// <summary>
	/// 注册指定通道的消息处理器（带返回值）。
	/// </summary>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <typeparam name="TResult">返回值类型。</typeparam>
	/// <param name="channel">通道名称。</param>
	/// <param name="handler">处理器委托。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public IConfigurator RegisterChannel<TMessage, TResult>(string channel, Func<TMessage, IMessageContext, Task<TResult>> handler)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		ArgumentNullException.ThrowIfNull(handler);
		_registrar.Register(channel, handler);
		return this;
	}

	/// <summary>
	/// 注册指定通道的消息处理器（无返回值）。
	/// </summary>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <param name="channel">通道名称。</param>
	/// <param name="handler">处理器委托。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public IConfigurator RegisterChannel<TMessage>(string channel, Func<TMessage, IMessageContext, Task> handler)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		ArgumentNullException.ThrowIfNull(handler);
		_registrar.Register(channel, handler);
		return this;
	}

	/// <summary>
	/// 扫描指定程序集中的处理器类型并注册到通道。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集数组。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public IConfigurator RegisterChannel(params Assembly[] assemblies)
	{
		return RegisterChannel(() => assemblies.SelectMany(assembly => assembly.DefinedTypes));
	}

	/// <summary>
	/// 通过工厂函数返回的处理器类型注册到通道。
	/// </summary>
	/// <param name="typesFactory">返回待注册处理器类型的工厂函数。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public IConfigurator RegisterChannel(Func<IEnumerable<Type>> typesFactory)
	{
		return RegisterChannel(typesFactory());
	}

	/// <summary>
	/// 注册指定的处理器类型数组到通道。
	/// </summary>
	/// <param name="types">要注册的处理器类型数组。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public IConfigurator RegisterChannel(params Type[] types)
	{
		return RegisterChannel(types.AsEnumerable);
	}

	/// <summary>
	/// 将指定的处理器类型集合注册到通道。
	/// </summary>
	/// <param name="types">要注册的处理器类型集合。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public IConfigurator RegisterChannel(IEnumerable<Type> types)
	{
		_registrar.Register(types);
		return this;
	}
}