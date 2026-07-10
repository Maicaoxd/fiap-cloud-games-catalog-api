using CatalogAPI.Domain.Libraries;

namespace CatalogAPI.Application.Abstractions.Persistence
{
    public interface ILibraryRepository
    {
        Task<bool> ExistsByUserAndGameAsync(
            Guid userId,
            Guid gameId,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<LibraryGameReadModel>> ListGamesByUserIdAsync(
            Guid userId,
            CancellationToken cancellationToken = default);

        Task AddAsync(Library library, CancellationToken cancellationToken = default);
    }
}

