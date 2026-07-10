namespace CatalogAPI.Application.Games.List
{
    public sealed record ListGameResult(
        Guid GameId,
        string Title,
        string Description,
        decimal Price);
}

