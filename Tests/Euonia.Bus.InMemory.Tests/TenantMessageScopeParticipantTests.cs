using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Messaging;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 验证消息租户参与者的信任、解析与停用策略。
/// </summary>
public class TenantMessageScopeParticipantTests
{
	private static ServiceProvider BuildProvider(params ITenantInfo[] tenants)
	{
		return BuildProvider(null, tenants);
	}

	private static ServiceProvider BuildProvider(Action<MultiTenancyOptions>? configure, params ITenantInfo[] tenants)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddTenancy(options =>
		{
			options.Enabled = true;
			options.TrustBrokerTenant = true;
			options.TrustedTransports = ["rabbitmq"];
			options.InactiveTenantMessagePolicy = TenantInactiveMessagePolicy.DeadLetter;
			configure?.Invoke(options);
		});
		services.AddInMemoryTenantStore(tenants);
		return services.BuildServiceProvider();
	}

	private static MessageContext CreateMessage(string? tenantId, string transport = "inmemory")
	{
		var metadata = new MessageMetadata();
		if (tenantId != null)
		{
			metadata[MessageMetadataKeys.TenantId] = tenantId;
		}

		return new MessageContext
		{
			MessageId = Guid.NewGuid().ToString("N"),
			Metadata = metadata,
			Transport = transport,
		};
	}

	private static TenantInfo NewTenant(string id, TenantStatus status = TenantStatus.Active)
	{
		return new TenantInfo { Id = id, Name = id, Status = status };
	}

	/// <summary>
	/// 可信来源的活跃租户安装作用域，释放后恢复。
	/// </summary>
	[Fact]
	public async Task EnterAsync_ShouldInstallTenantScope()
	{
		await using var provider = BuildProvider(NewTenant("a"));
		var participant = new TenantMessageScopeParticipant();
		var message = CreateMessage("a");

		var entry = await participant.EnterAsync(message, provider, TestContext.Current.CancellationToken);

		Assert.Equal(MessageExecutionScopeAction.Continue, entry.Action);
		Assert.NotNull(entry.Install);

		var accessor = provider.GetRequiredService<ITenantAccessor>();
		var lease = entry.Install!();
		Assert.Equal("a", accessor.TenantId);
		Assert.Equal("a", accessor.Current!.Tenant!.Id);

		lease!.Dispose();
		Assert.Null(accessor.TenantId);
	}

	/// <summary>
	/// 不可信来源携带租户元数据时转入死信。
	/// </summary>
	[Fact]
	public async Task EnterAsync_ShouldDeadLetterUntrustedTransport()
	{
		await using var provider = BuildProvider(NewTenant("a"));
		var participant = new TenantMessageScopeParticipant();

		var entry = await participant.EnterAsync(CreateMessage("a", "unknown-broker"), provider, TestContext.Current.CancellationToken);

		Assert.Equal(MessageExecutionScopeAction.DeadLetter, entry.Action);
		Assert.Null(entry.Install);
	}

	/// <summary>
	/// 停用租户按策略跳过。
	/// </summary>
	[Fact]
	public async Task EnterAsync_ShouldSkipInactiveTenantWhenPolicyIsSkip()
	{
		await using var provider = BuildProvider(
			options => options.InactiveTenantMessagePolicy = TenantInactiveMessagePolicy.Skip,
			NewTenant("a", TenantStatus.Suspended));

		var participant = new TenantMessageScopeParticipant();
		var entry = await participant.EnterAsync(CreateMessage("a"), provider, TestContext.Current.CancellationToken);

		Assert.Equal(MessageExecutionScopeAction.Skip, entry.Action);
	}

	/// <summary>
	/// 不存在的租户转入死信。
	/// </summary>
	[Fact]
	public async Task EnterAsync_ShouldDeadLetterUnknownTenant()
	{
		await using var provider = BuildProvider(NewTenant("a"));
		var participant = new TenantMessageScopeParticipant();

		var entry = await participant.EnterAsync(CreateMessage("missing"), provider, TestContext.Current.CancellationToken);

		Assert.Equal(MessageExecutionScopeAction.DeadLetter, entry.Action);
	}

	/// <summary>
	/// 消息未携带租户元数据时保持父级上下文。
	/// </summary>
	[Fact]
	public async Task EnterAsync_ShouldContinueWithoutTenantMetadata()
	{
		await using var provider = BuildProvider(NewTenant("a"));
		var participant = new TenantMessageScopeParticipant();

		var entry = await participant.EnterAsync(CreateMessage(null), provider, TestContext.Current.CancellationToken);

		Assert.Equal(MessageExecutionScopeAction.Continue, entry.Action);
		Assert.Null(entry.Install);
	}
}
