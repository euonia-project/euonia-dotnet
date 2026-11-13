using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// JWT 令牌验证类。
/// </summary>
public class TokenValidation
{
    /// <summary>
    /// 提供用于在 JWT 与引用令牌之间转发的函数（依据令牌中是否包含点）。
    /// </summary>
    /// <param name="introspectionScheme">内省处理程序的方案名称。</param>
    /// <returns>选择认证方案的委托：引用令牌返回 <paramref name="introspectionScheme"/>，JWT 或其他情况返回 <see langword="null"/>。</returns>
    public static Func<HttpContext, string?> ForwardReferenceToken(string introspectionScheme = "introspection")
    {
        string? Select(HttpContext context)
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
    /// 从 Authorization 头中提取方案与凭据（如果存在）。
    /// </summary>
    /// <param name="context">当前 HTTP 请求上下文。</param>
    /// <returns>解析出的方案与凭据；请求头缺失或格式非法时返回 <see cref="SchemeCredential.Empty"/>。</returns>
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