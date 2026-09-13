using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Abstractions.Caching;

namespace CatalogAPI.Application.Games.Update
{
    public sealed class UpdateGameUseCase
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameCache _cache;

        public UpdateGameUseCase(IGameRepository gameRepository, IGameCache cache)
        {
            _gameRepository = gameRepository;
            _cache = cache;
        }

        public async Task ExecuteAsync(
            UpdateGameCommand command,
            CancellationToken cancellationToken = default)
        {
            var game = await _gameRepository.GetByIdAsync(command.GameId, cancellationToken);

            if (game is null)
                throw new GameNotFoundException();

            if (await _gameRepository.ExistsByTitleForAnotherGameAsync(
                    command.Title,
                    command.GameId,
                    cancellationToken))
                throw new GameTitleAlreadyRegisteredException();

            game.Update(
                command.Title,
                command.Description,
                command.Price,
                command.UpdatedBy);

            await _gameRepository.UpdateAsync(game, cancellationToken);
            await _cache.InvalidateAsync(game.Id, CancellationToken.None);
        }
    }
}

