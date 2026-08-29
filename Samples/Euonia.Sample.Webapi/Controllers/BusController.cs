using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Sample.Domain.Dtos;

namespace Nerosoft.Euonia.Sample.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BusController(IBus bus): ControllerBase
{
	[HttpPost("publish/eto")]
	public async Task<IActionResult> PublishEtoAsync()
	{
		await bus.Publish(new OnetimeCodeCreatedEto())
		         .WithChannel("nerosoft.chalky.eto:OnetimeCodeCreated")
		         .ExecuteAsync(HttpContext.RequestAborted);
		return Ok();
	}
}