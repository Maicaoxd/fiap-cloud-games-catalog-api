namespace CatalogAPI.Application.Payments.ProcessPaymentResult
{
    public sealed record ProcessPaymentResultCommand(
        Guid OrderId,
        Guid UserId,
        decimal TotalPrice,
        string Status,
        DateTime ProcessedAt);
}
