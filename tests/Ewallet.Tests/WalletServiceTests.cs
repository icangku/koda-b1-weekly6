using Ewallet.Data;
using Ewallet.Dtos.History;
using Ewallet.Dtos.TopUps;
using Ewallet.Dtos.Transfers;
using Ewallet.Dtos.Users;
using Ewallet.Exceptions;
using Ewallet.Models;
using Ewallet.Repositories;
using Ewallet.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Ewallet.Tests;

// Business-rule tests on the EF InMemory provider.
// Note: InMemory ignores DB transactions, CHECK constraints and RowVersion generation,
// so rollback and concurrency are verified against SQL Server (see README).
public class WalletServiceTests
{
    private readonly AppDbContext _db;
    private readonly UserService _users;
    private readonly TopUpService _topUps;
    private readonly TransferService _transfers;
    private readonly WalletService _wallets;

    public WalletServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString(), o => o.EnableNullChecks(false))   // InMemory does not generate rowversion
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _db = new AppDbContext(options);

        var uow = new UnitOfWork(_db);
        var walletRepo = new WalletRepository(_db);
        var trxRepo = new TransactionRepository(_db);
        var mutationRepo = new WalletMutationRepository(_db);
        var audit = new AuditLogService(new AuditLogRepository(_db), uow, new HttpContextAccessor());

        _users = new UserService(new UserRepository(_db), walletRepo, uow);
        _topUps = new TopUpService(walletRepo, new PaymentMethodRepository(_db), trxRepo, new TopUpRequestRepository(_db), mutationRepo, audit, uow);
        _transfers = new TransferService(walletRepo, trxRepo, audit, uow);
        _wallets = new WalletService(walletRepo, trxRepo, mutationRepo);

        _db.PaymentMethods.AddRange(
            new PaymentMethod { Id = 1, Code = "QRIS", Name = "QRIS", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new PaymentMethod { Id = 2, Code = "OLD", Name = "Old", IsActive = false, CreatedAtUtc = DateTime.UtcNow });
        _db.SaveChanges();
    }

    private async Task<long> NewWalletAsync(string username, decimal topUp = 0)
    {
        var user = await _users.CreateAsync(new CreateUserRequest(username, username, $"{username}@example.com", $"08{username.GetHashCode() & 0x7fffffff}"));
        var walletId = user.WalletId!.Value;
        if (topUp > 0)
        {
            var req = await _topUps.RequestAsync(new CreateTopUpRequest(walletId, 1, topUp));
            await _topUps.ConfirmAsync(req.Id);
        }
        _db.ChangeTracker.Clear();
        return walletId;
    }

    private async Task<Wallet> WalletAsync(long id) => await _db.Wallets.AsNoTracking().SingleAsync(w => w.Id == id);

    [Fact]
    public async Task CreateUser_CreatesActiveWalletWithZeroBalance()
    {
        var user = await _users.CreateAsync(new CreateUserRequest("Budi", "Budi", "budi@example.com", "081200000001"));

        Assert.Equal("budi", user.Username);
        var wallet = await WalletAsync(user.WalletId!.Value);
        Assert.Equal(0m, wallet.Balance);
        Assert.Equal(WalletStatus.Active, wallet.Status);
    }

    [Fact]
    public async Task CreateUser_DuplicateUsername_Conflict()
    {
        await _users.CreateAsync(new CreateUserRequest("budi", "Budi", "budi@example.com", "081200000001"));
        await Assert.ThrowsAsync<ConflictException>(() =>
            _users.CreateAsync(new CreateUserRequest("BUDI", "Budi 2", "budi2@example.com", "081200000002")));
    }

    [Fact]
    public async Task TopUpRequest_IsPending_AndDoesNotChangeBalance()
    {
        var walletId = await NewWalletAsync("siti");

        var result = await _topUps.RequestAsync(new CreateTopUpRequest(walletId, 1, 100_000));

        Assert.Equal(TopUpStatus.Pending, result.Status);
        Assert.Equal(TransactionStatus.Pending, result.TransactionStatus);
        Assert.Equal(0m, (await WalletAsync(walletId)).Balance);
        Assert.Empty(_db.WalletMutations);
        Assert.Single(_db.AuditLogs, a => a.Action == AuditActions.TopUpRequested);
    }

    [Fact]
    public async Task TopUpRequest_InactivePaymentMethod_Rejected()
    {
        var walletId = await NewWalletAsync("siti");
        await Assert.ThrowsAsync<PaymentMethodNotFoundException>(() => _topUps.RequestAsync(new CreateTopUpRequest(walletId, 2, 10_000)));
    }

    [Fact]
    public async Task ConfirmTopUp_AddsBalance_CreatesInMutation_AndCannotBeRepeated()
    {
        var walletId = await NewWalletAsync("andi");
        var req = await _topUps.RequestAsync(new CreateTopUpRequest(walletId, 1, 250_000));

        var paid = await _topUps.ConfirmAsync(req.Id);

        Assert.Equal(TopUpStatus.Paid, paid.Status);
        Assert.Equal(TransactionStatus.Success, paid.TransactionStatus);
        Assert.Equal(250_000m, (await WalletAsync(walletId)).Balance);
        var mutation = Assert.Single(_db.WalletMutations);
        Assert.Equal(MutationDirection.In, mutation.Direction);
        Assert.Equal(0m, mutation.BalanceBefore);
        Assert.Equal(250_000m, mutation.BalanceAfter);
        Assert.Single(_db.AuditLogs, a => a.Action == AuditActions.TopUpPaid);

        await Assert.ThrowsAsync<ConflictException>(() => _topUps.ConfirmAsync(req.Id));
        Assert.Equal(250_000m, (await WalletAsync(walletId)).Balance);
    }

    [Fact]
    public async Task Transfer_Success_OneTransactionTwoMutations()
    {
        var sender = await NewWalletAsync("dewi", 100_000);
        var receiver = await NewWalletAsync("rizky");
        var receiverNumber = (await WalletAsync(receiver)).WalletNumber;

        var result = await _transfers.TransferAsync(new TransferRequest(sender, receiverNumber, 40_000, "makan", null));

        Assert.Equal(60_000m, (await WalletAsync(sender)).Balance);
        Assert.Equal(40_000m, (await WalletAsync(receiver)).Balance);
        var mutations = _db.WalletMutations.Where(m => m.TransactionId == result.TransactionId).ToList();
        Assert.Equal(2, mutations.Count);
        Assert.Contains(mutations, m => m.WalletId == sender && m.Direction == MutationDirection.Out && m.BalanceAfter == 60_000m);
        Assert.Contains(mutations, m => m.WalletId == receiver && m.Direction == MutationDirection.In && m.BalanceAfter == 40_000m);
        Assert.Single(_db.AuditLogs, a => a.Action == AuditActions.TransferCreated && a.TransactionId == result.TransactionId);
    }

    [Theory]
    [InlineData(500_000, typeof(InsufficientBalanceException))]
    [InlineData(0, typeof(InvalidAmountException))]
    [InlineData(-5, typeof(InvalidAmountException))]
    public async Task Transfer_Invalid_IsRejected_AndAudited(decimal amount, Type expected)
    {
        var sender = await NewWalletAsync("putri", 100_000);
        var receiver = await NewWalletAsync("agus");
        var receiverNumber = (await WalletAsync(receiver)).WalletNumber;

        var ex = await Assert.ThrowsAnyAsync<WalletException>(() =>
            _transfers.TransferAsync(new TransferRequest(sender, receiverNumber, amount, null, null)));

        Assert.IsType(expected, ex);
        Assert.Equal(100_000m, (await WalletAsync(sender)).Balance);
        Assert.Equal(0m, (await WalletAsync(receiver)).Balance);
        Assert.DoesNotContain(_db.Transactions, t => t.Type == TransactionType.Transfer);
        var log = Assert.Single(_db.AuditLogs, a => a.Action == AuditActions.TransferRejected);
        Assert.Equal(AuditOutcome.Rejected, log.Outcome);
    }

    [Fact]
    public async Task Transfer_ToSelf_Rejected()
    {
        var sender = await NewWalletAsync("maya", 10_000);
        var number = (await WalletAsync(sender)).WalletNumber;
        await Assert.ThrowsAsync<SelfTransferException>(() => _transfers.TransferAsync(new TransferRequest(sender, number, 1_000, null, null)));
    }

    [Fact]
    public async Task Transfer_FromFrozenWallet_Rejected()
    {
        var sender = await NewWalletAsync("dimas", 10_000);
        var receiver = await NewWalletAsync("nadia");
        var w = await _db.Wallets.SingleAsync(x => x.Id == sender);
        w.Status = WalletStatus.Frozen;
        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();

        var receiverNumber = (await WalletAsync(receiver)).WalletNumber;
        await Assert.ThrowsAsync<WalletInactiveException>(() =>
            _transfers.TransferAsync(new TransferRequest(sender, receiverNumber, 1_000, null, null)));
    }

    [Fact]
    public async Task History_FiltersAndPaginates()
    {
        var a = await NewWalletAsync("aaa", 300_000);
        var b = await NewWalletAsync("bbb");
        var bNumber = (await WalletAsync(b)).WalletNumber;
        for (var i = 1; i <= 3; i++)
            await _transfers.TransferAsync(new TransferRequest(a, bNumber, i * 10_000, null, null));

        var page = await _wallets.GetTransactionsAsync(a, new HistoryQuery { Page = 1, PageSize = 2 });
        Assert.Equal(4, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Equal(2, page.Items.Count);

        var outOnly = await _wallets.GetTransactionsAsync(a, new HistoryQuery { Direction = MutationDirection.Out, SortBy = "amount", SortDir = "asc" });
        Assert.Equal([10_000m, 20_000m, 30_000m], outOnly.Items.Select(t => t.Amount));

        var mutations = await _wallets.GetMutationsAsync(b, new HistoryQuery { Type = TransactionType.Transfer });
        Assert.Equal(3, mutations.TotalItems);
        Assert.All(mutations.Items, m => Assert.Equal(MutationDirection.In, m.Direction));

        await Assert.ThrowsAsync<WalletException>(() => _wallets.GetTransactionsAsync(a, new HistoryQuery { SortBy = "foo" }));
    }
}
