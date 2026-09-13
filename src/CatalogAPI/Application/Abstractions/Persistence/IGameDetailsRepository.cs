using CatalogAPI.Application.Games.Details;

namespace CatalogAPI.Application.Abstractions.Persistence;

public interface IGameDetailsRepository
{
    Task<GameDetailsResult?> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task<GameDetailsResult> UpsertAsync(Guid gameId, GameDetailsContent content, CancellationToken cancellationToken = default);
}
