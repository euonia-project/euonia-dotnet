using System.Security.Principal;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// The request context accessor.
/// </summary>
/// <returns></returns>
public delegate IPrincipal IdentityAccessor();