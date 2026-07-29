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
/// Extension methods for <see cref="IApplicationBuilder"/>.
/// </summary>
public static class ApplicationBuilderExtensions
{
	/// <param name="app"></param>
	extension(IApplicationBuilder app)
	{
		/// <summary>
		/// Set current thread culture from Accept-Language header
		/// </summary>
		/// <returns></returns>
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
		/// Use JWT token from Authorization header to set HttpContext.User
		/// </summary>
		/// <returns></returns>
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
		/// Use JWT authentication middleware
		/// </summary>
		/// <param name="schema"></param>
		/// <returns></returns>
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
		/// Use DefaultRequestContextAccessor to set RequestContext for each request.
		/// </summary>
		/// <returns></returns>
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
		/// Initialize the application.
		/// </summary>
		public void InitializeApplication()
		{
			app.ApplicationServices.GetRequiredService<ObjectAccessor<IApplicationBuilder>>().Value = app;
			var application = app.ApplicationServices.GetRequiredService<IApplicationWithServiceProvider>();
			var applicationLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();

			applicationLifetime.ApplicationStopping.Register(() =>
			{
				application.Shutdown();
			});

			applicationLifetime.ApplicationStopped.Register(() =>
			{
				application.Dispose();
			});

			application.Initialize(app.ApplicationServices);
		}
	}
}