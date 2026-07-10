using CatalogAPI.Domain.Games;

namespace CatalogAPI.Application.Abstractions.Persistence
{
    public interface IGameRepository
    {
        Task<bool> ExistsByTitleAsync(string title, CancellationToken cancellationToken = default);

        Task<bool> ExistsByTitleForAnotherGameAsync(
            string title,
            Guid gameId,
            CancellationToken cancellationToken = default);

        Task<Game?> GetByIdAsync(Guid gameId, CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Game>> ListByIdsAsync(
            IReadOnlyCollection<Guid> gameIds,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyCollection<Game>> ListActiveAsync(CancellationToken cancellationToken = default);

        Task AddAsync(Game game, CancellationToken cancellationToken = default);

        Task UpdateAsync(Game game, CancellationToken cancellationToken = default);
    }
}
