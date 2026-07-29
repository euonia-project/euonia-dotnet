using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Modularity;
using Nerosoft.Euonia.Pipeline;
using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Application;

/// <summary>
/// A pipeline behavior that adds the user principal to the command metadata.
/// </summary>
public class AuthorizationBehavior<TMessage, TResponse> : IPipelineBehavior<TMessage, TResponse>
	where TMessage : class, IRoutedMessage
{
	private readonly UserPrincipal _user;
	private readonly IRequestContextAccessor _contextAccessor;

	/// <summary>
	/// Initializes a new instance of the <see cref="AuthorizationBehavior{TMessage, TResponse}"/> class.
	/// </summary>
	/// <param name="provider"></param>
	public AuthorizationBehavior(IServiceProvider provider)
	{
		_user = provider.GetService<UserPrincipal>();
		_contextAccessor = provider.GetService<IRequestContextAccessor>();
	}

	/// <inheritdoc />
	public async Task<TResponse> HandleAsync(TMessage context, PipelineDelegate<TMessage, TResponse> next)
	{
		if (_contextAccessor?.Context?.RequestHeaders.TryGetValue("Authorization", out var value) == true)
		{
			if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("Bearer") && !value.Equals("Bearer null", StringComparison.OrdinalIgnoreCase))
			{
				context.Metadata.Set("Authorization", value);
			}
		}

		if (_user is { IsAuthenticated: true })
		{
			context.Metadata.Set("$nerosoft:user.name", _user.Username);
			context.Metadata.Set("$nerosoft:user.id", _user.UserId);
			context.Metadata.Set("$nerosoft:user.code", _user.Code);
			context.Metadata.Set("$nerosoft:user.tenant", _user.Tenant);
		}

		{
		}

		return await next(context);
	}
}