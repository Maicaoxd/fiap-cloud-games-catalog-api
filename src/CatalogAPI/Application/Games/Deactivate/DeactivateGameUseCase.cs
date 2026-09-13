using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Abstractions.Caching;

namespace CatalogAPI.Application.Games.Deactivate
{
    public sealed class DeactivateGameUseCase
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameCache _cache;

        public DeactivateGameUseCase(IGameRepository gameRepository, IGameCache cache)
        {
            _gameRepository = gameRepository;
            _cache = cache;
        }

        public async Task ExecuteAsync(
            DeactivateGameCommand command,
            CancellationToken cancellationToken = default)
        {
            var game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken);

            if (game is null)
                throw new GameNotFoundException();

            if (!game.IsActive)
            {
                await _cache.InvalidateAsync(game.Id, CancellationToken.None);
                return;
            }

            game.Deactivate(command.DeactivatedBy);

            await _gameRepository.UpdateAsync(game, cancellationToken);
            await _cache.InvalidateAsync(game.Id, CancellationToken.None);
        }
    }
}

