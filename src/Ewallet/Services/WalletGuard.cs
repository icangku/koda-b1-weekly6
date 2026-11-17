using Ewallet.Exceptions;
using Ewallet.Models;

namespace Ewallet.Services;

internal static class WalletGuard
{
  public static void EnsureActive(Wallet wallet, string role)
  {
    if (wallet.Status != WalletStatus.Active)
      throw new WalletInactiveException($"Wallet {role} berstatus {wallet.Status.ToString().ToUpperInvariant()}.");
    if (wallet.User is { Status: not UserStatus.Active })
      throw new WalletInactiveException($"Pemilik wallet {role} berstatus {wallet.User.Status.ToString().ToUpperInvariant()}.");
  }

  public static void EnsureValidAmount(decimal amount)
  {
    if (amount <= 0) throw new InvalidAmountException();
    if (decimal.Round(amount, 2) != amount) throw new InvalidAmountException("Nominal maksimal 2 angka desimal.");
    if (amount > 9_999_999_999_999_999.99m) throw new InvalidAmountException("Nominal terlalu besar.");
  }
}
