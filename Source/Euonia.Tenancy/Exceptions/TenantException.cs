using System.Net;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户相关错误的基类。
/// </summary>
[HttpStatusCode(HttpStatusCode.BadRequest)]
public class TenantException : BusinessException
{
	/// <summary>
	/// 使用错误代码与消息初始化 <see cref="TenantException"/> 的新实例。
	/// </summary>
	/// <param name="code">错误代码。</param>
	/// <param name="message">错误消息。</param>
	public TenantException(string code, string message)
		: base(code, message)
	{
	}

	/// <summary>
	/// 使用错误代码、消息与内部异常初始化 <see cref="TenantException"/> 的新实例。
	/// </summary>
	/// <param name="code">错误代码。</param>
	/// <param name="message">错误消息。</param>
	/// <param name="innerException">导致当前异常的异常。</param>
	public TenantException(string code, string message, Exception innerException)
		: base(code, message, innerException)
	{
	}
}

/// <summary>
/// 需要租户但未解析到租户时抛出的异常。
/// </summary>
[HttpStatusCode(HttpStatusCode.BadRequest)]
public sealed class TenantRequiredException : TenantException
{
	/// <summary>
	/// 初始化 <see cref="TenantRequiredException"/> 的新实例。
	/// </summary>
	public TenantRequiredException()
		: base(TenantFailureCodes.Required, "A tenant is required but was not resolved.")
	{
	}
}

/// <summary>
/// 租户标识格式非法时抛出的异常。
/// </summary>
[HttpStatusCode(HttpStatusCode.BadRequest)]
public sealed class TenantInvalidException : TenantException
{
	/// <summary>
	/// 使用非法租户标识初始化 <see cref="TenantInvalidException"/> 的新实例。
	/// </summary>
	/// <param name="tenantId">非法租户标识。</param>
	public TenantInvalidException(string? tenantId)
		: base(TenantFailureCodes.Invalid, $"The tenant identifier '{tenantId}' is invalid.")
	{
	}
}

/// <summary>
/// 租户不存在时抛出的异常。
/// </summary>
[HttpStatusCode(HttpStatusCode.NotFound)]
public sealed class TenantNotFoundException : TenantException
{
	/// <summary>
	/// 使用租户标识初始化 <see cref="TenantNotFoundException"/> 的新实例。
	/// </summary>
	/// <param name="tenantId">未找到的租户标识。</param>
	public TenantNotFoundException(string? tenantId)
		: base(TenantFailureCodes.NotFound, $"The tenant '{tenantId}' was not found.")
	{
	}
}

/// <summary>
/// 租户停用或归档时抛出的异常。
/// </summary>
[HttpStatusCode(HttpStatusCode.Forbidden)]
public sealed class TenantSuspendedException : TenantException
{
	/// <summary>
	/// 使用租户标识与状态初始化 <see cref="TenantSuspendedException"/> 的新实例。
	/// </summary>
	/// <param name="tenantId">租户标识。</param>
	/// <param name="status">租户状态。</param>
	public TenantSuspendedException(string tenantId, TenantStatus status)
		: base(status == TenantStatus.Archived ? TenantFailureCodes.Archived : TenantFailureCodes.Suspended, $"The tenant '{tenantId}' is {status}.")
	{
	}
}

/// <summary>
/// 用户无权访问租户时抛出的异常。
/// </summary>
[HttpStatusCode(HttpStatusCode.Forbidden)]
public sealed class TenantAccessDeniedException : TenantException
{
	/// <summary>
	/// 使用租户标识初始化 <see cref="TenantAccessDeniedException"/> 的新实例。
	/// </summary>
	/// <param name="tenantId">租户标识。</param>
	public TenantAccessDeniedException(string tenantId)
		: base(TenantFailureCodes.Forbidden, $"Access to the tenant '{tenantId}' was denied.")
	{
	}
}

/// <summary>
/// 租户存储不可用时抛出的异常。
/// </summary>
[HttpStatusCode(HttpStatusCode.ServiceUnavailable)]
public sealed class TenantStoreUnavailableException : TenantException
{
	/// <summary>
	/// 使用内部异常初始化 <see cref="TenantStoreUnavailableException"/> 的新实例。
	/// </summary>
	/// <param name="innerException">导致当前异常的异常。</param>
	public TenantStoreUnavailableException(Exception innerException)
		: base(TenantFailureCodes.StoreUnavailable, "The tenant store is unavailable.", innerException)
	{
	}
}

/// <summary>
/// 跨租户写入被拒绝时抛出的异常。
/// </summary>
[HttpStatusCode(HttpStatusCode.Forbidden)]
public sealed class CrossTenantOperationException : TenantException
{
	/// <summary>
	/// 使用消息初始化 <see cref="CrossTenantOperationException"/> 的新实例。
	/// </summary>
	/// <param name="message">错误消息。</param>
	public CrossTenantOperationException(string message)
		: base(TenantFailureCodes.CrossTenant, message)
	{
	}
}
