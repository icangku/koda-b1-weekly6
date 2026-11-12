namespace Ewallet.Models;

public class Wallet
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string WalletNumber { get; set; } = null!;
    public decimal Balance { get; set; }
    public WalletStatus Status {get; set;} = WalletStatus.Active;
    public byte[] RowVersion {get; set;} = null!;
    public DateTime CreatedAtUtc {get; set;}
    public DateTime? UpdatedAtUtc {get; set;}
    public User User {get; set;} = null!;
    public ICollection<Transaction> SentTransactions {get; set;} = [];
    public ICollection<Transaction> ReceivedTransaction {get; set;} = [];
    public ICollection<WalletMutation> Mutations {get; set;} = [];
}