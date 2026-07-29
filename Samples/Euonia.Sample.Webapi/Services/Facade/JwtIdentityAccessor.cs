using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;

namespace Nerosoft.Euonia.Sample.Facade;

/// <summary>
/// Resolves user identities from JWT authorization headers.
/// </summary>
internal class JwtIdentityAccessor
{
	/// <summary>
	/// Resolves an <see cref="IPrincipal"/> from the provided JWT authorization header.
	/// </summary>
	/// <param name="jwt">
	/// The authorization header value expected in the form "Bearer {token}".
	/// If <c>null</c> or not containing a token the method returns an empty principal.
	/// </param>
	/// <param name="configuration">
	/// The application's <see cref="IConfiguration"/> instance. The method reads the
	/// following settings from the <c>JwtAuthenticationOptions</c> section:
	/// <list type="bullet">
	/// <item><description><c>SigningKey</c> (string) — symmetric signing key.</description></item>
	/// <item><description><c>Issuer</c> (string[]) — valid issuers.</description></item>
	/// <item><description><c>ValidateIssuer</c> (bool) — whether to validate issuer.</description></item>
	/// <item><description><c>ValidateAudience</c> (bool) — whether to validate audience.</description></item>
	/// </list>
	/// </param>
	/// <returns>
	/// A validated <see cref="ClaimsPrincipal"/> when the token is valid; otherwise an empty <see cref="ClaimsPrincipal"/>.
	/// The returned instance implements <see cref="IPrincipal"/>.
	/// </returns>
	/// <remarks>
	/// The method extracts the token using a regular expression that looks for the
	/// prefix "Bearer ". Token validation is performed with <see cref="JwtSecurityTokenHandler"/>.
	/// All validation exceptions are swallowed and an empty principal is returned on failure.
	/// </remarks>
	public static IPrincipal Resolve(string jwt, IConfiguration configuration)
	{
		var token = Regex.Match(jwt ?? "Bearer ", @"^Bearer\s+(.*)").Groups[1].Value;
		ClaimsPrincipal principal;
		if (string.IsNullOrWhiteSpace(token))
		{
			principal = new ClaimsPrincipal(); //GenericPrincipal(null, null);
		}
		else
		{
			const string prefix = "JwtAuthenticationOptions";

			var signingKey = configuration.GetValue<string>($"{prefix}:SigningKey");
			var key = Encoding.UTF8.GetBytes(signingKey);

			var validation = new TokenValidationParameters
			{
				NameClaimType = ClaimTypes.Name,
				RoleClaimType = ClaimTypes.Role,
				ValidIssuers = configuration.GetSection($"{prefix}:Issuer").Get<string[]>(),
				ValidateIssuer = configuration.GetValue<bool>($"{prefix}:ValidateIssuer"),
				ValidateAudience = configuration.GetValue<bool>($"{prefix}:ValidateAudience"),
				IssuerSigningKey = new SymmetricSecurityKey(key)
			};
			try
			{
				principal = new JwtSecurityTokenHandler().ValidateToken(token, validation, out _);
			}
			catch
			{
				principal = new ClaimsPrincipal();
			}
		}

		return principal;
	}
}
