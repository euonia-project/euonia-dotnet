using System.IdentityModel.Tokens.Jwt;
using Duende.AspNetCore.Authentication.OAuth2Introspection;
using Duende.AspNetCore.Authentication.OAuth2Introspection.Infrastructure;
using Duende.IdentityModel.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// IdentityServer 认证的选项。
/// </summary>
/// <seealso cref="AuthenticationSchemeOptions" />
public class IdentityServerAuthenticationOptions : AuthenticationSchemeOptions
{
	/// <summary>
	/// 从当前请求上下文中提取内部令牌的委托。
	/// </summary>
	private static readonly Func<HttpRequest, string> _internalTokenRetriever = request => request.HttpContext.Items[IdentityServerAuthenticationDefaults.TokenItemsKey] as string;

	/// <summary>
	/// 令牌颁发者的基地址。
	/// </summary>
	public string Authority { get; set; }

	/// <summary>
	/// 指定发现端点是否要求 HTTPS。
	/// </summary>
	public bool RequireHttpsMetadata { get; set; } = true;

	/// <summary>
	/// 指定支持的令牌类型（JWT、引用令牌或两者）。
	/// </summary>
	public SupportedTokens SupportedTokens { get; set; } = SupportedTokens.Both;

	/// <summary>
	/// 用于从传入请求中检索令牌的回调。
	/// </summary>
	public Func<HttpRequest, string> TokenRetriever { get; set; } = TokenRetrieval.FromAuthorizationHeader();

	/// <summary>
	/// 用于向内省端点进行身份验证的 API 资源名称。
	/// </summary>
	public string ApiName { get; set; }

	/// <summary>
	/// 用于向内省端点进行身份验证的密钥。
	/// </summary>
	public string ApiSecret { get; set; }

	/// <summary>
	/// 当此 API 由 IdentityServer3 保护且需要同时支持 JWT 和引用令牌时启用。
	/// 启用后，应对传入的 JWT 添加作用域（scope）验证。
	/// </summary>
	public bool LegacyAudienceValidation { get; set; } = false;

	/// <summary>
	/// 名称的声明类型。
	/// </summary>
	public string NameClaimType { get; set; } = "name";

	/// <summary>
	/// 角色的声明类型。
	/// </summary>
	public string RoleClaimType { get; set; } = "role";

	/// <summary>
	/// 指定是否启用内省响应的缓存（需要分布式缓存实现）。
	/// </summary>
	public bool EnableCaching { get; set; } = false;

	/// <summary>
	/// 指定内省响应缓存的生存时间（TTL）。
	/// </summary>
	public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(10);

	/// <summary>
	/// 指定缓存键（令牌）的前缀。
	/// </summary>
	public string CacheKeyPrefix { get; set; } = string.Empty;

	/// <summary>
	/// 获取或设置内省发现文档的策略。
	/// </summary>
	/// <value>
	/// 内省发现策略。
	/// </value>
	public DiscoveryPolicy IntrospectionDiscoveryPolicy { get; set; } = new();

	/// <summary>
	/// 指定令牌是否应保存在认证属性中。
	/// </summary>
	public bool SaveToken { get; set; } = true;

	/// <summary>
	/// 指定验证 JWT 令牌时允许的时钟偏差。
	/// </summary>
	public TimeSpan? JwtValidationClockSkew { get; set; }

	// todo: 切换到工厂方式
	/// <summary>
	/// JWT 中间件的后通道处理器。
	/// </summary>
	public HttpMessageHandler JwtBackChannelHandler { get; set; }

	/// <summary>
	/// 后通道操作的超时时间。
	/// </summary>
	public TimeSpan BackChannelTimeouts { get; set; } = TimeSpan.FromSeconds(60);

	// todo
	/// <summary>
	/// JWT 中间件的事件。
	/// </summary>
	public JwtBearerEvents JwtBearerEvents { get; set; } = new();

	/// <summary>
	/// 内省端点的事件。
	/// </summary>
	public OAuth2IntrospectionEvents OAuth2IntrospectionEvents { get; set; } = new();

	/// <summary>
	/// 指定缓存发现文档的副本应多久刷新一次。
	/// 如果未设置，则使用 Microsoft 底层配置管理器的默认值（目前为 24 小时）。
	/// 如果需要更精细的控制，请在 JWT 选项上提供自定义配置管理器。
	/// </summary>
	public TimeSpan? DiscoveryDocumentRefreshInterval { get; set; }

	/// <summary>
	/// 获取一个值，指示是否支持 JWT。
	/// </summary>
	public bool SupportsJwt => SupportedTokens is SupportedTokens.Jwt or SupportedTokens.Both;

	/// <summary>
	/// 获取一个值，指示是否支持引用令牌。
	/// </summary>
	public bool SupportsIntrospection => SupportedTokens is SupportedTokens.Reference or SupportedTokens.Both;

	/// <summary>
	/// 配置 JWT Bearer 选项：设置颁发者、HTTPS 要求、事件、发现文档管理器、受众验证与令牌验证参数等。
	/// </summary>
	/// <param name="jwtOptions">JWT Bearer 选项。</param>
	internal void ConfigureJwtBearer(JwtBearerOptions jwtOptions)
	{
		jwtOptions.Authority = Authority;
		jwtOptions.RequireHttpsMetadata = RequireHttpsMetadata;
		jwtOptions.BackchannelTimeout = BackChannelTimeouts;
		jwtOptions.RefreshOnIssuerKeyNotFound = true;
		jwtOptions.SaveToken = SaveToken;

		jwtOptions.Events = new JwtBearerEvents
		{
			OnMessageReceived = e =>
			{
				e.Token = _internalTokenRetriever(e.Request);
				return JwtBearerEvents.MessageReceived(e);
			},

			OnTokenValidated = e => JwtBearerEvents.TokenValidated(e),
			OnAuthenticationFailed = e => JwtBearerEvents.AuthenticationFailed(e),
			OnChallenge = e => JwtBearerEvents.Challenge(e)
		};

		if (DiscoveryDocumentRefreshInterval.HasValue)
		{
			var parsedUrl = DiscoveryEndpoint.ParseUrl(Authority);

			var httpClient = new HttpClient(JwtBackChannelHandler ?? new HttpClientHandler())
			{
				Timeout = BackChannelTimeouts,
				MaxResponseContentBufferSize = 1024 * 1024 * 10 // 10 MB
			};

			var manager = new ConfigurationManager<OpenIdConnectConfiguration>(
				parsedUrl.Url,
				new OpenIdConnectConfigurationRetriever(),
				new HttpDocumentRetriever(httpClient) { RequireHttps = RequireHttpsMetadata })
			{
				AutomaticRefreshInterval = DiscoveryDocumentRefreshInterval.Value
			};

			jwtOptions.ConfigurationManager = manager;
		}

		if (JwtBackChannelHandler != null)
		{
			jwtOptions.BackchannelHttpHandler = JwtBackChannelHandler;
		}

		// 如果设置了 API 名称，则执行严格的受众检查
		if (!string.IsNullOrWhiteSpace(ApiName) && !LegacyAudienceValidation)
		{
			jwtOptions.Audience = ApiName;
		}
		else
		{
			// 不进行受众验证，仅依赖作用域检查
			jwtOptions.TokenValidationParameters.ValidateAudience = false;
		}

		jwtOptions.TokenValidationParameters.NameClaimType = NameClaimType;
		jwtOptions.TokenValidationParameters.RoleClaimType = RoleClaimType;

		if (JwtValidationClockSkew.HasValue)
		{
			jwtOptions.TokenValidationParameters.ClockSkew = JwtValidationClockSkew.Value;
		}

		var handler = new JwtSecurityTokenHandler
		{
			MapInboundClaims = false
		};

#pragma warning disable CS0618 // 类型或成员已过时
		jwtOptions.SecurityTokenValidators.Clear();
		jwtOptions.SecurityTokenValidators.Add(handler);
#pragma warning restore CS0618 // 类型或成员已过时
	}

	/// <summary>
	/// 配置 OAuth2 内省选项：设置颁发者、客户端凭据、令牌检索器、缓存与事件等。若未配置 ApiSecret 则直接返回。
	/// </summary>
	/// <param name="introspectionOptions">OAuth2 内省选项。</param>
	internal void ConfigureIntrospection(OAuth2IntrospectionOptions introspectionOptions)
	{
		if (string.IsNullOrWhiteSpace(ApiSecret))
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(ApiName))
		{
			throw new ArgumentException("ApiName must be configured if ApiSecret is set.");
		}

		introspectionOptions.Authority = Authority;
		introspectionOptions.ClientId = ApiName;
		introspectionOptions.ClientSecret = ApiSecret;
		introspectionOptions.NameClaimType = NameClaimType;
		introspectionOptions.RoleClaimType = RoleClaimType;
		introspectionOptions.TokenRetriever = _internalTokenRetriever;
		introspectionOptions.SaveToken = SaveToken;
		introspectionOptions.DiscoveryPolicy = IntrospectionDiscoveryPolicy;

		//introspectionOptions.EnableCaching = EnableCaching;
		introspectionOptions.CacheDuration = CacheDuration;
		introspectionOptions.CacheKeyPrefix = CacheKeyPrefix;

		introspectionOptions.DiscoveryPolicy.RequireHttps = RequireHttpsMetadata;

		introspectionOptions.Events = new OAuth2IntrospectionEvents
		{
			OnAuthenticationFailed = e => OAuth2IntrospectionEvents.AuthenticationFailed(e),
			OnTokenValidated = e => OAuth2IntrospectionEvents.OnTokenValidated(e),
		};
	}
}