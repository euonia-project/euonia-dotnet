using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MemberCanBePrivate.Global

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 默认的消息总线配置器，用于注册处理器、设置约定、分配传输策略和配置身份提供程序。
/// </summary>
internal sealed class DefaultConfigurator : IConfigurator
{
	private readonly ConcurrentDictionary<string, ITransportStrategyBuilder> _strategyBuilders = new();
	private readonly ConcurrentDictionary<string, ChannelRegistration> _registrations = new();
	private Func<string> _defaultTransporterGetter;

	/// <summary>
	/// 获取默认传输器的名称。
	/// </summary>
	public string DefaultTransporter => _defaultTransporterGetter?.Invoke();

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
	public IDictionary<string, ChannelRegistration> Registrations => _registrations;

	/// <summary>
	/// 使用指定的委托配置消息约定。
	/// </summary>
	/// <param name="conventionConfigurator">用于配置 <see cref="IMessageConventionBuilder"/> 的委托。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public DefaultConfigurator SetConvention(Action<IMessageConventionBuilder> conventionConfigurator)
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
	public DefaultConfigurator SetStrategy(string name, Action<ITransportStrategyBuilder> strategyConfigurator)
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
	public DefaultConfigurator RegisterChannel<TMessage, TResult>(string channel, Func<TMessage, IMessageContext, Task<TResult>> handler)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		ArgumentNullException.ThrowIfNull(handler);
		ChannelRegistrar.Instance.Register(channel, handler);
		return this;
	}

	/// <summary>
	/// 注册指定通道的消息处理器（无返回值）。
	/// </summary>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <param name="channel">通道名称。</param>
	/// <param name="handler">处理器委托。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public DefaultConfigurator RegisterChannel<TMessage>(string channel, Func<TMessage, IMessageContext, Task> handler)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(channel);
		ArgumentNullException.ThrowIfNull(handler);
		ChannelRegistrar.Instance.Register(channel, handler);
		return this;
	}

	/// <summary>
	/// 扫描指定程序集中的处理器类型并注册到通道。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集数组。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public DefaultConfigurator RegisterChannel(params Assembly[] assemblies)
	{
		return RegisterChannel(() => assemblies.SelectMany(assembly => assembly.DefinedTypes));
	}

	/// <summary>
	/// 通过工厂函数返回的处理器类型注册到通道。
	/// </summary>
	/// <param name="typesFactory">返回待注册处理器类型的工厂函数。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public DefaultConfigurator RegisterChannel(Func<IEnumerable<Type>> typesFactory)
	{
		return RegisterChannel(typesFactory());
	}

	/// <summary>
	/// 注册指定的处理器类型数组到通道。
	/// </summary>
	/// <param name="types">要注册的处理器类型数组。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public DefaultConfigurator RegisterChannel(params Type[] types)
	{
		return RegisterChannel(types.AsEnumerable);
	}

	/// <summary>
	/// 将指定的处理器类型集合注册到通道。
	/// </summary>
	/// <param name="types">要注册的处理器类型集合。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public DefaultConfigurator RegisterChannel(IEnumerable<Type> types)
	{
		ChannelRegistrar.Instance.Register(types);
		return this;
	}

	/// <summary>
	/// 设置用于获取默认传输器名称的工厂委托。
	/// </summary>
	/// <param name="transporterGetter">返回默认传输器名称的工厂委托。</param>
	/// <returns>返回当前的 <see cref="DefaultConfigurator"/> 实例，以便进行链式调用。</returns>
	public DefaultConfigurator SetDefaultTransporter(Func<string> transporterGetter)
	{
		ArgumentNullException.ThrowIfNull(transporterGetter);
		_defaultTransporterGetter = transporterGetter;
		return this;
	}
}