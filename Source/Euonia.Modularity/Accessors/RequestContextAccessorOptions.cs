namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// <see cref="RequestContextAccessor"/> 的选项。
/// </summary>
public class RequestContextAccessorOptions
{
	/// <summary>
    /// 获取或设置一个值，该值指示使用默认的请求上下文访问器还是委托访问器。
    /// </summary>
	public bool UseDefaultAccessor { get; set; } = true;
}