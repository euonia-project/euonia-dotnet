using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Net.Http.Headers;
using Nerosoft.Euonia.Hosting;
using Nerosoft.Euonia.Modularity;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// 为 <see cref="IApplicationBuilder"/> 提供 Euonia Hosting 相关的中间件与应用初始化扩展。
/// </summary>
public static class ApplicationBuilderExtensions
{
	/// <summary>
	/// 定义基于 <see cref="IApplicationBuilder"/> 的扩展成员块。
	/// </summary>
	/// <param name="app">当前应用构建器实例。</param>
	extension(IApplicationBuilder app)
	{
		/// <summary>
		/// 根据请求头 <c>Accept-Language</c> 设置当前线程的区域性。
		/// </summary>
		/// <returns>当前 <see cref="IApplicationBuilder"/> 实例，用于链式调用。</returns>
		public IApplicationBuilder UseCulture()
		{
			app.Use(async (context, next) =>
			{
				if (context.Request.Headers.TryGetValue(HeaderNames.AcceptLanguage, out var values))
				{
					Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(values);
				}

				await next();
			});
			return app;
		}

		/// <summary>
		/// 从请求头中的 Bearer Token 读取声明，并直接设置到 <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/>。
		/// </summary>
		/// <returns>当前 <see cref="IApplicationBuilder"/> 实例，用于链式调用。</returns>
		/// <remarks>
		/// 该方法仅解析 JWT 中的声明，不执行签名、过期时间或颁发者等校验。
		/// </remarks>
		// ReSharper disable once UnusedMember.Local
		private IApplicationBuilder UseJwtToken()
		{
			return app.Use(async (context, next) =>
			{
				if (context.Request.Headers.TryGetValue(HeaderNames.Authorization, out var values))
				{
					if (values.Count > 0)
					{
						var value = values[0];
						if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("Bearer") && !value.Equals("Bearer null", StringComparison.OrdinalIgnoreCase))
						{
							var tokenString = value.Replace("Bearer", string.Empty).Trim();
							var handler = new JwtSecurityTokenHandler();
							var token = handler.ReadJwtToken(tokenString);
							var claims = token.Claims;
							context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "AuthenticationTypes.Federation", "name", "role"));
						}
					}
				}

				await next();
			});
		}

		/// <summary>
		/// 使用指定认证方案对当前请求执行认证，并在成功后设置 <see cref="Microsoft.AspNetCore.Http.HttpContext.User"/>。
		/// </summary>
		/// <param name="schema">要使用的认证方案名称，默认使用 JWT Bearer 方案。</param>
		/// <returns>当前 <see cref="IApplicationBuilder"/> 实例，用于链式调用。</returns>
		public IApplicationBuilder UseJwt(string schema = JwtBearerDefaults.AuthenticationScheme)
		{
			return app.Use(async (context, next) =>
			{
				if (context.User.Identity?.IsAuthenticated != true)
				{
					var result = await context.AuthenticateAsync(schema);
					if (result.Succeeded && result.Principal != null)
					{
						context.User = result.Principal;
					}
				}

				await next();
			});
		}

		/// <summary>
		/// 为每个请求创建并设置默认的请求上下文访问器内容。
		/// </summary>
		/// <returns>当前 <see cref="IApplicationBuilder"/> 实例，用于链式调用。</returns>
		public IApplicationBuilder UseDefaultRequestContextAccessor()
		{
			return app.Use((httpContext, next) =>
			{
				var accessor = httpContext.RequestServices.GetService<DefaultRequestContextAccessor>();
				accessor?.Context = RequestContext.From(httpContext);
				return next();
			});
		}

		/// <summary>
		/// 初始化 Euonia 应用，并注册应用生命周期回调。
		/// </summary>
		public void InitializeApplication()
		{
			app.ApplicationServices.GetRequiredService<ObjectAccessor<IApplicationBuilder>>().Value = app;
			var application = app.ApplicationServices.GetRequiredService<IApplicationWithServiceProvider>();
			var applicationLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

			applicationLifetime.ApplicationStopping.Register(application.Shutdown);

			applicationLifetime.ApplicationStopped.Register(application.Dispose);

			application.Initialize(app.ApplicationServices);
		}
	}
}