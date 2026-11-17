namespace Ewallet.Exceptions;

// Business-rule exceptions. Their messages are safe to return to the client and to store in AuditLogs.
public class WalletException : Exception
{
    public WalletException(string message) : base(message) { }
}

public class WalletNotFoundException : WalletException
{
    public WalletNotFoundException(string message = "Wallet tidak ditemukan.") : base(message) { }
}

public class UserNotFoundException : WalletException
{
    public UserNotFoundException(string message = "User tidak ditemukan.") : base(message) { }
}

public class TopUpNotFoundException : WalletException
{
    public TopUpNotFoundException(string message = "Top up tidak ditemukan.") : base(message) { }
}

public class PaymentMethodNotFoundException : WalletException
{
    public PaymentMethodNotFoundException(string message = "Metode pembayaran tidak ditemukan atau tidak aktif.") : base(message) { }
}

public class InsufficientBalanceException : WalletException
{
    public InsufficientBalanceException(string message = "Saldo tidak mencukupi.") : base(message) { }
}

public class InvalidAmountException : WalletException
{
    public InvalidAmountException(string message = "Nominal harus lebih besar dari 0.") : base(message) { }
}

public class SelfTransferException : WalletException
{
    public SelfTransferException(string message = "Tidak dapat melakukan transfer ke wallet sendiri.") : base(message) { }
}

public class WalletInactiveException : WalletException
{
    public WalletInactiveException(string message) : base(message) { }
}

public class ConflictException : WalletException
{
    public ConflictException(string message) : base(message) { }
}

/// <summary>Dev/Testing only: thrown after writes but before commit to demonstrate rollback.</summary>
public class SimulatedFailureException : Exception
{
    public SimulatedFailureException()
        : base("Simulasi kegagalan setelah saldo diubah. Seluruh perubahan dibatalkan (rollback).") { }
}
