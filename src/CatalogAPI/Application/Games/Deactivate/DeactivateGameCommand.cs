namespace CatalogAPI.Application.Games.Deactivate
{
    public sealed record DeactivateGameCommand(
        Guid GameId,
        Guid DeactivatedBy);
}

