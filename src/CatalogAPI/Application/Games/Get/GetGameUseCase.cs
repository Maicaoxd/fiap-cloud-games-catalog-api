using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Games.Details;

namespace CatalogAPI.Application.Games.Get
{
    public sealed class GetGameUseCase
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameDetailsRepository _detailsRepository;
        private readonly ILogger<GetGameUseCase> _logger;

        public GetGameUseCase(IGameRepository gameRepository, IGameDetailsRepository detailsRepository, ILogger<GetGameUseCase> logger)
        {
            _gameRepository = gameRepository;
            _detailsRepository = detailsRepository;
            _logger = logger;
        }

        public async Task<GetGameResult> ExecuteAsync(
            Guid gameId,
            CancellationToken cancellationToken = default)
        {
            var game = await _gameRepository.GetByIdAsync(gameId, cancellationToken);

            if (game is null || !game.IsActive)
                throw new GameNotFoundException();

            GameDetailsResult? details = null;
            var detailsStatus = "notConfigured";
            try
            {
                details = await _detailsRepository.GetByGameIdAsync(gameId, cancellationToken);
                if (details is not null) detailsStatus = "available";
            }
            catch (GameDetailsUnavailableException)
            {
                detailsStatus = "unavailable";
                _logger.LogWarning("MongoDB unavailable for game {GameId}; returning SQL data only.", gameId);
            }

            return new GetGameResult(
                game.Id,
                game.Title,
                game.Description,
                game.Price,
                details,
                detailsStatus);
        }
    }
}

