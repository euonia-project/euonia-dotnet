using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// An implementation of <see cref="IRequestContextAccessor"/>.
/// </summary>
/// <remarks>
/// 访问器与所在作用域同生命周期，直接从该作用域解析上下文存储；
/// 不依赖环境服务访问器，也不创建兜底作用域。
/// </remarks>
public class RequestContextAccessor : IRequestContextAccessor
{
	private readonly RequestContextAccessorOptions _options;
	private readonly IServiceProvider _provider;

	/// <summary>
	/// Initializes a new instance of the <see cref="RequestContextAccessor"/> class.
	/// </summary>
	/// <param name="options">访问器配置选项。</param>
	/// <param name="provider">当前作用域的服务提供程序。</param>
	public RequestContextAccessor(IOptions<RequestContextAccessorOptions> options, IServiceProvider provider)
	{
		_options = options.Value;
		_provider = provider;
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
			var defaultContext = _provider.GetService<DefaultRequestContextAccessor>()?.Context;

			if (_options.UseDefaultAccessor)
			{
				return defaultContext;
			}

			var delegateAccessor = _provider.GetService<DelegateRequestContextAccessor>();
			return delegateAccessor?.Invoke() ?? defaultContext;
		}
	}
}
