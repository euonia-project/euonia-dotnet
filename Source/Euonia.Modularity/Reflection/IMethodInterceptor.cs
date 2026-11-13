namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 方法拦截器的契约接口。
/// </summary>
public interface IMethodInterceptor
{
	/// <summary>
	/// 拦截方法调用。
	/// </summary>
	/// <param name="invocation">要拦截的方法调用信息。</param>
	/// <returns>表示异步拦截操作的任务。</returns>
    Task InterceptAsync(IMethodInvocation invocation);
}