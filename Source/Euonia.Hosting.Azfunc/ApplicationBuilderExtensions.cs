using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Hosting.Azfunc;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// 为 Azure Functions 工作进程提供中间件注册扩展。
/// </summary>
public static class ApplicationBuilderExtensions
{
	/// <param name="builder">函数工作进程应用构建器。</param>
	extension(IFunctionsWorkerApplicationBuilder builder)
	{
		/// <summary>
		/// 按 <see cref="EuoniaFunctionsOptions"/> 注册请求上下文与异常处理中间件。
		/// </summary>
		/// <returns>传入的构建器，以便链式调用。</returns>
		public IFunctionsWorkerApplicationBuilder UseEuoniaFunctions()
		{
			var options = builder.Services.GetRequiredService<EuoniaFunctionsOptions>();
			UseEuoniaFunctions(builder, options);
			return builder;
		}
	}

	/// <param name="application">函数应用构建器。</param>
	extension(FunctionsApplicationBuilder application)
	{
		/// <summary>
		/// 按 <see cref="EuoniaFunctionsOptions"/> 注册请求上下文与异常处理中间件。
		/// </summary>
		/// <returns>传入的函数应用构建器，以便链式调用。</returns>
		public FunctionsApplicationBuilder UseEuoniaFunctions()
		{
			var options = application.Services.GetRequiredService<EuoniaFunctionsOptions>();
			UseEuoniaFunctions(application, options);
			return application;
		}
	}

	private static void UseEuoniaFunctions(IFunctionsWorkerApplicationBuilder builder, EuoniaFunctionsOptions options)
	{
		if (options.EnableRequestTrace)
		{
			builder.UseMiddleware<RequestTraceMiddleware>();
		}

		if (options.EnableExceptionHandling)
		{
			builder.UseMiddleware<ExceptionHandlingMiddleware>();
		}

		if (options.EnableJwtAuthentication)
		{
			builder.UseMiddleware<JwtAuthenticationMiddleware>();
		}

		if (options.EnableHttpContext)
		{
			builder.UseMiddleware<HttpContextAccessorMiddleware>();
		}

		if (options.EnableRateLimiting)
		{
			builder.UseMiddleware<RateLimitingMiddleware>();
		}
	}

	private static void UseEuoniaFunctions(FunctionsApplicationBuilder builder, EuoniaFunctionsOptions options)
	{
		if (options.EnableRequestTrace)
		{
			builder.UseMiddleware<RequestTraceMiddleware>();
		}

		if (options.EnableExceptionHandling)
		{
			builder.UseMiddleware<ExceptionHandlingMiddleware>();
		}

		if (options.EnableJwtAuthentication)
		{
			builder.UseMiddleware<JwtAuthenticationMiddleware>();
		}

		if (options.EnableHttpContext)
		{
			builder.UseMiddleware<HttpContextAccessorMiddleware>();
		}

		if (options.EnableRateLimiting)
		{
			builder.UseMiddleware<RateLimitingMiddleware>();
		}
	}
}
