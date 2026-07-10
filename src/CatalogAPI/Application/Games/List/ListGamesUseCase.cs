using CatalogAPI.Application.Abstractions.Persistence;

namespace CatalogAPI.Application.Games.List
{
    public sealed class ListGamesUseCase
    {
        private readonly IGameRepository _gameRepository;

        public ListGamesUseCase(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public async Task<IReadOnlyCollection<ListGameResult>> ExecuteAsync(
            CancellationToken cancellationToken = default)
        {
            var games = await _gameRepository.ListActiveAsync(cancellationToken);

            return games
                .Select(game => new ListGameResult(
                    game.Id,
                    game.Title,
                    game.Description,
                    game.Price))
                .ToList();
        }
    }
}

