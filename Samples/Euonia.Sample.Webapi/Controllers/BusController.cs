using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Bus;
using Nerosoft.Euonia.Sample.Domain.Etos;

namespace Nerosoft.Euonia.Sample.Controllers;

/// <summary>
/// 消息总线示例端点：演示如何通过总线发布集成事件并指定目标通道。
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class BusController(IBus bus): ControllerBase
{
	/// <summary>
	/// 发布一条一次性验证码创建事件到固定通道，成功后返回 200。
	/// </summary>
	/// <returns>表示操作结果的动作结果。</returns>
	[HttpPost("publish/eto")]
	public async Task<IActionResult> PublishEtoAsync()
	{
		await bus.Publish(new OnetimeCodeCreatedEto())
		         .WithChannel("nerosoft.chalky.eto:OnetimeCodeCreated")
		         .ExecuteAsync(HttpContext.RequestAborted);
		return Ok();
	}
}