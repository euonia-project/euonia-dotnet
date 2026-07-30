namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 可覆盖的消息约定，基于内部约定提供可被外部覆盖的约定判断逻辑。
/// 当未显式设置覆盖值时，将回退到内部约定的判断结果。
/// </summary>
internal class OverridableMessageConvention : IMessageConvention
{
	private readonly IMessageConvention _innerConvention;
	private Func<string, bool> _isUnicast, _isMulticast, _isRequest;

	/// <summary>
	/// 初始化 <see cref="OverridableMessageConvention"/> 类的新实例。
	/// </summary>
	/// <param name="innerConvention">内部消息约定实例。</param>
	public OverridableMessageConvention(IMessageConvention innerConvention)
	{
		_innerConvention = innerConvention;
	}

	/// <inheritdoc />
	public string Name => $"Override with {_innerConvention.Name}";

	/// <inheritdoc />
	bool IMessageConvention.IsUnicast(string messageType)
	{
		return IsUnicast(messageType);
	}

	/// <inheritdoc />
	bool IMessageConvention.IsMulticast(string messageType)
	{
		return IsMulticast(messageType);
	}

	/// <inheritdoc />
	bool IMessageConvention.IsRequest(string messageType)
	{
		return IsRequest(messageType);
	}

	/// <summary>
	/// 获取或设置用于判断消息类型是否为单播消息的函数。
	/// 若未显式设置，则回退到内部约定的判断逻辑。
	/// </summary>
	public Func<string, bool> IsUnicast
	{
		get => _isUnicast ?? _innerConvention.IsUnicast;
		set => _isUnicast = value;
	}

	/// <summary>
	/// 获取或设置用于判断消息类型是否为多播消息的函数。
	/// 若未显式设置，则回退到内部约定的判断逻辑。
	/// </summary>
	public Func<string, bool> IsMulticast
	{
		get => _isMulticast ?? _innerConvention.IsMulticast;
		set => _isMulticast = value;
	}

	/// <summary>
	/// 获取或设置用于判断消息类型是否为请求消息的函数。
	/// 若未显式设置，则回退到内部约定的判断逻辑。
	/// </summary>
	public Func<string, bool> IsRequest
	{
		get => _isRequest ?? _innerConvention.IsRequest;
		set => _isRequest = value;
	}

	/// <summary>
	/// 定义用于判断消息类型是否为单播消息的约定函数。
	/// </summary>
	/// <param name="convention">用于判断消息是否为单播消息的函数。</param>
	public void DefineUnicast(Func<string, bool> convention)
	{
		_isUnicast = convention;
	}

	/// <summary>
	/// 定义用于判断消息类型是否为多播消息的约定函数。
	/// </summary>
	/// <param name="convention">用于判断消息是否为多播消息的函数。</param>
	public void DefineMulticast(Func<string, bool> convention)
	{
		_isMulticast = convention;
	}

	/// <summary>
	/// 定义用于判断消息类型是否为请求消息的约定函数。
	/// </summary>
	/// <param name="convention">用于判断消息是否为请求消息的函数。</param>
	public void DefineRequest(Func<string, bool> convention)
	{
		_isRequest = convention;
	}
}