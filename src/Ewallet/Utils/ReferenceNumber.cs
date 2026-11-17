namespace Ewallet.Utils;

public static class ReferenceNumber
{
  // e.g. TRF-20261117083015-9F3A1C2B (max 50 chars, UNIQUE index guards collisions)
  public static string New(string prefix, DateTime nowUtc)
    => $"{prefix}-{nowUtc:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
