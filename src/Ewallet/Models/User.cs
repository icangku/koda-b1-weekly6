namespace Ewallet.Models;

public class User
{
    public long Id { get; set; }
    public string Username {get; set;} = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? PasswordHash {get; set;}
    public UserStatus Status {get; set;} = UserStatus.Active;
    public DateTime CreatedAtUtc {get; set;}
    public DateTime? UpdatedAtUtc {get; set;}
    public Wallet? Wallet { get; set; }
    public ICollection<AuditLog> AuditLogs {get; set;} = [];
}