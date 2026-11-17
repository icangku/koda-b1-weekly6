using Ewallet.Services;
using Microsoft.AspNetCore.Mvc;

namespace Ewallet.Controllers;

[ApiController]
[Route("api/payment-methods")]
public class PaymentMethodsController(IPaymentMethodService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActive(CancellationToken ct) => Ok(await service.GetActiveAsync(ct));
}