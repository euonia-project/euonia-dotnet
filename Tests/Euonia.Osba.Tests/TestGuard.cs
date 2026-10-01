using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Tests;

/// <summary>
/// 测试里对守卫的<b>预热</b>。
/// </summary>
/// <remarks>
/// 引擎的同步读只读「已解析的快照」，冷缓存时抛错、不阻塞（见 <c>IScopeGuard</c>）。
/// 生产代码里这一步由入口负责（工厂的异步入口 <c>await</c> 预热，同步入口在
/// <c>AuthorizationWarmup</c> 里阻塞一次）；测试里就是这里。
/// </remarks>
internal static class TestGuard
{
	/// <summary>
	/// 解析一次授权数据，供后续同步判定使用。
	/// </summary>
	/// <param name="provider">作用域的服务提供程序。</param>
	/// <returns>原 <paramref name="provider"/>，便于链式调用。</returns>
	public static IServiceProvider Warm(this IServiceProvider provider)
	{
		var guard = provider.GetService<IScopeGuard>();

		if (guard != null && provider.GetService<IScopeSubjectResolver>() != null)
		{
			guard.EnsureResolvedAsync().AsTask().GetAwaiter().GetResult();
		}

		return provider;
	}
}
