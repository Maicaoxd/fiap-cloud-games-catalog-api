using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;

namespace CatalogAPI.Application.Games.Details;

public sealed class UpsertGameDetailsUseCase(IGameRepository games, IGameDetailsRepository details)
{
    public async Task<GameDetailsResult> ExecuteAsync(Guid gameId, GameDetailsContent content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var normalized = content.NormalizeAndValidate();
        var game = await games.GetByIdAsync(gameId, cancellationToken);
        if (game is null || !game.IsActive) throw new GameNotFoundException();
        return await details.UpsertAsync(gameId, normalized, cancellationToken);
    }
}
