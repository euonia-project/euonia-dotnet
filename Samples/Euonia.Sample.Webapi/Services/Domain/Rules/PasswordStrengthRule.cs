using System.Text.RegularExpressions;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Sample.Business.Rules;

/// <summary>
/// Represents a rule to validate the strength of a user's password during creation or update.
/// </summary>
internal sealed class PasswordStrengthRule(IPropertyInfo property) : RuleBase(property)
{
	/// <summary>
	/// The regular expression pattern used to validate password strength.
	/// The password must:
	/// - Contain at least one lowercase letter.
	/// - Contain at least one uppercase letter.
	/// - Contain at least one digit.
	/// - Be between 8 and 32 characters long.
	/// </summary>
	private const string REGEX_PATTERN = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[\x00-\xff]{8,32}$";

	/// <summary>
	/// Executes the rule to validate the password strength.
	/// </summary>
	/// <param name="context">The rule context containing the target object.</param>
	/// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
	/// <returns>A task that represents the asynchronous operation.</returns>
	public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		// Ensure the target object is of type IEditableObject.
		if (context.Target is not IEditableObject target)
		{
			return;
		}

		var value = target.ReadProperty(Property)?.ToString();

		// Validate the password during user creation.
		if (target.IsNew)
		{
			// Check if the password is null or whitespace.
			if (string.IsNullOrWhiteSpace(value))
			{
				// Add an error result if the password is required but missing.
				context.AddErrorResult("Password is required.");
			}
			// Check if the password matches the strength requirements.
			else if (!Regex.IsMatch(value, REGEX_PATTERN))
			{
				// Add an error result if the password does not meet the strength requirements.
				context.AddErrorResult("Password does not meet strength requirements.");
			}
		}
		// Validate the password during user updates.
		else if (target.IsChanged && !string.IsNullOrEmpty(value) && !Regex.IsMatch(value, REGEX_PATTERN))
		{
			// Add an error result if the updated password does not meet the strength requirements.
			context.AddErrorResult("Password does not meet strength requirements.");
		}

		// Complete the task.
		await Task.CompletedTask;
	}
}