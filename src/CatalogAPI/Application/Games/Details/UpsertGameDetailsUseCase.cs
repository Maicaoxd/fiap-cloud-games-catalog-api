using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Abstractions.Caching;

namespace CatalogAPI.Application.Games.Details;

public sealed class UpsertGameDetailsUseCase(IGameRepository games, IGameDetailsRepository details, IGameCache cache)
{
    public async Task<GameDetailsResult> ExecuteAsync(Guid gameId, GameDetailsContent content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var normalized = content.NormalizeAndValidate();
        var game = await games.GetByIdAsync(gameId, cancellationToken);
        if (game is null || !game.IsActive) throw new GameNotFoundException();
        var result = await details.UpsertAsync(gameId, normalized, cancellationToken);
        await cache.InvalidateAsync(gameId, CancellationToken.None);
        return result;
    }
}
