using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Messaging;
using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Bus.Tests;

/// <summary>
/// 端到端验证租户参与者在真实执行作用域中的安装时机：处理器必须能看到租户。
/// </summary>
/// <remarks>
/// 回归点：参与者若在 <c>await</c> 内部写 <c>AsyncLocal</c>，写时复制会使处理器看不到该值。
/// </remarks>
public class TenantMessageScopeEndToEndTests
{
	/// <summary>
	/// 可信消息的处理器可读取当前租户。
	/// </summary>
	[Fact]
	public async Task ExecutionScope_ShouldExposeTenantToHandler()
	{
		await using var provider = BuildProvider(TenantStatus.Active);
		var accessor = provider.GetRequiredService<ITenantAccessor>();
		var environment = new MessageExecutionEnvironment(provider, null);

		string? observed = null;
		var result = await environment.RunWithResultAsync(
			MessageExecutionDescriptor.DistributedInbound,
			CreateContext("a"),
			_ =>
			{
				observed = accessor.TenantId;
				return Task.CompletedTask;
			});

		Assert.Equal(MessageExecutionScopeAction.Continue, result.Action);
		Assert.Equal("a", observed);
		Assert.Null(accessor.TenantId);
	}

	/// <summary>
	/// 停用租户按跳过策略处理时处理器不执行，且消息正常完成。
	/// </summary>
	[Fact]
	public async Task ExecutionScope_ShouldSkipHandlerForInactiveTenant()
	{
		await using var provider = BuildProvider(
			TenantStatus.Suspended,
			options => options.InactiveTenantMessagePolicy = TenantInactiveMessagePolicy.Skip);
		var environment = new MessageExecutionEnvironment(provider, null);

		var called = false;
		var result = await environment.RunWithResultAsync(
			MessageExecutionDescriptor.DistributedInbound,
			CreateContext("a"),
			_ =>
			{
				called = true;
				return Task.CompletedTask;
			});

		Assert.Equal(MessageExecutionScopeAction.Skip, result.Action);
		Assert.False(called);
	}

	/// <summary>
	/// 不可信来源携带租户元数据时转入死信，处理器不执行。
	/// </summary>
	[Fact]
	public async Task ExecutionScope_ShouldDeadLetterUntrustedTransport()
	{
		await using var provider = BuildProvider(TenantStatus.Active);
		var environment = new MessageExecutionEnvironment(provider, null);

		var called = false;
		var result = await environment.RunWithResultAsync(
			MessageExecutionDescriptor.DistributedInbound,
			CreateContext("a", "unknown-broker"),
			_ =>
			{
				called = true;
				return Task.CompletedTask;
			});

		Assert.True(result.DeadLettered);
		Assert.False(called);
	}

	private static ServiceProvider BuildProvider(TenantStatus status, Action<MultiTenancyOptions>? configure = null)
	{
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddTenancy(options =>
		{
			options.Enabled = true;
			configure?.Invoke(options);
		});
		services.AddInMemoryTenantStore([new TenantInfo { Id = "a", Name = "a", Status = status }]);

		// AddEuoniaBus 会注册这两个参与者；本测试只验证执行作用域，直接注册以缩小依赖面。
		services.AddTransient<IMessageExecutionScopeParticipant, ExecutionContextMessageScopeParticipant>();
		services.AddTransient<IMessageExecutionScopeParticipant, TenantMessageScopeParticipant>();

		return services.BuildServiceProvider();
	}

	private static MessageContext CreateContext(string tenantId, string transport = "inmemory")
	{
		var metadata = new MessageMetadata
		{
			[MessageMetadataKeys.TenantId] = tenantId,
		};

		return new MessageContext
		{
			MessageId = Guid.NewGuid().ToString("N"),
			Metadata = metadata,
			Transport = transport,
		};
	}
}
