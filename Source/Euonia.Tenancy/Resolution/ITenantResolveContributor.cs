using System.Security.Claims;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 定义租户候选值来源。
/// </summary>
/// <remarks>
/// 数字越小优先级越高。宿主按需注册 Claim / Host / Route / Header 等来源；
/// 消息路径由 Bus 适配层提供消息元数据来源。
/// </remarks>
public interface ITenantResolveContributor
{
	/// <summary>
	/// 获取来源优先级；数字越小越优先。
	/// </summary>
	int Order { get; }

	/// <summary>
	/// 解析候选值。
	/// </summary>
	/// <param name="context">解析上下文。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>候选值；当前来源不可用时为 <c>null</c>。</returns>
	ValueTask<string?> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default);
}
