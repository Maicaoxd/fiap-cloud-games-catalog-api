using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;

namespace CatalogAPI.Application.Games.Get
{
    public sealed class GetGameUseCase
    {
        private readonly IGameRepository _gameRepository;

        public GetGameUseCase(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<GetGameResult> ExecuteAsync(
            Guid gameId,
            CancellationToken cancellationToken = default)
        {
            var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

            if (game is null || !game.IsActive)
                throw new GameNotFoundException();

            return new GetGameResult(
                game.Id,
                game.Title,
                game.Description,
                game.Price);
        }
    }
}

