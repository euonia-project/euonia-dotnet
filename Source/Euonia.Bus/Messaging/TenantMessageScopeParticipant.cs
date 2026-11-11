using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Messaging;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Bus;

/// <summary>
/// 从消息元数据解析租户并安装租户作用域；不可信来源与停用租户按策略跳过或转入死信。
/// </summary>
public sealed class TenantMessageScopeParticipant : IMessageExecutionScopeParticipant
{
	/// <inheritdoc />
	public int Order => 100;

	/// <inheritdoc />
	public async ValueTask<MessageExecutionScopeEntry> EnterAsync(IMessageContext message, IServiceProvider services, CancellationToken cancellationToken = default)
	{
		var scopeFactory = services.GetService<ITenantScopeFactory>();
		if (scopeFactory == null)
		{
			return new MessageExecutionScopeEntry();
		}

		var tenantId = ReadTenantId(message);
		if (string.IsNullOrWhiteSpace(tenantId))
		{
			// 单租户兼容：消息未携带租户元数据时保持父级租户（若存在）。
			return new MessageExecutionScopeEntry();
		}

		var options = services.GetRequiredService<IOptions<MultiTenancyOptions>>().Value;
		var logger = services.GetService<ILogger<TenantMessageScopeParticipant>>();

		var trustPolicy = services.GetService<ITenantMessageTrustPolicy>();
		var trusted = trustPolicy?.IsTrusted(message.Transport, tenantId)
		              ?? TenantMessageTrustPolicy.IsTrusted(options, message.Transport, tenantId);

		if (!trusted)
		{
			logger?.LogWarning("Message {MessageId} from transport {Transport} carries tenant {TenantId} but the transport is not trusted; the message will be dead-lettered.", message.MessageId, message.Transport, tenantId);
			return new MessageExecutionScopeEntry(null, MessageExecutionScopeAction.DeadLetter);
		}

		var resolver = services.GetRequiredService<ITenantResolver>();
		var resolution = await resolver.ResolveAsync(
			new TenantResolveContext
			{
				Services = services,
				MessageTenantId = tenantId,
				MessageId = message.MessageId,
				Transport = message.Transport,
				IsTrustedTransport = true,
			},
			cancellationToken).ConfigureAwait(false);

		var tenant = resolution.Tenant;
		if (tenant == null)
		{
			logger?.LogWarning("Message {MessageId} tenant {TenantId} could not be resolved ({FailureCode}); the message will be dead-lettered.", message.MessageId, tenantId, resolution.FailureCode);
			return new MessageExecutionScopeEntry(null, MessageExecutionScopeAction.DeadLetter);
		}
		if (tenant.Status != TenantStatus.Active)
		{
			switch (options.InactiveTenantMessagePolicy)
			{
				case TenantInactiveMessagePolicy.Skip:
					logger?.LogInformation("Message {MessageId} targets inactive tenant {TenantId}; skipped by policy.", message.MessageId, tenant.Id);
					return new MessageExecutionScopeEntry(null, MessageExecutionScopeAction.Skip);

				case TenantInactiveMessagePolicy.Process:
					break;

				default:
					logger?.LogWarning("Message {MessageId} targets inactive tenant {TenantId}; dead-lettered by policy.", message.MessageId, tenant.Id);
					return new MessageExecutionScopeEntry(null, MessageExecutionScopeAction.DeadLetter);
			}
		}

		Func<IDisposable?> install = () => scopeFactory.Begin(tenant);
		return new MessageExecutionScopeEntry(install);
	}

	private static string? ReadTenantId(IMessageContext message)
	{
		if (message.Metadata.TryGetValue(MessageMetadataKeys.TenantId, out var value) && value is string tenantId && !string.IsNullOrWhiteSpace(tenantId))
		{
			return tenantId;
		}

		// 兼容旧键：仅用于过渡期，新代码必须写标准键。
		return message.Metadata.TryGetValue(MessageMetadataKeys.LegacyUserTenant, out var legacy) && legacy is string legacyTenantId
			       ? legacyTenantId
			       : null;
	}
}
