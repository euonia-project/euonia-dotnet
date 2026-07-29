using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nerosoft.Euonia.Sample.Domain.Dtos;
using Nerosoft.Euonia.Sample.Facade.Contracts;

namespace Nerosoft.Euonia.Sample.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UserController(IUserApplicationService service) : ControllerBase
{
	[HttpGet("{id}")]
	public async Task<IActionResult> GetAsync(string id)
	{
		var result = await service.GetAsync(id, HttpContext.RequestAborted);
		if (result == null)
		{
			throw new NotFoundException();
		}
		return Ok(result);
	}

	[HttpGet("search")]
	public async Task<IActionResult> FindAsync([FromQuery] string keyword, [FromQuery] int skip = 0, [FromQuery] int take = 20)
	{
		var result = await service.FindAsync(keyword ?? string.Empty, skip, take, HttpContext.RequestAborted);
		return Ok(result);
	}

	[HttpPost]
	public async Task<IActionResult> CreateAsync([FromBody] UserCreateDto data)
	{
		var id = await service.CreateAsync(data, HttpContext.RequestAborted);
		return CreatedAtAction(nameof(GetAsync), new { id }, null);
	}
}
