namespace Ewallet.Models;

public class AuditLog
{
  public long Id {get; set; }
  public long? ActorUserId {get; set;}
  public long? TransactionId {get; set;}
  public string Action {get; set; } = null!;
  public string  EntityType {get; set;} = null!;
  public string? EntityId {get; set;}
  public AuditOutcome Outcome {get; set;}
  public string? Description {get; set;}
  public string? CorrelationId {get; set;}
  public DateTime CreatedAtUtc {get; set;}
  
  public User? ActorUser {get; set;}
  public Transaction? Transaction {get; set;}
}