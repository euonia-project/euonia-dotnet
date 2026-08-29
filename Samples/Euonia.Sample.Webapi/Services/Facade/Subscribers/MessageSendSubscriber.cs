using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Facade.Subscribers;

public class MessageSendSubscriber : ITransientDependency
{
	[Subscribe("nerosoft.chalky.eto:OnetimeCodeCreated")]
	public async Task HandleOtpCreatedAsync(OnetimeCodeCreatedEto eto, CancellationToken cancellationToken = default)
	{
		Console.WriteLine($"[MessageSendSubscriber] Received OTP: Type={eto.Type}, Code={eto.Code}, Recipient={eto.Recipient}, Usage={eto.Usage}, Timeout={eto.Timeout}");
	}
}