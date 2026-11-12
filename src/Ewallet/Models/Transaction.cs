namespace Ewallet.Models;

public class Transaction
{
    public int Id { get; set; }
    public string ReferenceNo {get; set;} = null!;
    public long? SenderWalletId { get; set; }
    public long ReceiverWalletId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public TransactionStatus Status {get; set;}
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public Wallet? SenderWallet {get; set;}
    public Wallet ReceiverWallet {get; set;} = null!;
    public ICollection<WalletMutation> Mutations {get; set;} = [];
    public TopUpRequest? TopUpRequest {get; set;}
}