namespace CatalogAPI.Api.Contracts.Games.List
{
    public sealed record ListGameResponse(
        Guid GameId,
        string Title,
        string Description,
        decimal Price);
}

