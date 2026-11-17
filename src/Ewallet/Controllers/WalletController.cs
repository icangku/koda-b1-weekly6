using Ewallet.Dtos.History;
using Ewallet.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ewallet.Controllers;

[ApiController]
[Route("api/wallets")]
public class WalletsController(IWalletService service) : ControllerBase
{
    /// <summary>Wallet info: owner, wallet number, balance, status, RowVersion.</summary>
    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetInfo(long id, CancellationToken ct) => Ok(await service.GetInfoAsync(id, ct));

    /// <summary>Transaction history of a wallet (search, filter, sort, pagination).</summary>
    [HttpGet("{id:long}/transactions")]
    public async Task<IActionResult> GetTransactions(long id, [FromQuery] HistoryQuery query, CancellationToken ct)
        => Ok(await service.GetTransactionsAsync(id, query, ct));

    /// <summary>Balance mutations of a wallet (search, filter, sort, pagination).</summary>
    [HttpGet("{id:long}/mutations")]
    public async Task<IActionResult> GetMutations(long id, [FromQuery] HistoryQuery query, CancellationToken ct)
        => Ok(await service.GetMutationsAsync(id, query, ct));
}
