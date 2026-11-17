using Ewallet.Data;
using Ewallet.Dtos.PaymentMethods;
using Ewallet.Models;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Repositories;

public interface IPaymentMethodRepository
{
    Task<List<PaymentMethodResponse>> GetActiveAsync(CancellationToken ct = default);
    Task<PaymentMethod?> GetActiveByIdAsync(int id, CancellationToken ct = default);
}

public class PaymentMethodRepository(AppDbContext db) : IPaymentMethodRepository
{
    public Task<List<PaymentMethodResponse>> GetActiveAsync(CancellationToken ct = default)
        => db.PaymentMethods.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Id)
            .Select(p => new PaymentMethodResponse(p.Id, p.Code, p.Name, p.ProviderName))
            .ToListAsync(ct);

    public Task<PaymentMethod?> GetActiveByIdAsync(int id, CancellationToken ct = default)
        => db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id && p.IsActive, ct);
}
