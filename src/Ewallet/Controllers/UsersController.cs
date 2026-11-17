using Ewallet.Dtos.Users;
using Ewallet.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ewallet.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(IUserService service) : ControllerBase
{
  [HttpGet]
  public async Task<IActionResult> GetAll(CancellationToken ct) => Ok(await service.GetAllAsync(ct));

  [HttpGet("{id:long}")]
  public async Task<IActionResult> GetById(long id, CancellationToken ct) => Ok(await service.GetByIdAsync(id, ct));

  [HttpPost]
  public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken ct)
  {
    var created = await service.CreateAsync(request, ct);
    return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
  }

  [HttpPut("{id:long}")]
  public async Task<IActionResult> Update(long id, [FromBody] UpdateUserRequest request, CancellationToken ct)
  {
    await service.UpdateAsync(id, request, ct);
    return NoContent();
  }
}