using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application.Tests;

public class UserContextBehaviorTests
{
	[Fact]
	public async Task HandleAsync_WithBearerToken_ShouldWriteAuthorizationMetadata()
	{
		var behavior = CreateBehavior(new Dictionary<string, string> { [nameof(RequestContext.Authorization)] = "Bearer abc" }, CreateAuthenticatedUser());
		var context = new TestMessage();

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.Equal("Bearer abc", context.Metadata[UserContextMetadataKeys.Authorization]);
	}

	[Fact]
	public async Task HandleAsync_WithoutToken_ShouldNotWriteAuthorizationMetadata()
	{
		var behavior = CreateBehavior(new Dictionary<string, string>(), CreateAuthenticatedUser());
		var context = new TestMessage();

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.False(context.Metadata.ContainsKey(UserContextMetadataKeys.Authorization));
	}

	[Fact]
	public async Task HandleAsync_BearerNull_ShouldNotWriteAuthorizationMetadata()
	{
		var behavior = CreateBehavior(new Dictionary<string, string> { [nameof(RequestContext.Authorization)] = "Bearer null" }, CreateAuthenticatedUser());
		var context = new TestMessage();

		await behavior.HandleAsync(context, _ => Task.FromResult(true));

		Assert.False(context.Metadata.ContainsKey(UserContextMetadataKeys.Authorization));
	}

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

	[Fact]
	public void UserContextExtensions_NoUserKeys_ShouldReturnNull()
	{
		var metadata = new MessageMetadata();

		Assert.Null(metadata.GetUserPrincipal());
	}

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

	[Fact]
	public void UserContextExtensions_AuthorizationToken_ShouldReadToken()
	{
		var metadata = new MessageMetadata();
		metadata.Set(UserContextMetadataKeys.Authorization, "Bearer token-1");

		Assert.Equal("Bearer token-1", metadata.GetAuthorizationToken());
	}

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

	private sealed class TestMessage : RoutedMessage<string>
	{
		public TestMessage()
			: base("payload", "channel")
		{
		}
	}
}