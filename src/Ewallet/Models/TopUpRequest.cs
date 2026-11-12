namespace Ewallet.Models;

public class TopUpRequest
{
  public long Id {get; set;}
  public long TransactionId {get; set;}
  public long WalletId {get; set;}
  public int PaymentMethodId {get; set;}
  public decimal Amount {get; set;}
  public TopUpStatus Status{get; set;}
  public string? ProviderReference {get; set;}
  public DateTime CreatedAtUtc {get; set;}
  public DateTime? ExpiredAtUtc {get; set;}
  public DateTime? PaidAtUtc {get; set;}

  public Transaction Transaction {get; set; } = null!;
  public Wallet Wallet {get; set;} = null!;
  public PaymentMethod PaymentMethod {get; set;} = null!;
}