using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// The jwt token validation class.
/// </summary>
public class TokenValidation
{
    /// <summary>
    /// Provides a forwarding func for JWT vs reference tokens (based on existence of dot in token)
    /// </summary>
    /// <param name="introspectionScheme">Scheme name of the introspection handler</param>
    /// <returns></returns>
    public static Func<HttpContext, string> ForwardReferenceToken(string introspectionScheme = "introspection")
    {
        string Select(HttpContext context)
        {
            var (scheme, credential) = GetSchemeAndCredential(context);
            if (scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase) && !credential.Contains('.'))
            {
                return introspectionScheme;
            }

            return null;
        }

        return Select;
    }

    /// <summary>
    /// Extracts scheme and credential from Authorization header (if present)
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public static SchemeCredential GetSchemeAndCredential(HttpContext context)
    {
        var header = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(header))
        {
            return SchemeCredential.Empty;
        }

        var parts = header.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return SchemeCredential.Empty;
        }

        return new SchemeCredential(parts[0], parts[1]);
    }

    /// <summary>
    /// <c>Authorization</c> 头解析出的方案与凭据。
    /// </summary>
    /// <param name="Scheme">方案名（例如 <c>Bearer</c>）。</param>
    /// <param name="Credential">凭据本体（令牌）。</param>
    /// <remarks>
    /// 用具名类型而不是 <c>(string, string)</c>：元组的元素名不进 API 文档，
    /// 调用方看到的是 <c>Item1</c>/<c>Item2</c>，而这两个都是字符串、顺序写反了编译器也不会吭声。
    /// <c>record struct</c> 自带解构，<c>var (scheme, credential) = GetSchemeAndCredential(context);</c> 照旧可用。
    /// </remarks>
    public readonly record struct SchemeCredential(string Scheme, string Credential)
    {
        /// <summary>
        /// 头缺失或格式非法时的取值：两项皆为空字符串。
        /// </summary>
        /// <remarks>不是 <see cref="string"/> 的 <c>default</c>（那会是 <see langword="null"/>）：调用方此前拿到的是空串并直接调 <c>Contains</c>，语义要保持一致。</remarks>
        public static SchemeCredential Empty { get; } = new(string.Empty, string.Empty);
    }
}