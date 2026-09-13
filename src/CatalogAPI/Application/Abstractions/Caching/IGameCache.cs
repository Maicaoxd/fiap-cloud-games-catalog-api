using CatalogAPI.Application.Games.Get;

namespace CatalogAPI.Application.Abstractions.Caching;

public interface IGameCache
{
    Task<GetGameResult?> GetAsync(Guid gameId, CancellationToken cancellationToken = default);
    Task SetAsync(GetGameResult game, CancellationToken cancellationToken = default);
    Task InvalidateAsync(Guid gameId, CancellationToken cancellationToken = default);
}
