namespace CatalogAPI.Application.Abstractions.Persistence
{
    public sealed record LibraryGameReadModel(
        Guid LibraryId,
        Guid GameId,
        string Title,
        string Description,
        decimal Price,
        bool IsActive,
        DateTime AcquiredAt);
}

