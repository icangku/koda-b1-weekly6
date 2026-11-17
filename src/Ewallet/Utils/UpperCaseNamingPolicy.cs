using System.Text.Json;

namespace Ewallet.Utils;

public class UpperCaseNamingPolicy : JsonNamingPolicy
{
  public override string ConvertName(string name) => name.ToUpperInvariant();
}