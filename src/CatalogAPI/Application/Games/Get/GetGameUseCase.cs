using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Games.Details;
using CatalogAPI.Application.Abstractions.Caching;

namespace CatalogAPI.Application.Games.Get
{
    public sealed class GetGameUseCase
    {
        private readonly IGameRepository _gameRepository;
        private readonly IGameDetailsRepository _detailsRepository;
        private readonly ILogger<GetGameUseCase> _logger;
        private readonly IGameCache _cache;

        public GetGameUseCase(
            IGameRepository gameRepository,
            IGameDetailsRepository detailsRepository,
            ILogger<GetGameUseCase> logger,
            IGameCache cache)
        {
            _gameRepository = gameRepository;
            _detailsRepository = detailsRepository;
            _logger = logger;
            _cache = cache;
        }

        public async Task<GetGameResult> ExecuteAsync(
            Guid gameId,
            CancellationToken cancellationToken = default)
        {
            var cached = await _cache.GetAsync(gameId, cancellationToken);
            if (cached is not null) return cached;

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
                _logger.LogWarning("MongoDB indisponível para o jogo {GameId}; retornando somente os dados SQL.", gameId);
            }

            var result = new GetGameResult(
                game.Id,
                game.Title,
                game.Description,
                game.Price,
                details,
                detailsStatus);
            if (detailsStatus != "unavailable")
                await _cache.SetAsync(result, cancellationToken);
            return result;
        }
    }
}
