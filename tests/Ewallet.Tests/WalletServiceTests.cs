using Ewallet.Exceptions;
using Ewallet.Interfaces;
using Ewallet.Models;
using Ewallet.Repositories;
using Ewallet.Services;

namespace Ewallet.Tests;

public class WalletServiceTests
{
    private readonly FailingTransactionRepository _transactions = new();
    private readonly WalletService _service;

    public WalletServiceTests()
    {
        var accounts = new InMemoryAccountRepository();
        var wallets = new InMemoryWalletRepository();

        var andi = new Account(1, "Andi");
        var budi = new Account(2, "Budi");
        accounts.Save(andi);
        accounts.Save(budi);
        wallets.Save(new Wallet(andi, 100_000));
        wallets.Save(new Wallet(budi, 50_000));

        _service = new WalletService(accounts, wallets, _transactions);
    }

    private long Balance(int id) => _service.GetWallet(id).Balance;

    [Fact]
    public void TopUpAddsBalance()
    {
        _service.TopUp(1, 25_000);
        Assert.Equal(125_000, Balance(1));
        Assert.Single(_service.GetHistory(1));
    }

    [Fact]
    public void TopUpZeroIsRejected() =>
        Assert.Throws<InvalidAmountException>(() => _service.TopUp(1, 0));

    [Fact]
    public void TransferMovesBalance()
    {
        _service.Transfer(1, "WAL002", 40_000);
        Assert.Equal(60_000, Balance(1));
        Assert.Equal(90_000, Balance(2));
    }

    [Fact]
    public void TransferMoreThanBalanceIsRejected()
    {
        Assert.Throws<InsufficientBalanceException>(() => _service.Transfer(1, "WAL002", 200_000));
        Assert.Equal(100_000, Balance(1));
        Assert.Equal(50_000, Balance(2));
    }

    [Fact]
    public void TransferToSelfIsRejected() =>
        Assert.Throws<SelfTransferException>(() => _service.Transfer(1, "WAL001", 1_000));

    [Fact]
    public void TransferToUnknownWalletIsRejected() =>
        Assert.Throws<WalletNotFoundException>(() => _service.Transfer(1, "WAL999", 1_000));

    [Fact]
    public void TransferIsRolledBackWhenRepositoryFails()
    {
        _transactions.ShouldFail = true;
        Assert.Throws<WalletException>(() => _service.Transfer(1, "WAL002", 30_000));
        Assert.Equal(100_000, Balance(1));
        Assert.Equal(50_000, Balance(2));
        Assert.Empty(_service.GetHistory(1));
    }

    [Fact]
    public void TopUpIsRolledBackWhenRepositoryFails()
    {
        _transactions.ShouldFail = true;
        Assert.Throws<WalletException>(() => _service.TopUp(1, 10_000));
        Assert.Equal(100_000, Balance(1));
    }

    [Fact]
    public void HistoryCanBeFilteredByIncomeAndExpense()
    {
        _service.TopUp(1, 10_000);
        _service.Transfer(1, "WAL002", 20_000);
        _service.Transfer(2, "WAL001", 5_000);

        Assert.Equal(2, _service.GetIncomeHistory(1).Count);
        Assert.Single(_service.GetExpenseHistory(1));
        Assert.Equal(15_000, _service.GetTotalIncome(1));
        Assert.Equal(20_000, _service.GetTotalExpense(1));
    }

    private class FailingTransactionRepository : ITransactionRepository
    {
        private readonly InMemoryTransactionRepository _inner = new();
        public bool ShouldFail { get; set; }

        public void Add(Transaction transaction)
        {
            if (ShouldFail) throw new InvalidOperationException("gagal menyimpan transaksi");
            _inner.Add(transaction);
        }

        public IReadOnlyList<Transaction> FindByAccountId(int accountId) => _inner.FindByAccountId(accountId);
    }
}
