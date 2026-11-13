using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 租户中间件注册扩展。
/// </summary>
public static class TenancyApplicationBuilderExtensions
{
	/// <summary>
	/// 安装租户解析与校验中间件；应位于 <c>UseRouting</c> 之后、<c>UseAuthorization</c> 之前。
	/// </summary>
	/// <param name="app">应用构建器。</param>
	/// <returns>应用构建器。</returns>
	/// <remarks>
	/// 必须放在 <c>UseAuthentication()</c> 之后，否则 Header 解析拿不到已认证身份，
	/// 默认 <c>TrustHeaderForAnonymous=false</c> 时会忽略租户头。
	/// </remarks>
	public static IApplicationBuilder UseTenancy(this IApplicationBuilder app)
	{
		ArgumentNullException.ThrowIfNull(app);

		var options = app.ApplicationServices.GetService<IOptions<MultiTenancyOptions>>()?.Value;
		return options is null or { Enabled: false } ? app : app.UseMiddleware<TenancyMiddleware>();
	}
}
