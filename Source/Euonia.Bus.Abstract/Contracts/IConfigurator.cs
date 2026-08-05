namespace Nerosoft.Euonia.Bus;

/// <summary>
/// <see cref="IConfigurator"/>是配置消息总线的主接口。
/// 提供了配置消息约定、传输策略和处理器注册的方法。此接口的实现将在总线启动之前用于设置总线。
/// </summary>
public interface IConfigurator
{
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
	/// 
	/// </summary>
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
	/// <returns>对应的 <see cref="ITransportStrategy"/> 实例。</returns>
	ITransportStrategy GetStrategy(string transport)
	{
		return StrategyBuilders.TryGetValue(transport, out var builder) ? builder.Strategy : null;
	}
}