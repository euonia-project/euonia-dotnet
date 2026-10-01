using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Security.Tests;

/// <summary>
/// 测试里对守卫的<b>预热</b>。
/// </summary>
/// <remarks>
/// 引擎的同步读只读「已解析的快照」，冷缓存时抛错、不阻塞（见 <c>IScopeGuard</c>）。
/// 因此凡是要做同步判定的用例，都必须先在某个异步入口解析一次——测试里就是这里。
/// 未注册解析器的场景不预热：那种情况下要断言的正是「解析时的启动校验会报错」。
/// </remarks>
internal static class TestGuard
{
	/// <summary>
	/// 解析一次授权数据，供后续同步判定使用。
	/// </summary>
	/// <param name="provider">服务提供程序。</param>
	/// <returns>原 <paramref name="provider"/>，便于链式调用。</returns>
	public static ServiceProvider Warm(this ServiceProvider provider)
	{
		if (provider.GetService<IScopeSubjectResolver>() == null)
		{
			return provider;
		}

		try
		{
			provider.GetRequiredService<IScopeGuard>().EnsureResolvedAsync().AsTask().GetAwaiter().GetResult();
		}
		catch (InvalidOperationException)
		{
			// 启动期校验失败（缺 UserPrincipal 之类）：把异常留给用例自己去触发与断言，
			// 预热本身不改变它们的结论。
		}

		return provider;
	}
}
