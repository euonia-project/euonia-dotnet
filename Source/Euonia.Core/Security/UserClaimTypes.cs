// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Nerosoft.Euonia.Security;

/// <summary>
/// 用户声明类型常量定义。
/// </summary>
public static class UserClaimTypes
{
	/// <summary>最终用户在其颁发者处的唯一标识符。</summary>
	public const string Subject = "sub";

	/// <summary>最终用户的全名，以可显示的形式包含所有名称部分，可能包括头衔和后缀，按照最终用户的地区和偏好排序。</summary>
	public const string Name = "name";

	/// <summary>最终用户的名字（Given name）。注意，在某些文化中，人们可能有多个名字；所有名字均可出现，以空格字符分隔。</summary>
	public const string GivenName = "given_name";

	/// <summary>最终用户的姓氏（Family name）。注意，在某些文化中，人们可能有多个姓氏或没有姓氏；所有姓氏均可出现，以空格字符分隔。</summary>
	public const string FamilyName = "family_name";

	/// <summary>最终用户的中间名（Middle name）。注意，在某些文化中，人们可能有多个中间名；所有中间名均可出现，以空格字符分隔。同时注意，在某些文化中不使用中间名。</summary>
	public const string MiddleName = "middle_name";

	/// <summary>最终用户的别名，可能与 given_name 相同也可能不同。例如，Mike 的昵称值可能会与 Michael 的 given_name 值一起返回。</summary>
	public const string NickName = "nickname";

	/// <summary>最终用户希望在依赖方处使用的简写名称，如 janedoe 或 j.doe。此值可以是任何有效的 JSON 字符串，包括 @、/ 或空格等特殊字符。依赖方不得依赖此值的唯一性。</summary>
	/// <remarks>依赖方不得依赖此值的唯一性，参见 http://openid.net/specs/openid-connect-basic-1_0-32.html#ClaimStability。</remarks>
	public const string PreferredUserName = "preferred_username";

	/// <summary>最终用户个人资料页面的 URL。此网页的内容应与最终用户相关。</summary>
	public const string Profile = "profile";

	/// <summary>最终用户头像图片的 URL。此 URL 必须指向图像文件（例如 PNG、JPEG 或 GIF 图像文件），而不是包含图像的网页。</summary>
	/// <remarks>注意，此 URL 应该专门引用适合在描述最终用户时显示的个人资料照片，而不是最终用户拍摄的任意照片。</remarks>
	public const string Picture = "picture";

	/// <summary>最终用户网页或博客的 URL。此网页应包含由最终用户或最终用户所属组织发布的信息。</summary>
	public const string WebSite = "website";

	/// <summary>最终用户的首选电子邮件地址。其值必须符合 RFC 5322 [RFC5322] addr-spec 语法。依赖方不得依赖此值的唯一性。</summary>
	public const string Email = "email";

	/// <summary>如果最终用户的电子邮件地址已经过验证，则为 "true"；否则为 "false"。</summary>
	/// <remarks>当此声明值为 "true" 时，表示 OP 采取了确认措施以确保在进行验证时该电子邮件地址由最终用户控制。验证电子邮件地址的方式取决于具体上下文，并依赖于各方运作所在的信任框架或合同协议。</remarks>
	public const string EmailVerified = "email_verified";

	/// <summary>最终用户的性别。此规范定义的值有 "female" 和 "male"。当定义的两种值都不适用时，可以使用其他值。</summary>
	public const string Gender = "gender";

	/// <summary>最终用户的生日，以 ISO 8601:2004 [ISO8601‑2004] YYYY-MM-DD 格式表示。年份可以为 0000，表示省略。如果仅表示年份，允许使用 YYYY 格式。注意，根据底层平台日期相关函数的不同，仅提供年份可能导致月份和日期发生变化，因此实现者需要考虑此因素以正确处理日期。</summary>
	public const string BirthDate = "birthdate";

	/// <summary>来自时区数据库（http://www.twinsun.com/tz/tz-link.htm）的字符串，表示最终用户的时区。例如，Europe/Paris 或 America/Los_Angeles。</summary>
	public const string ZoneInfo = "zoneinfo";

	/// <summary>最终用户的区域设置，以 BCP47 [RFC5646] 语言标签表示。通常为小写的 ISO 639-1 Alpha-2 [ISO639‑1] 语言代码和大写的 ISO 3166-1 Alpha-2 [ISO3166‑1] 国家代码，用连字符分隔。例如，en-US 或 fr-CA。作为兼容性说明，某些实现使用下划线而非连字符作为分隔符，例如 en_US；依赖方可以选择也接受此区域语法。</summary>
	public const string Locale = "locale";

	/// <summary>最终用户的首选电话号码。建议使用 E.164（https://www.itu.int/rec/T-REC-E.164/e）格式作为此声明的格式，例如 +1 (425) 555-1212 或 +56 (2) 687 2400。如果电话号码包含分机号，建议使用 RFC 3966 [RFC3966] 分机语法表示，例如 +1 (604) 555-1234;ext=5678。</summary>
	public const string PhoneNumber = "phone_number";

	/// <summary>如果最终用户的电话号码已经过验证，则为 true；否则为 false。当此声明值为 true 时，表示 OP 采取了确认措施以确保在进行验证时该电话号码由最终用户控制。</summary>
	/// <remarks>验证电话号码的方式取决于具体上下文，并依赖于各方运作所在的信任框架或合同协议。当为 true 时，phone_number 声明必须为 E.164 格式，任何分机号必须以 RFC 3966 格式表示。</remarks>
	public const string PhoneNumberVerified = "phone_number_verified";

	/// <summary>最终用户的首选邮政地址。address 成员的值是一个 JSON 结构，包含 http://openid.net/specs/openid-connect-basic-1_0-32.html#AddressClaim 中定义的部分或全部成员。</summary>
	public const string Address = "address";

	/// <summary>此 ID Token 的目标受众。它必须包含依赖方的 OAuth 2.0 client_id 作为受众值。它还可以包含其他受众的标识符。通常情况下，aud 值是一个区分大小写的字符串数组。在只有一个受众的常见特殊情况下，aud 值可以是单个区分大小写的字符串。</summary>
	public const string Audience = "aud";

	/// <summary>响应颁发者的颁发者标识符。iss 值是使用 https 协议的区分大小写的 URL，包含协议、主机以及可选的端口号和路径组件，不包含查询或片段组件。</summary>
	public const string Issuer = "iss";

	/// <summary>JWT 在此时间之前不得被接受处理，指定为从 1970-01-01T0:0:0Z 起的秒数。</summary>
	public const string NotBefore = "nbf";

	/// <summary>exp（过期时间）声明标识了在此时间或之后令牌不得被接受处理的过期时间，指定为从 1970-01-01T0:0:0Z 起的秒数。</summary>
	public const string Expiration = "exp";

	/// <summary>最终用户信息最后更新的时间。其值为 JSON 数字，表示从 1970-01-01T0:0:0Z 起以 UTC 测量的秒数。</summary>
	public const string UpdatedAt = "updated_at";

	/// <summary>iat（签发时间）声明标识了 JWT 签发的时间，指定为从 1970-01-01T0:0:0Z 起的秒数。</summary>
	public const string IssuedAt = "iat";

	/// <summary>身份验证方法引用。JSON 字符串数组，是身份验证中使用的身份验证方法的标识符。</summary>
	public const string AuthenticationMethod = "amr";

	/// <summary>会话标识符。这表示 OP 在 RP 处为用户代理或已登录最终用户设备建立的会话。其内容对 OP 唯一，对 RP 不透明。</summary>
	public const string SessionId = "sid";

	/// <summary>
	/// 身份验证上下文类引用。指定一个标识身份验证上下文类引用值的字符串，该值标识了所执行的身份验证满足的身份验证上下文类。
	/// 值 "0" 表示最终用户身份验证未满足 ISO/IEC 29115 级别 1 的要求。
	/// 例如，使用长期浏览器 Cookie 进行身份验证是适合使用 "level 0" 的一个例子。
	/// 级别 0 的身份验证不应用于授权访问任何具有货币价值的资源。
	/// （对应于 OpenID 2.0 PAPE nist_auth_level 0。）
	/// 应使用绝对 URI 或 RFC 6711 注册名称作为 acr 值；注册名称不得用于与其注册含义不同的含义。
	/// 使用此声明的各方需要就所用值的含义达成一致，这可能取决于具体上下文。
	/// acr 值是一个区分大小写的字符串。
	/// </summary>
	public const string AuthenticationContextClassReference = "acr";

	/// <summary>最终用户身份验证发生的时间。其值为 JSON 数字，表示从 1970-01-01T0:0:0Z 起以 UTC 测量的秒数。当发出 max_age 请求或将 auth_time 作为基本声明请求时，此声明为必需项；否则，其包含是可选的。</summary>
	public const string AuthenticationTime = "auth_time";

	/// <summary>ID Token 被颁发给的参与方。如果存在，它必须包含该参与方的 OAuth 2.0 Client ID。此声明仅在 ID Token 只有一个受众值且该受众与授权方不同时才需要。即使授权方与唯一受众相同，也可以包含它。azp 值是一个区分大小写的字符串，包含 StringOrURI 值。</summary>
	public const string AuthorizedParty = "azp";

	/// <summary>访问令牌哈希值。其值是 access_token 值 ASCII 表示形式的八位字节哈希的最左半部分的 base64url 编码，使用的哈希算法是 ID Token 的 JOSE 头中 alg 头参数使用的哈希算法。例如，如果 alg 是 RS256，则用 SHA-256 哈希 access_token 值，然后取最左 128 位并进行 base64url 编码。at_hash 值是一个区分大小写的字符串。</summary>
	public const string AccessTokenHash = "at_hash";

	/// <summary>授权码哈希值。其值是 code 值 ASCII 表示形式的八位字节哈希的最左半部分的 base64url 编码，使用的哈希算法是 ID Token 的 JOSE 头中 alg 头参数使用的哈希算法。例如，如果 alg 是 HS512，则用 SHA-512 哈希 code 值，然后取最左 256 位并进行 base64url 编码。c_hash 值是一个区分大小写的字符串。</summary>
	public const string AuthorizationCodeHash = "c_hash";

	/// <summary>状态哈希值。其值是 state 值 ASCII 表示形式的八位字节哈希的最左半部分的 base64url 编码，使用的哈希算法是 ID Token 的 JOSE 头中 alg 头参数使用的哈希算法。例如，如果 alg 是 HS512，则用 SHA-512 哈希 code 值，然后取最左 256 位并进行 base64url 编码。c_hash 值是一个区分大小写的字符串。</summary>
	public const string StateHash = "s_hash";

	/// <summary>用于将客户端会话与 ID Token 关联并缓解重放攻击的字符串值。该值从身份验证请求到 ID Token 不经修改地传递。如果 ID Token 中存在该值，客户端必须验证 nonce 声明值等于身份验证请求中发送的 nonce 参数值。如果身份验证请求中存在该值，授权服务器必须在 ID Token 中包含一个 nonce 声明，其声明值为身份验证请求中发送的 nonce 值。授权服务器不应对使用的 nonce 值执行其他处理。nonce 值是一个区分大小写的字符串。</summary>
	public const string Nonce = "nonce";

	/// <summary>JWT ID。令牌的唯一标识符，可用于防止令牌重用。这些令牌只能使用一次，除非各方之间协商了重用条件；任何此类协商超出了本规范的范围。</summary>
	public const string JwtId = "jti";

	/// <summary>定义一组事件语句，每个语句可以添加额外的声明来完整描述一个已发生的单一逻辑事件。</summary>
	public const string Events = "events";

	/// <summary>在授权服务器处有效的 OAuth 2.0 客户端标识符。</summary>
	public const string ClientId = "client_id";

	/// <summary>OpenID Connect 请求必须包含 "openid" scope 值。如果 openid scope 值不存在，则行为完全未指定。可以存在其他 scope 值。实现不理解的 scope 值应被忽略。</summary>
	public const string Scope = "scope";

	/// <summary>"act"（actor）声明在 JWT 中提供了一种表示已发生委托并标识被授予权限的操作方的方式。"act" 声明值是一个 JSON 对象，JSON 对象中的成员是标识 actor 的声明。构成 "act" 声明的声明标识并可能提供有关 actor 的额外信息。</summary>
	public const string Actor = "act";

	/// <summary>"may_act" 声明表明一方被授权成为 actor 并代表另一方行事。声明值是一个 JSON 对象，JSON 对象中的成员是标识被声明为有资格代表包含该声明的 JWT 所标识方行事的声明。</summary>
	public const string MayAct = "may_act";

	/// <summary>标识符。</summary>
	public const string Id = "id";

	/// <summary>身份提供者。</summary>
	public const string IdentityProvider = "idp";

	/// <summary>角色。</summary>
	public const string Role = "role";

	/// <summary>引用令牌标识符。</summary>
	public const string ReferenceTokenId = "reference_token_id";

	/// <summary>确认。</summary>
	public const string Confirmation = "cnf";

	/// <summary>用户唯一编码。</summary>
	public const string Code = "code";

	/// <summary>用户租户 ID。</summary>
	public const string Tenant = "tenant";

	/// <summary>方案。</summary>
	public const string Scheme = "scheme";
}