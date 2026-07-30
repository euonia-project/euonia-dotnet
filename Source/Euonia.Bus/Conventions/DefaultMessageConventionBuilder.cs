namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 用于构建自定义消息约定的构建器，以替代默认的消息约定。
/// </summary>
public class DefaultMessageConventionBuilder: IMessageConventionBuilder
{
	private readonly BaseMessageConvention _convention = new BaseMessageConvention();
	
	/// <summary>
	/// 获取此构建器已构建的 <see cref="IMessageConvention"/> 实例。
	/// </summary>
	public IMessageConvention Convention => _convention;

	/// <summary>
	/// 定义单播消息类型的约定。
	/// </summary>
	/// <param name="convention">用于判断消息是否为单播消息的约定函数。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	public IMessageConventionBuilder EvaluateUnicast(Func<string, bool> convention)
	{
		ArgumentAssert.ThrowIfNull(convention);
		_convention.DefineUnicastTypeConvention(convention);
		return this;
	}

	/// <summary>
	/// 定义多播消息类型的约定。
	/// </summary>
	/// <param name="convention">用于判断消息是否为多播消息的约定函数。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	public IMessageConventionBuilder EvaluateMulticast(Func<string, bool> convention)
	{
		ArgumentAssert.ThrowIfNull(convention);
		_convention.DefineMulticastTypeConvention(convention);
		return this;
	}

	/// <summary>
	/// 定义请求消息类型的约定。
	/// </summary>
	/// <param name="convention">用于判断消息是否为请求消息的约定函数。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	public IMessageConventionBuilder EvaluateRequest(Func<string, bool> convention)
	{
		ArgumentAssert.ThrowIfNull(convention);
		_convention.DefineRequestTypeConvention(convention);
		return this;
	}
	
	/// <summary>
	/// 定义消息类型约定，根据返回的 <see cref="MessageConventionType"/> 将消息分类为单播、多播或请求类型。
	/// </summary>
	/// <param name="convention">用于评估消息约定类型的函数。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	public IMessageConventionBuilder Evaluate(Func<string, MessageConventionType> convention)
	{
		ArgumentAssert.ThrowIfNull(convention);
		_convention.DefineTypeConvention(convention);
		return this;
	}

	/// <summary>
	/// 添加一个消息约定实例，用于评估类型是消息、命令还是事件。
	/// </summary>
	/// <typeparam name="TConvention">消息约定的类型。</typeparam>
	/// <param name="convention">消息约定实例。</param>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	public IMessageConventionBuilder Add<TConvention>(TConvention convention)
		where TConvention : class, IMessageConvention
	{
		ArgumentAssert.ThrowIfNull(convention);
		_convention.Add(convention);
		return this;
	}

	/// <summary>
	/// 添加一个消息约定类型，用于评估类型是消息、命令还是事件。
	/// </summary>
	/// <typeparam name="TConvention">实现 <see cref="IMessageConvention"/> 且具有无参构造函数的消息约定类型。</typeparam>
	/// <returns>返回当前的 <see cref="IMessageConventionBuilder"/> 实例，以便进行链式调用。</returns>
	public IMessageConventionBuilder Add<TConvention>()
		where TConvention : class, IMessageConvention, new()
	{
		_convention.Add(new TConvention());
		return this;
	}
}