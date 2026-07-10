using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Domain.Games;

namespace CatalogAPI.Application.Games.Create
{
    public sealed class CreateGameUseCase
    {
        private readonly IGameRepository _gameRepository;

        public CreateGameUseCase(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<CreateGameResult> ExecuteAsync(
            CreateGameCommand command,
            CancellationToken cancellationToken = default)
        {
            var game = Game.Create(
                command.Title,
                command.Description,
                command.Price,
                command.CreatedBy);

            if (await _gameRepository.ExistsByTitleAsync(game.Title, cancellationToken))
                throw new GameTitleAlreadyRegisteredException();

            await _gameRepository.AddAsync(game, cancellationToken);

            return new CreateGameResult(game.Id);
        }
    }
}

