using Ewallet.Dtos.Common;
using Ewallet.Dtos.History;
using Ewallet.Dtos.Wallets;
using Ewallet.Exceptions;
using Ewallet.Repositories;

namespace Ewallet.Services;

public interface IWalletService
{
    Task<WalletResponse> GetInfoAsync(long walletId, CancellationToken ct = default);
    Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(long walletId, HistoryQuery query, CancellationToken ct = default);
    Task<PagedResponse<MutationResponse>> GetMutationsAsync(long walletId, HistoryQuery query, CancellationToken ct = default);
}

public class WalletService(IWalletRepository wallets, ITransactionRepository transactions, IWalletMutationRepository mutations) : IWalletService
{
    private static readonly string[] SortFields = ["createdAt", "amount"];
    private static readonly string[] SortDirs = ["asc", "desc"];

    public async Task<WalletResponse> GetInfoAsync(long walletId, CancellationToken ct = default)
        => await wallets.GetInfoAsync(walletId, ct) ?? throw new WalletNotFoundException();

    public async Task<PagedResponse<TransactionResponse>> GetTransactionsAsync(long walletId, HistoryQuery query, CancellationToken ct = default)
    {
        await EnsureValidAsync(walletId, query, ct);
        return await transactions.GetHistoryAsync(walletId, query, ct);
    }

    public async Task<PagedResponse<MutationResponse>> GetMutationsAsync(long walletId, HistoryQuery query, CancellationToken ct = default)
    {
        await EnsureValidAsync(walletId, query, ct);
        return await mutations.GetHistoryAsync(walletId, query, ct);
    }

    private async Task EnsureValidAsync(long walletId, HistoryQuery q, CancellationToken ct)
    {
        if (!SortFields.Contains(q.SortBy, StringComparer.OrdinalIgnoreCase))
            throw new WalletException("sortBy harus salah satu dari: createdAt, amount.");
        if (!SortDirs.Contains(q.SortDir, StringComparer.OrdinalIgnoreCase))
            throw new WalletException("sortDir harus asc atau desc.");
        if (q.From is { } from && q.To is { } to && from > to)
            throw new WalletException("Parameter from tidak boleh lebih besar dari to.");
        if (!await wallets.ExistsAsync(walletId, ct)) throw new WalletNotFoundException();
    }
}
