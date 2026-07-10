namespace CatalogAPI.Application.Purchases.PurchaseGame
{
    public sealed record PurchaseGameCommand(
        Guid UserId,
        IReadOnlyCollection<Guid> GameIds);
}
