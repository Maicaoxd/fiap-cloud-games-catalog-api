using CatalogAPI.Application.Games.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CatalogAPI.Api.Controllers;

[ApiController]
[Route("api/search")]
[Authorize]
public sealed class SearchController(SearchGamesUseCase useCase) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(GameSearchResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<GameSearchResult>> SearchAsync(
        [FromQuery(Name = "q")] string? query, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await useCase.ExecuteAsync(query, page, pageSize, cancellationToken));
}
