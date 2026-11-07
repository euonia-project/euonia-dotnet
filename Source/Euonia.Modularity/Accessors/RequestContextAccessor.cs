using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// <see cref="IRequestContextAccessor"/> 的实现。
/// </summary>
/// <remarks>
/// 访问器与所在作用域同生命周期，直接从该作用域解析上下文存储；
/// 不依赖环境服务访问器，也不创建兜底作用域。
/// </remarks>
public class RequestContextAccessor : IRequestContextAccessor
{
    private readonly RequestContextAccessorOptions _options;
    private readonly DefaultRequestContextAccessor? _defaultAccessor;
    private readonly DelegateRequestContextAccessor? _delegateAccessor;

    /// <summary>
    /// 初始化 <see cref="RequestContextAccessor"/> 类的新实例。
    /// </summary>
    /// <param name="options">访问器配置选项。</param>
    /// <param name="provider">当前作用域的服务提供程序。</param>
    public RequestContextAccessor(IOptions<RequestContextAccessorOptions> options, IServiceProvider provider)
    {
        _options = Check.EnsureNotNull(options, nameof(options)).Value;
        Check.EnsureNotNull(provider, nameof(provider));

        _defaultAccessor = provider.GetService<DefaultRequestContextAccessor>();
        _delegateAccessor = _options.UseDefaultAccessor ? null : provider.GetService<DelegateRequestContextAccessor>();
    }

    /// <summary>
    /// 获取当前请求上下文实例。
    /// </summary>
    /// <remarks>
    /// 委托访问器在请求流之外（如消息消费者进程）可能返回 <c>null</c>；
    /// 此时回退到默认访问器中由消息信封恢复的上下文。
    /// </remarks>
    public RequestContext? Context
    {
        get
        {
            var defaultContext = _defaultAccessor?.Context;

            if (_options.UseDefaultAccessor)
            {
                return defaultContext;
            }

            return _delegateAccessor?.Invoke() ?? defaultContext;
        }
    }
}
