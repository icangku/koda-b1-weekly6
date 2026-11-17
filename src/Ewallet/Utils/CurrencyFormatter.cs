using System.Globalization;

namespace Ewallet.Utils;

public static class CurrencyFormatter
{
    private static readonly NumberFormatInfo Format = new()
    {
        NumberGroupSeparator = ".",
        NumberDecimalSeparator = ",",
        NumberDecimalDigits = 0
    };

    public static string Rupiah(long amount) => "Rp" + amount.ToString("N0", Format);

    public static string Rupiah(decimal amount)
        => "Rp" + amount.ToString(decimal.Truncate(amount) == amount ? "N0" : "N2", Format);
}
