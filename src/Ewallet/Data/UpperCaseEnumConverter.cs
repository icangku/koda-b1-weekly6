using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Ewallet.Data;

public class UpperCaseEnumConverter<TEnum> : ValueConverter<TEnum, string> where TEnum : struct, Enum
{
  public UpperCaseEnumConverter() : base(v => v.ToString().ToUpperInvariant(), v => Enum.Parse<TEnum>(v, true)){}
}