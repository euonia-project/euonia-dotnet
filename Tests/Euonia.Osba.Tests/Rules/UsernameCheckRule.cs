using System.Diagnostics;
using Nerosoft.Euonia.Osba;

namespace Nerosoft.Euonia.Core.Tests.Rules;

[ExecuteOnState(ObjectEditState.New)]
public class UsernameCheckRule : RuleBase
{
	public override Task ExecuteAsync(IRuleContext context, CancellationToken cancellationToken = default)
	{
		// if (context.Target is not UserGeneralBusiness obj)
		// {
		// 	return;
		// }
		//
		// if (string.Equals(obj.Name, "admin", StringComparison.OrdinalIgnoreCase))
		// {
		// 	// Username "admin" is not allowed.
		// 	context.AddErrorResult("UsernameNotAllowed");
		// }

		return Task.Run(() =>
		          {
			          if (context.Target is not UserGeneralBusiness obj)
			          {
				          return;
			          }

			          if (string.Equals(obj.Name, "admin", StringComparison.OrdinalIgnoreCase))
			          {
				          // Username "admin" is not allowed.
				          context.AddErrorResult("UsernameNotAllowed");
			          }
		          }, cancellationToken);
	}
}