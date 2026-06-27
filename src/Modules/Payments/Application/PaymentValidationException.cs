namespace Bartrix.Modules.Payments.Application;

public sealed class PaymentValidationException(string message) : Exception(message);
