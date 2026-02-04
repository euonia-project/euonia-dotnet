using Nerosoft.Euonia.Osba;
using Nerosoft.Euonia.Sample.Domain.Repositories;

namespace Nerosoft.Euonia.Sample.Business.Rules;

/// <summary>
/// Represents a rule to check the availability of a phone number during user updates.
/// </summary>
internal sealed class PhoneNumberCheckRule(IPropertyInfo property) : RuleBase(property)
{
	/// <summary>
	/// Executes the rule to verify if the phone number is available.
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

		var value = target.ReadProperty(Property)?.ToString();

		// Skip the rule if the phone number is null or whitespace.
		if (string.IsNullOrWhiteSpace(value))
		{
			return;
		}

		// Check if the phone property has been changed.
		var field = target.FieldManager.GetFieldData(Property);
		if (!field.IsChanged)
		{
			return;
		}

		// Check if the phone number already exists for another user.
		var repository = target.BusinessContext.GetRequiredService<IUserRepository>();
		var exists = await repository.AnyAsync(t => t.Phone == value, cancellationToken);
		if (exists)
		{
			// Add an error result if the phone number is unavailable.
			context.AddErrorResult($"Phone number '{value}' is unavailable.");
		}
	}
}