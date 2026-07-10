namespace FiapCloudGames.Contracts.Events
{
    public sealed record OrderPlacedEvent(
        Guid OrderId,
        Guid UserId,
        IReadOnlyCollection<OrderPlacedGameEventItem> Games,
        decimal TotalPrice,
        DateTime CreatedAt);

    public sealed record OrderPlacedGameEventItem(
        Guid GameId,
        decimal Price);
}
