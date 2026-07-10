namespace CatalogAPI.Application.Games.Create
{
    public sealed record CreateGameCommand(
        string Title,
        string Description,
        decimal Price,
        Guid CreatedBy);
}

