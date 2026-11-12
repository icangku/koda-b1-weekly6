namespace Ewallet.Models;

public enum UserStatus { Active, Suspended}
public enum WalletStatus { Active, Frozen, Closed}
public enum TransactionType { TopUp, Transfer}
public enum TransactionStatus { Pending, Success, Failed}
public enum MutationDirection { In, Out}
public enum TopUpStatus {Pending, Paid, Failed, Expired}
public enum AuditOutcome { Success, Rejected, Failed}
