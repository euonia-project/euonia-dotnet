using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Business.Rules;

/// <summary>
/// Represents a rule to check the availability of a username during user creation.
/// </summary>
internal sealed class UsernameCheckRule(IPropertyInfo property) : RuleBase(property)
{
	/// <summary>
	/// Executes the rule to verify if the username is available.
	/// </summary>
	/// <param name="context">The rule context containing the target object.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		// Ensure the target object is of type UserGeneralBusiness.
		if (context.Target is not IEditableObject target)
		{
			return;
		}

		// Skip the rule if the operation is not an insert.
		if (!target.IsNew)
		{
			return;
		}

		var value = target.ReadProperty(Property)?.ToString();

		if (string.IsNullOrWhiteSpace(value))
		{
			context.AddErrorResult("Username is required.");
		}
		else
		{
			var configuration = target.BusinessContext.GetRequiredService<IConfiguration>();

			// Retrieve the list of reserved usernames from the configuration.
			var reserved = configuration.GetValue<List<string>>("ReservedUsernames");

			// Check if the username is in the reserved list.
			if (reserved?.Contains(value, StringComparer.InvariantCultureIgnoreCase) == true)
			{
				// Add an error result if the username is reserved.
				context.AddErrorResult($"Username '{value}' is unavailable.");
				return;
			}

			var repository = target.BusinessContext.GetRequiredService<IUserRepository>();

			// Check if the username already exists in the repository.
			var exists = await repository.AnyAsync(t => t.Username == value, cancellationToken);
			if (exists)
			{
				// Add an error result if the username already exists.
				context.AddErrorResult($"Username '{value}' is unavailable.");
			}
		}
	}
}