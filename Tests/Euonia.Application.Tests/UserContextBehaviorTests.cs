using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application.Tests;

/// <summary>
/// 针对 <see cref="UserContextBehavior{TMessage, TResponse}"/> 元数据写入与用户上下文扩展方法读取行为的测试。
/// </summary>
public class UserContextBehaviorTests
{
	/// <summary>
	/// 请求头包含 Bearer 令牌时应把授权信息写入消息元数据。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithBearerToken_ShouldWriteAuthorizationMetadata()
	{
		var behavior = CreateBehavior(new Dictionary<string, string> { [nameof(RequestContext.Authorization)] = "Bearer abc" }, CreateAuthenticatedUser());
		var context = new TestMessage();

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("Bearer abc", context.Metadata[UserContextMetadataKeys.Authorization]);
	}

	/// <summary>
	/// 请求头不含授权信息时不应写入授权元数据。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithoutToken_ShouldNotWriteAuthorizationMetadata()
	{
		var behavior = CreateBehavior(new Dictionary<string, string>(), CreateAuthenticatedUser());
		var context = new TestMessage();

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.False(context.Metadata.ContainsKey(UserContextMetadataKeys.Authorization));
	}

	/// <summary>
	/// 令牌为字面量 <c>null</c> 时视为未提供授权，不应写入授权元数据。
	/// </summary>
	[Fact]
	public async Task HandleAsync_BearerNull_ShouldNotWriteAuthorizationMetadata()
	{
		var behavior = CreateBehavior(new Dictionary<string, string> { [nameof(RequestContext.Authorization)] = "Bearer null" }, CreateAuthenticatedUser());
		var context = new TestMessage();

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.False(context.Metadata.ContainsKey(UserContextMetadataKeys.Authorization));
	}

	/// <summary>
	/// 已认证用户应把用户名、用户 ID、编码与租户写入元数据。
	/// </summary>
	[Fact]
	public async Task HandleAsync_WithAuthenticatedUser_ShouldWriteUserMetadata()
	{
		var behavior = CreateBehavior(new Dictionary<string, string>(), CreateAuthenticatedUser());
		var context = new TestMessage();

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("Alice", context.Metadata[UserContextMetadataKeys.UserName]);
		Assert.Equal("user-1", context.Metadata[UserContextMetadataKeys.UserId]);
		Assert.Equal("U001", context.Metadata[UserContextMetadataKeys.UserCode]);
		Assert.Equal("TENANT1", context.Metadata[UserContextMetadataKeys.UserTenant]);
	}

	/// <summary>
	/// 匿名用户不应写入任何用户元数据。
	/// </summary>
	[Fact]
	public async Task HandleAsync_AnonymousUser_ShouldNotWriteUserMetadata()
	{
		var behavior = CreateBehavior(new Dictionary<string, string> { [nameof(RequestContext.Authorization)] = "Bearer abc" }, CreateAnonymousUser());
		var context = new TestMessage();

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.False(context.Metadata.ContainsKey(UserContextMetadataKeys.UserName));
		Assert.False(context.Metadata.ContainsKey(UserContextMetadataKeys.UserId));
		Assert.False(context.Metadata.ContainsKey(UserContextMetadataKeys.UserCode));
		Assert.False(context.Metadata.ContainsKey(UserContextMetadataKeys.UserTenant));
	}

	/// <summary>
	/// 行为应继续调用管道中的下一个处理器。
	/// </summary>
	[Fact]
	public async Task HandleAsync_ShouldInvokeNext()
	{
		var behavior = CreateBehavior(new Dictionary<string, string>(), CreateAuthenticatedUser());
		var context = new TestMessage();
		var invoked = false;

		await behavior.HandleAsync(context, _ =>
		{
			invoked = true;
			return Task.FromResult(true);
		});

		Assert.True(invoked);
	}

	/// <summary>
	/// 写入元数据后读取应还原出完整的已认证用户主体。
	/// </summary>
	[Fact]
	public async Task UserContextExtensions_WriteThenRead_ShouldRoundTripUserPrincipal()
	{
		var behavior = CreateBehavior(new Dictionary<string, string> { [nameof(RequestContext.Authorization)] = "Bearer abc" }, CreateAuthenticatedUser());
		var context = new TestMessage();

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		var user = context.Metadata.GetUserPrincipal();
		Assert.NotNull(user);
		Assert.True(user.IsAuthenticated);
		Assert.Equal("Alice", user.Username);
		Assert.Equal("user-1", user.UserId);
		Assert.Equal("U001", user.Code);
		Assert.Equal("TENANT1", user.Tenant);
	}

	/// <summary>
	/// 元数据不含用户相关键时 <c>GetUserPrincipal</c> 应返回 <see langword="null"/>。
	/// </summary>
	[Fact]
	public void UserContextExtensions_NoUserKeys_ShouldReturnNull()
	{
		var metadata = new MessageMetadata();

		Assert.Null(metadata.GetUserPrincipal());
	}

	/// <summary>
	/// 仅包含部分用户键时应按可用声明重建用户主体，缺失字段为 <see langword="null"/>。
	/// </summary>
	[Fact]
	public void UserContextExtensions_PartialUserKeys_ShouldRebuildAvailableClaims()
	{
		var metadata = new MessageMetadata();
		metadata.Set(UserContextMetadataKeys.UserName, "Bob");

		var user = metadata.GetUserPrincipal();

		Assert.NotNull(user);
		Assert.Equal("Bob", user.Username);
		Assert.Null(user.UserId);
		Assert.Null(user.Code);
		Assert.Null(user.Tenant);
	}

	/// <summary>
	/// 应能读取元数据中保存的授权令牌。
	/// </summary>
	[Fact]
	public void UserContextExtensions_AuthorizationToken_ShouldReadToken()
	{
		var metadata = new MessageMetadata();
		metadata.Set(UserContextMetadataKeys.Authorization, "Bearer token-1");

		Assert.Equal("Bearer token-1", metadata.GetAuthorizationToken());
	}

	/// <summary>
	/// 元数据不含授权键时读取令牌应返回 <see langword="null"/>。
	/// </summary>
	[Fact]
	public void UserContextExtensions_NoAuthorizationKey_ShouldReturnNull()
	{
		var metadata = new MessageMetadata();

		Assert.Null(metadata.GetAuthorizationToken());
	}

	private static UserContextBehavior<TestMessage, bool> CreateBehavior(IDictionary<string, string> headers, UserPrincipal user)
	{
		var services = new ServiceCollection();
		var accessor = new StubRequestContextAccessor
		{
			Context = new RequestContext { Headers = headers }
		};
		services.AddSingleton<IRequestContextAccessor>(accessor);
		services.AddScoped(_ => user);
		var provider = services.BuildServiceProvider();

		return new UserContextBehavior<TestMessage, bool>(provider.GetRequiredService<IServiceScopeFactory>());
	}

	private static UserPrincipal CreateAuthenticatedUser()
	{
		var claims = new List<Claim>
		{
			new(UserClaimTypes.Subject, "user-1"),
			new(UserClaimTypes.Name, "Alice"),
			new(UserClaimTypes.Code, "U001"),
			new(UserClaimTypes.Tenant, "TENANT1"),
		};
		var identity = new ClaimsIdentity(claims, "Bearer");
		return new UserPrincipal(new ClaimsPrincipal(identity));
	}

	private static UserPrincipal CreateAnonymousUser()
	{
		return new UserPrincipal(new ClaimsPrincipal(new ClaimsIdentity()));
	}

	/// <summary>
	/// 携带固定负载与通道路由信息的测试消息，用于驱动用户上下文行为读取消息头并构建用户主体。
	/// </summary>
	private sealed class TestMessage : RoutedMessage<string>
	{
		public TestMessage()
			: base("payload", "channel")
		{
		}
	}
}