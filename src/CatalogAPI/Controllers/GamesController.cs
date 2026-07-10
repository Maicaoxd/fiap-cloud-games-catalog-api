using CatalogAPI.Api.Common;
using CatalogAPI.Api.Contracts.Games.Create;
using CatalogAPI.Api.Contracts.Games.Get;
using CatalogAPI.Api.Contracts.Games.List;
using CatalogAPI.Api.Contracts.Games.Update;
using CatalogAPI.Application.Games.Create;
using CatalogAPI.Application.Games.Deactivate;
using CatalogAPI.Application.Games.Get;
using CatalogAPI.Application.Games.List;
using CatalogAPI.Application.Games.Update;
using CatalogAPI.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Api.Controllers
{
    [ApiController]
    [Route("api/games")]
    public sealed class GamesController : ControllerBase
    {
        private readonly CreateGameUseCase _createGameUseCase;
        private readonly DeactivateGameUseCase _deactivateGameUseCase;
        private readonly GetGameUseCase _getGameUseCase;
        private readonly ListGamesUseCase _listGamesUseCase;
        private readonly UpdateGameUseCase _updateGameUseCase;

        public GamesController(
            CreateGameUseCase createGameUseCase,
            DeactivateGameUseCase deactivateGameUseCase,
            GetGameUseCase getGameUseCase,
            ListGamesUseCase listGamesUseCase,
            UpdateGameUseCase updateGameUseCase)
        {
            _createGameUseCase = createGameUseCase;
            _deactivateGameUseCase = deactivateGameUseCase;
            _getGameUseCase = getGameUseCase;
            _listGamesUseCase = listGamesUseCase;
            _updateGameUseCase = updateGameUseCase;
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(IReadOnlyCollection<ListGameResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<ListGameResponse>>> ListAsync(
            CancellationToken cancellationToken)
        {
            var result = await _listGamesUseCase.ExecuteAsync(cancellationToken);
            var response = result
                .Select(game => new ListGameResponse(
                    game.GameId,
                    game.Title,
                    game.Description,
                    game.Price))
                .ToList();

            return Ok(response);
        }

        [HttpGet("{gameId:guid}")]
        [Authorize]
        [ProducesResponseType(typeof(GetGameResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetGameResponse>> GetByIdAsync(
            Guid gameId,
            CancellationToken cancellationToken)
        {
            var result = await _getGameUseCase.ExecuteAsync(gameId, cancellationToken);
            var response = new GetGameResponse(
                result.GameId,
                result.Title,
                result.Description,
                result.Price);

            return Ok(response);
        }

        [HttpPost]
        [Authorize(Roles = nameof(UserRole.Administrator))]
        [ProducesResponseType(typeof(CreateGameResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateGameResponse>> CreateAsync(
            CreateGameRequest request,
            CancellationToken cancellationToken)
        {
            var createdBy = User.GetRequiredUserId();

            var command = new CreateGameCommand(
                request.Title,
                request.Description,
                request.Price,
                createdBy);

            var result = await _createGameUseCase.ExecuteAsync(command, cancellationToken);
            var response = new CreateGameResponse(result.GameId);

            return Created($"/api/games/{response.GameId}", response);
        }

        [HttpPut("{gameId:guid}")]
        [Authorize(Roles = nameof(UserRole.Administrator))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateAsync(
            Guid gameId,
            UpdateGameRequest request,
            CancellationToken cancellationToken)
        {
            var updatedBy = User.GetRequiredUserId();

            var command = new UpdateGameCommand(
                gameId,
                request.Title,
                request.Description,
                request.Price,
                updatedBy);

            await _updateGameUseCase.ExecuteAsync(command, cancellationToken);

            return NoContent();
        }

        [HttpPatch("{gameId:guid}/deactivate")]
        [Authorize(Roles = nameof(UserRole.Administrator))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeactivateAsync(
            Guid gameId,
            CancellationToken cancellationToken)
        {
            var deactivatedBy = User.GetRequiredUserId();
            var command = new DeactivateGameCommand(gameId, deactivatedBy);

            await _deactivateGameUseCase.ExecuteAsync(command, cancellationToken);

            return NoContent();
        }
    }
}

