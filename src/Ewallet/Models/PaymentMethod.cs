namespace Ewallet.Models;

public class PaymentMethod
{
  public int Id {get; set;}
  public string Code {get; set;} = null!;
  public string Name {get; set;} = null!;
  public string? ProviderName {get; set;}
  public bool IsActive {get; set;}
  public DateTime CreatedAtUtc  {get; set;}
}