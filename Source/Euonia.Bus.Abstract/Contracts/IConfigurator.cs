using System.Reflection;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// <see cref="IConfigurator"/> 是配置消息总线的主接口。
/// 提供了配置消息约定、传输策略和处理器注册的方法。此接口的实现将在总线启动之前用于设置总线。
/// </summary>
public interface IConfigurator
{
	/// <summary>
	/// 当通道注册时触发的事件。
	/// </summary>
	event EventHandler<ChannelRegisteredEventArgs> ChannelRegistered;

	/// <summary>
	/// 获取已配置的 <see cref="IMessageConventionBuilder"/>。
	/// </summary>
	/// <returns>已配置的 <see cref="IMessageConventionBuilder"/> 实例。</returns>
	IMessageConventionBuilder ConventionBuilder { get; }

	/// <summary>
	/// 获取已配置的传输策略构建器映射。
	/// </summary>
	/// <returns>已配置的传输策略构建器映射。</returns>
	IDictionary<string, ITransportStrategyBuilder> StrategyBuilders { get; }

	/// <summary>
	/// 获取已注册的通道及其注册信息的映射。
	/// </summary>
	/// <returns>已注册的通道映射（通道名称 → 通道注册信息）。</returns>
	IDictionary<string, ChannelRegistration> Registrations { get; }

	/// <summary>
	/// 获取消息约定，可用于消息格式化和验证。
	/// </summary>
	/// <returns>已配置的 <see cref="IMessageConvention"/> 实例。</returns>
	IMessageConvention Convention => ConventionBuilder.Convention;

	/// <summary>
	/// 获取传输策略名称列表，可用于消息路由和分类。
	/// </summary>
	/// <returns>传输策略名称列表。</returns>
	List<string> StrategyAssignedTypes => [.. StrategyBuilders.Keys];

	/// <summary>
	/// 获取指定传输名称对应的传输策略，可用于消息路由和分类。
	/// </summary>
	/// <param name="transport">传输名称。</param>
	/// <returns>对应的 <see cref="ITransportStrategy"/> 实例；若未找到对应配置，则返回 <c>null</c>。</returns>
	ITransportStrategy GetStrategy(string transport)
	{
		return StrategyBuilders.TryGetValue(transport, out var builder) ? builder.Strategy : null;
	}

	/// <summary>
	/// 获取传输策略配置的版本号，每次策略变更时递增。
	/// </summary>
	/// <remarks>
	/// 供缓存「通道 + 类型 → 传输器列表」的组件判断缓存是否仍有效。
	/// 没有该版本号时，缓存无法感知运行期的策略变更，
	/// 后配置的传输器会被静默忽略（消息继续走旧传输器或退回默认传输器）。
	/// <para>
	/// 默认实现固定返回 <c>0</c>，因此既有实现无需改动即可继续工作；
	/// 但若其策略可变，应重写并递增该值，否则依赖方无法感知变更。
	/// </para>
	/// </remarks>
	long StrategyVersion => 0;

	/// <summary>
	/// 使用指定的委托配置消息约定。
	/// </summary>
	/// <param name="conventionConfigurator">用于配置 <see cref="IMessageConventionBuilder"/> 的委托。</param>
	/// <returns>返回当前的 <see cref="IConfigurator"/> 实例，以便进行链式调用。</returns>
	IConfigurator SetConvention(Action<IMessageConventionBuilder> conventionConfigurator);

	/// <summary>
	/// 为指定的传输器名称配置传输策略。
	/// </summary>
	/// <param name="name">传输器名称。</param>
	/// <param name="strategyConfigurator">用于配置 <see cref="ITransportStrategyBuilder"/> 的委托。</param>
	/// <returns>返回当前的 <see cref="IConfigurator"/> 实例，以便进行链式调用。</returns>
	IConfigurator SetStrategy(string name, Action<ITransportStrategyBuilder> strategyConfigurator);

	/// <summary>
	/// 注册指定通道的消息处理器（带返回值）。
	/// </summary>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <typeparam name="TResult">返回值类型。</typeparam>
	/// <param name="channel">通道名称。</param>
	/// <param name="handler">处理器委托。</param>
	/// <returns>返回当前的 <see cref="IConfigurator"/> 实例，以便进行链式调用。</returns>
	IConfigurator RegisterChannel<TMessage, TResult>(string channel, Func<TMessage, IMessageContext, Task<TResult>> handler);

	/// <summary>
	/// 注册指定通道的消息处理器（无返回值）。
	/// </summary>
	/// <typeparam name="TMessage">消息类型。</typeparam>
	/// <param name="channel">通道名称。</param>
	/// <param name="handler">处理器委托。</param>
	/// <returns>返回当前的 <see cref="IConfigurator"/> 实例，以便进行链式调用。</returns>
	IConfigurator RegisterChannel<TMessage>(string channel, Func<TMessage, IMessageContext, Task> handler);

	/// <summary>
	/// 扫描指定程序集中的处理器类型并注册到通道。
	/// </summary>
	/// <param name="assemblies">要扫描的程序集数组。</param>
	/// <returns>返回当前的 <see cref="IConfigurator"/> 实例，以便进行链式调用。</returns>
	IConfigurator RegisterChannel(params Assembly[] assemblies);

	/// <summary>
	/// 通过工厂函数返回的处理器类型注册到通道。
	/// </summary>
	/// <param name="typesFactory">返回待注册处理器类型的工厂函数。</param>
	/// <returns>返回当前的 <see cref="IConfigurator"/> 实例，以便进行链式调用。</returns>
	IConfigurator RegisterChannel(Func<IEnumerable<Type>> typesFactory);

	/// <summary>
	/// 注册指定的处理器类型数组到通道。
	/// </summary>
	/// <param name="types">要注册的处理器类型数组。</param>
	/// <returns>返回当前的 <see cref="IConfigurator"/> 实例，以便进行链式调用。</returns>
	IConfigurator RegisterChannel(params Type[] types);

	/// <summary>
	/// 将指定的处理器类型集合注册到通道。
	/// </summary>
	/// <param name="types">要注册的处理器类型集合。</param>
	/// <returns>返回当前的 <see cref="IConfigurator"/> 实例，以便进行链式调用。</returns>
	IConfigurator RegisterChannel(IEnumerable<Type> types);

	/// <summary>
	/// 获取当前用于解析消息通道名称的委托。
	/// </summary>
	/// <remarks>
	/// 该委托通常用于根据消息类型推导默认通道名称；其返回值可能为空或空白，调用方应在缓存或注册前进行校验。
	/// </remarks>
	Func<Type, string> ChannelResolver { get; }

	/// <summary>
	/// 设置消息类型到通道名称的解析器。
	/// </summary>
	/// <param name="channelResolver">根据消息类型返回通道名称的委托。</param>
	/// <param name="replaceDefault">是否替换默认解析逻辑；为 <c>true</c> 时，以该委托覆盖默认解析器。</param>
	/// <returns>返回当前的 <see cref="IConfigurator"/> 实例，以便进行链式调用。</returns>
	IConfigurator SetChannelResolver(Func<Type, string> channelResolver, bool replaceDefault = false);
}