using CatalogAPI.Application.Games.Details;
using CatalogAPI.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Api.Controllers;

[ApiController]
[Route("api/games/{gameId:guid}/details")]
public sealed class GameDetailsController(UpsertGameDetailsUseCase useCase) : ControllerBase
{
    [HttpPut]
    [Authorize(Roles = nameof(UserRole.Administrator))]
    [RequestSizeLimit(65536)]
    [ProducesResponseType(typeof(GameDetailsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GameDetailsResult>> UpsertAsync(Guid gameId, GameDetailsContent request, CancellationToken cancellationToken)
    {
        return Ok(await useCase.ExecuteAsync(gameId, request, cancellationToken));
    }
}
