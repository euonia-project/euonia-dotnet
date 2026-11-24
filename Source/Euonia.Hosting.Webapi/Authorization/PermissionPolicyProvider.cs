using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// 将以 <see cref="RequiresPermissionAttribute.PolicyPrefix"/> 开头的策略名还原为权限策略。
/// </summary>
/// <param name="fallback">默认策略提供程序，处理非权限策略名。</param>
public sealed class PermissionPolicyProvider(DefaultAuthorizationPolicyProvider fallback) : IAuthorizationPolicyProvider
{
	private static readonly ConcurrentDictionary<string, AuthorizationPolicy> Policies = new(StringComparer.OrdinalIgnoreCase);

	/// <inheritdoc />
	public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
	{
		return fallback.GetDefaultPolicyAsync();
	}

	/// <inheritdoc />
	public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
	{
		return fallback.GetFallbackPolicyAsync();
	}

	/// <inheritdoc />
	public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
	{
		ArgumentNullException.ThrowIfNull(policyName);

		if (!policyName.StartsWith(RequiresPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return fallback.GetPolicyAsync(policyName);
		}

		var policy = Policies.GetOrAdd(policyName, static name =>
		{
			var permissions = name[RequiresPermissionAttribute.PolicyPrefix.Length..]
			                   .Split([RequiresPermissionAttribute.PolicySeparator], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

			return new AuthorizationPolicyBuilder()
			       .RequireAuthenticatedUser()
			       .AddRequirements(new PermissionRequirement(permissions))
			       .Build();
		});

		return Task.FromResult<AuthorizationPolicy?>(policy);
	}
}
