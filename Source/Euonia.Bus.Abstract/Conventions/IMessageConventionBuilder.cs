namespace Nerosoft.Euonia.Bus;

/// <summary>
/// <see cref="IMessageConventionBuilder"/>提供了配置消息约定的流式 API，允许用户通过断言或自定义约定实现来定义消息类型（单播、多播、请求）的分类规则。
/// </summary>
/// <remarks>
///	<para>此构建器允许用户轻松地自定义消息分类逻辑，提供使用断言来评估单播、多播和请求类型的方法，以及使用自定义函数评估所有消息类型的方法。此外，用户还可以直接将自定义的 <see cref="IMessageConvention"/> 实现添加到构建器中。</para>
/// <para>构建完成的<see cref="IMessageConventionBuilder"/>可通过<see cref="Convention"/>访问已构建的<see cref="IMessageConvention"/>实例。</para>
/// </remarks>
public interface IMessageConventionBuilder
{
	/// <summary>
	/// 获取此构建器已构建的<see cref="IMessageConvention"/>实例。
	/// </summary>
	IMessageConvention Convention { get; }

	/// <summary>
	/// 添加一个消息约定，用于评估给定通道是否为单播消息。
	/// </summary>
	/// <param name="predicate">用于评估通道是否为单播消息的断言函数。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	IMessageConventionBuilder EvaluateUnicast(Func<string, Type, bool> predicate);

	/// <summary>
	/// 添加一个消息约定，用于评估给定通道是否为多播消息。
	/// </summary>
	/// <param name="predicate">用于评估通道是否为多播消息的断言函数。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	IMessageConventionBuilder EvaluateMulticast(Func<string, Type, bool> predicate);

	/// <summary>
	/// 添加一个消息约定，用于评估给定通道是否为请求消息。
	/// </summary>
	/// <param name="predicate">用于评估通道是否为请求消息的断言函数。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	IMessageConventionBuilder EvaluateRequest(Func<string, Type, bool> predicate);

	/// <summary>
	/// 添加一个消息约定，使用函数评估给定通道是哪种类型。
	/// </summary>
	/// <param name="predicate">用于评估消息约定类型的函数。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	IMessageConventionBuilder Evaluate(Func<string, Type, MessageConventionType> predicate);

	/// <summary>
	/// 添加一个自定义的消息约定实现类型。
	/// </summary>
	/// <typeparam name="TConvention">实现 <see cref="IMessageConvention"/> 且具有无参构造函数的约定类型。</typeparam>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	IMessageConventionBuilder Add<TConvention>() 
		where TConvention : class, IMessageConvention, new();

	/// <summary>
	/// 添加一个自定义的消息约定实例。
	/// </summary>
	/// <typeparam name="TConvention">实现 <see cref="IMessageConvention"/> 的约定类型。</typeparam>
	/// <param name="convention">要添加的消息约定实例。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	IMessageConventionBuilder Add<TConvention>(TConvention convention) 
		where TConvention : class, IMessageConvention;
}