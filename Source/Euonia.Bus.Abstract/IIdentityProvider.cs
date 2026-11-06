using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 定义身份提供程序。
/// </summary>
public interface IIdentityProvider
{
	/// <summary>
	/// 从授权标头获取标识。
	/// </summary>
	/// <returns>从授权标头解析出的标识。</returns>
	IPrincipal GetIdentity();
}