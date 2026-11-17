namespace Ewallet.Dtos.PaymentMethods;

public record PaymentMethodResponse(int Id, string Code, string Name, string? ProviderName);