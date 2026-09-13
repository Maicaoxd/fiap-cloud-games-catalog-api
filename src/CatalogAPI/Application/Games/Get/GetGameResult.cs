using CatalogAPI.Application.Games.Details;

namespace CatalogAPI.Application.Games.Get
{
    public sealed record GetGameResult(
        Guid GameId,
        string Title,
        string Description,
        decimal Price,
        GameDetailsResult? Details,
        string DetailsStatus);
}

