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
			return () => RequestContext.From(provider.GetService<IHttpContextAccessor>()?.HttpContext);
		});
		context.Services.AddScopeTransformation();
		context.Services.AddUserPrincipal();
		context.Services.AddObjectAccessor<IApplicationBuilder>();
		context.Services.AddTransient<ExceptionHandlingMiddleware>();
	}

	/// <summary>
	/// 应用程序初始化完成后执行：注册请求追踪中间件，并为 <see cref="IServiceAccessor"/> 设置服务提供程序。
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

		// 为 IServiceAccessor 设置 ServiceProvider。
		app.Use((httpContext, next) =>
		   {
			   var accessor = httpContext.RequestServices.GetService<IServiceAccessor>();
			   accessor?.ServiceProvider = httpContext.RequestServices;
			   return next();
		   })
		   .UseDefaultRequestContextAccessor();
	}
}