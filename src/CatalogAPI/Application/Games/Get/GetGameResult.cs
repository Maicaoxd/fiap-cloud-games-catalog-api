namespace CatalogAPI.Application.Games.Get
{
    public sealed record GetGameResult(
        Guid GameId,
        string Title,
        string Description,
        decimal Price);
}

