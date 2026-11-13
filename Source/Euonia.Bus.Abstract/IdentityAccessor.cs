using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 请求上下文访问器。
/// </summary>
/// <returns>当前请求的标识。</returns>
public delegate IPrincipal IdentityAccessor();