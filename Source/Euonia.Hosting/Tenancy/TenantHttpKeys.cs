namespace Nerosoft.Euonia.Hosting;

/// <summary>
/// HTTP 请求中与租户相关的 <see cref="Microsoft.AspNetCore.Http.HttpContext.Items"/> 键。
/// </summary>
public static class TenantHttpKeys
{
	/// <summary>
	/// 租户快照键；外层审计中间件在租户作用域释放后仍可读取。
	/// </summary>
	public const string Context = "Euonia:Tenant:Context";

	/// <summary>
	/// 解析失败代码键。
	/// </summary>
	public const string Failure = "Euonia:Tenant:Failure";

	/// <summary>
	/// 解析候选值键。
	/// </summary>
	public const string Candidates = "Euonia:Tenant:Candidates";
}
