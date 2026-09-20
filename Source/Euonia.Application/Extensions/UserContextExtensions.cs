using System.Security.Claims;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// 用于从消息元数据中恢复用户上下文的扩展方法。
/// </summary>
/// <remarks>
/// 与 <see cref="UserContextBehavior{TMessage,TResponse}"/> 的写入方向互为镜像：
/// 行为在发送端将认证令牌与用户身份写入消息元数据，本扩展在消费端读取
/// <see cref="UserContextMetadataKeys"/> 各键并重建 <see cref="UserPrincipal"/>。
/// </remarks>
public static class UserContextExtensions
{
	/// <summary>
	/// 从消息元数据中读取认证令牌（Bearer Token）。
	/// </summary>
	/// <param name="metadata">携带用户上下文键的消息元数据。</param>
	/// <returns>认证令牌；元数据中不存在时返回 <c>null</c>。</returns>
	public static string GetAuthorizationToken(this MessageMetadata metadata)
	{
		return metadata.GetOrDefault(UserContextMetadataKeys.Authorization);
	}

	/// <summary>
	/// 尝试从消息元数据中恢复 <see cref="UserPrincipal"/>。
	/// </summary>
	/// <param name="metadata">携带用户上下文键的消息元数据。</param>
	/// <returns>
	/// 重建后的 <see cref="UserPrincipal"/>；当元数据中不含任何用户身份键
	/// （用户名称、标识、编码、租户）时返回 <c>null</c>。
	/// </returns>
	/// <remarks>
	/// 重建的前提是元数据中存在至少一个用户身份键。恢复时以 <c>Bearer</c> 作为身份验证类型，
	/// 并依据 <see cref="UserPrincipal"/> 的读取规则填入对应声明（Subject/NameIdentifier、Name、Code、Tenant）。
	/// </remarks>
	public static UserPrincipal GetUserPrincipal(this MessageMetadata metadata)
	{
		var userName = metadata.GetOrDefault(UserContextMetadataKeys.UserName);
		var userId = metadata.GetOrDefault(UserContextMetadataKeys.UserId);
		var userCode = metadata.GetOrDefault(UserContextMetadataKeys.UserCode);
		var userTenant = metadata.GetOrDefault(UserContextMetadataKeys.UserTenant);

		if (string.IsNullOrEmpty(userName) && string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(userCode) && string.IsNullOrEmpty(userTenant))
		{
			return null;
		}

		var identity = new ClaimsIdentity("Bearer");
		if (!string.IsNullOrEmpty(userId))
		{
			identity.AddClaim(new Claim(UserClaimTypes.Subject, userId));
		}

		if (!string.IsNullOrEmpty(userName))
		{
			identity.AddClaim(new Claim(UserClaimTypes.Name, userName));
		}

		if (!string.IsNullOrEmpty(userCode))
		{
			identity.AddClaim(new Claim(UserClaimTypes.Code, userCode));
		}

		if (!string.IsNullOrEmpty(userTenant))
		{
			identity.AddClaim(new Claim(UserClaimTypes.Tenant, userTenant));
		}

		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	private static string GetOrDefault(this MessageMetadata metadata, string key)
	{
		return metadata.TryGetValue(key, out var value) && value is string text ? text : null;
	}
}