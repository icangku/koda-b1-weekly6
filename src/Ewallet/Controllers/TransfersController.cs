using Ewallet.Dtos.Transfers;
using Ewallet.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ewallet.Controllers;

[ApiController]
[Route("api/transfers")]
public class TransfersController(ITransferService service, IWebHostEnvironment env) : ControllerBase
{
    /// <summary>Transfer between wallets: 1 Transaction + 2 WalletMutations (OUT sender, IN receiver).</summary>
    /// <param name="simulateFailure">Development/Testing only: fail after writing, before commit, to demonstrate rollback.</param>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] TransferRequest request, [FromQuery] bool simulateFailure, CancellationToken ct)
    {
        if (simulateFailure && !(env.IsDevelopment() || env.IsEnvironment("Testing")))
            return BadRequest(new { status = 400, error = "simulateFailure hanya tersedia di Development/Testing." });

        var result = await service.TransferAsync(request, simulateFailure, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
