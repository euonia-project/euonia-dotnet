using Microsoft.AspNetCore.Builder;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 执行上下文中间件注册扩展。
/// </summary>
public static class ExecutionContextApplicationBuilderExtensions
{
	/// <summary>
	/// 安装执行上下文中间件；必须位于 <c>UseAuthentication</c> 之后。
	/// </summary>
	/// <param name="app">应用构建器。</param>
	/// <returns>应用构建器。</returns>
	public static IApplicationBuilder UseExecutionContext(this IApplicationBuilder app)
	{
		ArgumentNullException.ThrowIfNull(app);
		return app.UseMiddleware<ExecutionContextMiddleware>();
	}
}
