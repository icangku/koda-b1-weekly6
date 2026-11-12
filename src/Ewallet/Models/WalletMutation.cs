namespace Ewallet.Models;

public class WalletMutation
{
  public long Id {get; set;}
  public long TransactionId {get; set;}
  public long WalletId {get; set;}
  public MutationDirection Direction {get; set;}
  public decimal Amount {get; set;}
  public decimal BalanceBefore {get; set;}
  public decimal BalanceAfter {get; set;}
  public DateTime CreatedAtUtc {get; set;}
  public Transaction Transaction {get; set;} = null!;
  public Wallet Wallet {get; set;} = null!;
}