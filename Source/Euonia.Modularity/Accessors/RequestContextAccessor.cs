using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// An implementation of <see cref="IRequestContextAccessor"/>.
/// </summary>
public class RequestContextAccessor : IRequestContextAccessor
{
	private readonly RequestContextAccessorOptions _options;
	private readonly IServiceAccessor _service;

	/// <summary>
	/// Initializes a new instance of the <see cref="RequestContextAccessor"/> class.
	/// </summary>
	/// <param name="options"></param>
	/// <param name="service"></param>
	public RequestContextAccessor(IOptions<RequestContextAccessorOptions> options, IServiceAccessor service)
	{
		_options = options.Value;
		_service = service;
	}

	/// <summary>
	/// Gets the current request context instance.
	/// </summary>
	/// <remarks>
	/// 委托访问器在请求流之外（如消息消费者进程）可能返回 <c>null</c>；
	/// 此时回退到默认访问器中由消息信封恢复的上下文。
	/// </remarks>
	public RequestContext Context
	{
		get
		{
			var defaultContext = _service.GetService<DefaultRequestContextAccessor>()?.Context;

			if (_options.UseDefaultAccessor)
			{
				return defaultContext;
			}

			var delegateAccessor = _service.GetService<DelegateRequestContextAccessor>();
			return delegateAccessor?.Invoke() ?? defaultContext;
		}
	}
}