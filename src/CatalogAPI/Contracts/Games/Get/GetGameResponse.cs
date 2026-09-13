using CatalogAPI.Application.Games.Details;

namespace CatalogAPI.Api.Contracts.Games.Get
{
    public sealed record GetGameResponse(
        Guid GameId,
        string Title,
        string Description,
        decimal Price,
        GameDetailsResult? Details,
        string DetailsStatus);
}

