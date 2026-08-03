namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 可覆盖的消息约定，基于内部约定提供可被外部覆盖的约定判断逻辑。
/// 当未显式设置覆盖值时，将回退到内部约定的判断结果。
/// </summary>
internal class OverridableMessageConvention : IMessageConvention
{
	/// <summary>
	/// 内部消息约定实例，用于提供默认的判断逻辑。
	/// </summary>
	private readonly IMessageConvention _innerConvention;

	/// <summary>
	/// 单播、多播和请求消息类型的自定义判断函数。
	/// 为 <c>null</c> 时表示未覆盖，将回退到内部约定的判断逻辑。
	/// </summary>
	private Func<string, Type, bool> _isUnicast, _isMulticast, _isRequest;

	/// <summary>
	/// 初始化 <see cref="OverridableMessageConvention"/> 类的新实例。
	/// </summary>
	/// <param name="innerConvention">内部消息约定实例。</param>
	public OverridableMessageConvention(IMessageConvention innerConvention)
	{
		_innerConvention = innerConvention;
	}

	/// <summary>
	/// 获取消息约定的名称。
	/// </summary>
	public string Name => $"Override with {_innerConvention.Name}";

	/// <summary>
	/// 判断指定的消息类型是否为单播消息。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是单播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool IMessageConvention.IsUnicast(string channel, Type type)
	{
		return IsUnicast(channel, type);
	}

	/// <summary>
	/// 判断指定的消息类型是否为多播消息。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是多播消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool IMessageConvention.IsMulticast(string channel, Type type)
	{
		return IsMulticast(channel, type);
	}

	/// <summary>
	/// 判断指定的消息类型是否为请求消息。
	/// </summary>
	/// <param name="channel">消息通道名称。</param>
	/// <param name="type">要检查的消息类型。</param>
	/// <returns>如果是请求消息，则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool IMessageConvention.IsRequest(string channel, Type type)
	{
		return IsRequest(channel, type);
	}

	/// <summary>
	/// 获取或设置用于判断消息类型是否为单播消息的函数。
	/// 若未显式设置，则回退到内部约定的判断逻辑。
	/// </summary>
	public Func<string, Type, bool> IsUnicast
	{
		get => _isUnicast ?? _innerConvention.IsUnicast;
		set => _isUnicast = value;
	}

	/// <summary>
	/// 获取或设置用于判断消息类型是否为多播消息的函数。
	/// 若未显式设置，则回退到内部约定的判断逻辑。
	/// </summary>
	public Func<string, Type, bool> IsMulticast
	{
		get => _isMulticast ?? _innerConvention.IsMulticast;
		set => _isMulticast = value;
	}

	/// <summary>
	/// 获取或设置用于判断消息类型是否为请求消息的函数。
	/// 若未显式设置，则回退到内部约定的判断逻辑。
	/// </summary>
	public Func<string, Type, bool> IsRequest
	{
		get => _isRequest ?? _innerConvention.IsRequest;
		set => _isRequest = value;
	}

	/// <summary>
	/// 定义用于判断消息类型是否为单播消息的约定函数。
	/// </summary>
	/// <param name="convention">用于判断消息是否为单播消息的函数。</param>
	public void DefineUnicast(Func<string, Type, bool> convention)
	{
		_isUnicast = convention;
	}

	/// <summary>
	/// 定义用于判断消息类型是否为多播消息的约定函数。
	/// </summary>
	/// <param name="convention">用于判断消息是否为多播消息的函数。</param>
	public void DefineMulticast(Func<string, Type, bool> convention)
	{
		_isMulticast = convention;
	}

	/// <summary>
	/// 定义用于判断消息类型是否为请求消息的约定函数。
	/// </summary>
	/// <param name="convention">用于判断消息是否为请求消息的函数。</param>
	public void DefineRequest(Func<string, Type, bool> convention)
	{
		_isRequest = convention;
	}
}