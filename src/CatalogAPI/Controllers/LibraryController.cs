using CatalogAPI.Api.Common;
using CatalogAPI.Api.Contracts.Libraries.List;
using CatalogAPI.Api.Contracts.Purchases;
using CatalogAPI.Application.Libraries.List;
using CatalogAPI.Application.Purchases.PurchaseGame;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Api.Controllers
{
    [ApiController]
    [Route("api/library")]
    public sealed class LibraryController : ControllerBase
    {
        private readonly ListLibraryGamesUseCase _listLibraryGamesUseCase;
        private readonly PurchaseGameUseCase _purchaseGameUseCase;

        public LibraryController(
            ListLibraryGamesUseCase listLibraryGamesUseCase,
            PurchaseGameUseCase purchaseGameUseCase)
        {
            _listLibraryGamesUseCase = listLibraryGamesUseCase;
            _purchaseGameUseCase = purchaseGameUseCase;
        }

        [HttpGet("games")]
        [Authorize]
        [ProducesResponseType(typeof(IReadOnlyCollection<ListLibraryGameResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<ListLibraryGameResponse>>> ListAsync(
            CancellationToken cancellationToken)
        {
            var userId = User.GetRequiredUserId();
            var result = await _listLibraryGamesUseCase.ExecuteAsync(userId, cancellationToken);
            var response = result
                .Select(libraryGame => new ListLibraryGameResponse(
                    libraryGame.LibraryId,
                    libraryGame.GameId,
                    libraryGame.Title,
                    libraryGame.Description,
                    libraryGame.Price,
                    libraryGame.IsActive,
                    libraryGame.AcquiredAt))
                .ToList();

            return Ok(response);
        }

        [HttpPost("games/purchase")]
        [Authorize]
        [ProducesResponseType(typeof(PurchaseGameResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PurchaseGameResponse>> PurchaseAsync(
            PurchaseGamesRequest request,
            CancellationToken cancellationToken)
        {
            var userId = User.GetRequiredUserId();
            var command = new PurchaseGameCommand(userId, request.GameIds);
            var result = await _purchaseGameUseCase.ExecuteAsync(command, cancellationToken);
            var response = new PurchaseGameResponse(result.OrderId);

            return Accepted($"/api/orders/{response.OrderId}", response);
        }
    }
}
