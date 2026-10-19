using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Nerosoft.Euonia.Modularity;

namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// 用于配置托管环境的模块。
/// </summary>
public class HostingModule : ModuleContextBase
{
	/// <summary>
	/// 配置服务：注册请求上下文访问器、作用域转换、用户主体、应用程序构建器访问器及异常处理中间件。
	/// </summary>
	/// <param name="context">服务配置上下文。</param>
	public override void ConfigureServices(ServiceConfigurationContext context)
	{
		context.Services.TryAddScoped<DefaultRequestContextAccessor>();
		context.Services.TryAddScoped<DelegateRequestContextAccessor>(provider =>
		{
			// 请求流之外（如 broker 消费者回调）没有 HttpContext，返回 null 由访问器回退处理。
			return () => provider.GetService<IHttpContextAccessor>()?.HttpContext is { } httpContext
				              ? RequestContext.From(httpContext)
				              : null;
		});
		context.Services.AddScopeTransformation();
		context.Services.AddUserPrincipal();
		context.Services.AddObjectAccessor<IApplicationBuilder>();
		context.Services.AddTransient<ExceptionHandlingMiddleware>();
	}

	/// <summary>
	/// 应用程序初始化完成后执行：注册请求追踪中间件，并把请求上下文安装到请求作用域。
	/// </summary>
	/// <param name="context">应用程序初始化上下文。</param>
	public override void OnApplicationInitialization(ApplicationInitializationContext context)
	{
		base.OnApplicationInitialization(context);
		var app = context.GetApplicationBuilder();

		if (app == null)
		{
			return;
		}

		app.UseMiddleware<RequestTraceMiddleware>();

		// 请求上下文安装到请求作用域的访问器实例；服务访问器已是作用域服务，无需额外绑定。
		app.UseDefaultRequestContextAccessor();
	}
}