namespace CatalogAPI.Application.Libraries.List
{
    public sealed record ListLibraryGameResult(
        Guid LibraryId,
        Guid GameId,
        string Title,
        string Description,
        decimal Price,
        bool IsActive,
        DateTime AcquiredAt);
}

