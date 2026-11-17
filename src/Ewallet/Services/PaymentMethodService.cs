using Ewallet.Dtos.PaymentMethods;
using Ewallet.Repositories;

namespace Ewallet.Services;

public interface IPaymentMethodService
{
    Task<List<PaymentMethodResponse>> GetActiveAsync(CancellationToken ct = default);
}
public class PaymentMethodService(IPaymentMethodRepository repo) : IPaymentMethodService
{
    public Task<List<PaymentMethodResponse>> GetActiveAsync(CancellationToken ct = default)
        => repo.GetActiveAsync(ct);
}