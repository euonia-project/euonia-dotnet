using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests.Rules;

public class PermissionCheckRule : RuleBase
{
	public override async Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		// You do not have permission to perform this action.
		context.AddErrorResult("PermissionDenied");

		await Task.CompletedTask;
	}
}