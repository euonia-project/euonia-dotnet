using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// 用于验证 JWT 和引用令牌（reference token）的认证处理程序。
/// </summary>
public class IdentityServerAuthenticationHandler : AuthenticationHandler<IdentityServerAuthenticationOptions>
{
	/// <summary>
	/// 日志记录器实例。
	/// </summary>
	private readonly ILogger _logger;

#pragma warning disable CS0618 // 类型或成员已过时
	/// <summary>
	/// 初始化 <see cref="IdentityServerAuthenticationHandler"/> 的新实例。
	/// </summary>
	/// <param name="options">IdentityServer 认证选项监视器。</param>
	/// <param name="logger">日志工厂。</param>
	/// <param name="encoder">URL 编码器。</param>
	/// <param name="clock">系统时钟。</param>
	public IdentityServerAuthenticationHandler(IOptionsMonitor<IdentityServerAuthenticationOptions> options,
	                                           ILoggerFactory logger,
	                                           UrlEncoder encoder,
	                                           ISystemClock clock)
		: base(options, logger, encoder, clock)
	{
		_logger = logger.CreateLogger<IdentityServerAuthenticationHandler>();
	}
#pragma warning restore CS0618 // 类型或成员已过时

	/// <summary>
	/// 尝试验证当前请求中的令牌。根据令牌格式（JWT 或引用令牌）选择对应的子方案进行认证。
	/// </summary>
	/// <returns>认证结果。</returns>
	protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
	{
		_logger.LogTrace("HandleAuthenticateAsync called");

		var jwtScheme = Scheme.Name + IdentityServerAuthenticationDefaults.JwtAuthenticationScheme;
		var introspectionScheme = Scheme.Name + IdentityServerAuthenticationDefaults.IntrospectionAuthenticationScheme;

		var token = Options.TokenRetriever(Context.Request);
		var removeToken = false;

		try
		{
			if (token != null)
			{
				_logger.LogTrace("Token found: {Token}", token);

				removeToken = true;
				Context.Items.Add(IdentityServerAuthenticationDefaults.TokenItemsKey, token);

				// 似乎是 JWT
				if (token.Contains('.') && Options.SupportsJwt)
				{
					_logger.LogTrace("Token is a JWT and is supported");


					Context.Items.Add(IdentityServerAuthenticationDefaults.EffectiveSchemeKey + Scheme.Name,
						jwtScheme);
					return await Context.AuthenticateAsync(jwtScheme);
				}
				else if (Options.SupportsIntrospection)
				{
					_logger.LogTrace("Token is a reference token and is supported");

					Context.Items.Add(IdentityServerAuthenticationDefaults.EffectiveSchemeKey + Scheme.Name,
						introspectionScheme);
					return await Context.AuthenticateAsync(introspectionScheme);
				}
				else
				{
					_logger.LogTrace(
						"Neither JWT nor reference tokens seem to be correctly configured for incoming token");
				}
			}

			// 如果支持 JWT，则将默认质询处理程序设置为 JwtBearer
			if (Options.SupportsJwt)
			{
				Context.Items.Add(IdentityServerAuthenticationDefaults.EffectiveSchemeKey + Scheme.Name, jwtScheme);
			}

			return AuthenticateResult.NoResult();
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "{Message}", ex.Message);
			return AuthenticateResult.Fail(ex);
		}
		finally
		{
			if (removeToken)
			{
				Context.Items.Remove(IdentityServerAuthenticationDefaults.TokenItemsKey);
			}
		}
	}

	/// <summary>
	/// 处理 401 质询。若存在有效的生效方案，则将质询转发到该方案；否则调用基类默认行为。
	/// </summary>
	/// <param name="properties">认证属性。</param>
	/// <returns>表示异步质询处理的任务。</returns>
	protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
	{
		if (Context.Items.TryGetValue(IdentityServerAuthenticationDefaults.EffectiveSchemeKey + Scheme.Name, out object value))
		{
			if (value is string scheme)
			{
				_logger.LogTrace("Forwarding challenge to scheme: {Scheme}", scheme);
				await Context.ChallengeAsync(scheme);
			}
		}
		else
		{
			await base.HandleChallengeAsync(properties);
		}
	}
}