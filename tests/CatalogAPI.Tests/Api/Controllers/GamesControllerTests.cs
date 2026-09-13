using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CatalogAPI.Api.Contracts.Games.Create;
using CatalogAPI.Api.Contracts.Games.Get;
using CatalogAPI.Api.Contracts.Games.List;
using CatalogAPI.Api.Contracts.Games.Update;
using CatalogAPI.Api.Controllers;
using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Games.Create;
using CatalogAPI.Application.Games.Deactivate;
using CatalogAPI.Application.Games.Get;
using Microsoft.Extensions.Logging.Abstractions;
using CatalogAPI.Application.Games.List;
using CatalogAPI.Application.Games.Update;
using CatalogAPI.Domain.Games;
using CatalogAPI.Domain.Users;
using CatalogAPI.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Api.Controllers;

[Trait("Category", "Unit")]
public sealed class GamesControllerTests
{
    [Fact]
    public async Task CreateAsync_QuandoAdminAutenticadoEJogoValido_DeveRetornarCreatedComGameId()
    {
        var adminId = Guid.NewGuid();
        var gameRepository = Substitute.For<IGameRepository>();
        Game? addedGame = null;

        gameRepository
            .ExistsByTitleAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        gameRepository
            .AddAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                addedGame = callInfo.ArgAt<Game>(0);
                return Task.CompletedTask;
            });

        var controller = CreateController(gameRepository, adminId);
        var request = new CreateGameRequest(
            "Stardew Valley",
            "Simulador de fazenda e vida no campo.",
            24.90m);

        var actionResult = await controller.CreateAsync(request, CancellationToken.None);

        var createdResult = actionResult.Result.ShouldBeOfType<CreatedResult>();
        var response = createdResult.Value.ShouldBeOfType<CreateGameResponse>();

        response.GameId.ShouldNotBe(Guid.Empty);
        createdResult.Location.ShouldBe($"/api/games/{response.GameId}");
        addedGame.ShouldNotBeNull();
        addedGame!.CreatedBy.ShouldBe(adminId);
        addedGame.Title.ShouldBe("Stardew Valley");
        addedGame.Description.ShouldBe("Simulador de fazenda e vida no campo.");
        addedGame.Price.ShouldBe(24.90m);
        await gameRepository.Received(1).AddAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ListAsync_QuandoUsuarioEstiverAutenticado_DeveRetornarOkComJogos()
    {
        var firstGame = Game.Create(
            "Hades",
            "Roguelike de acao.",
            49.90m,
            Guid.NewGuid());
        var secondGame = Game.Create(
            "Stardew Valley",
            "Simulador de fazenda e vida no campo.",
            24.90m,
            Guid.NewGuid());

        var gameRepository = Substitute.For<IGameRepository>();
        gameRepository
            .ListActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { firstGame, secondGame });

        var controller = CreateController(gameRepository, Guid.NewGuid(), nameof(UserRole.User));

        var actionResult = await controller.ListAsync(CancellationToken.None);

        var okResult = actionResult.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<List<ListGameResponse>>();

        response.Count.ShouldBe(2);
        response.ShouldContain(game => game.GameId == firstGame.Id);
        response.ShouldContain(game => game.GameId == secondGame.Id);
        await gameRepository.Received(1).ListActiveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_QuandoUsuarioAutenticadoEJogoExistir_DeveRetornarOkComJogo()
    {
        var game = Game.Create(
            "Hades",
            "Roguelike de acao.",
            49.90m,
            Guid.NewGuid());
        var gameRepository = Substitute.For<IGameRepository>();

        gameRepository
            .GetByIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(game);

        var controller = CreateController(gameRepository, Guid.NewGuid(), nameof(UserRole.User));

        var actionResult = await controller.GetByIdAsync(game.Id, CancellationToken.None);

        var okResult = actionResult.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<GetGameResponse>();

        response.GameId.ShouldBe(game.Id);
        response.Title.ShouldBe("Hades");
        response.Description.ShouldBe("Roguelike de acao.");
        response.Price.ShouldBe(49.90m);
        await gameRepository.Received(1).GetByIdAsync(game.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_QuandoAdminAutenticadoEJogoExistir_DeveRetornarNoContent()
    {
        var adminId = Guid.NewGuid();
        var game = Game.Create(
            "Stardew Valley",
            "Simulador de fazenda.",
            24.90m,
            Guid.NewGuid());
        var gameRepository = Substitute.For<IGameRepository>();

        gameRepository
            .GetByIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(game);
        gameRepository
            .ExistsByTitleForAnotherGameAsync(Arg.Any<string>(), game.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        var controller = CreateController(gameRepository, adminId);
        var request = new UpdateGameRequest(
            "Stardew Valley Deluxe",
            "Simulador de fazenda com conteudo extra.",
            39.90m);

        var actionResult = await controller.UpdateAsync(game.Id, request, CancellationToken.None);

        actionResult.ShouldBeOfType<NoContentResult>();
        game.Title.ShouldBe("Stardew Valley Deluxe");
        game.Description.ShouldBe("Simulador de fazenda com conteudo extra.");
        game.Price.ShouldBe(39.90m);
        game.UpdatedBy.ShouldBe(adminId);
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateAsync_QuandoAdminAutenticadoEJogoExistir_DeveRetornarNoContent()
    {
        var adminId = Guid.NewGuid();
        var game = Game.Create(
            "Stardew Valley",
            "Simulador de fazenda.",
            24.90m,
            Guid.NewGuid());
        var gameRepository = Substitute.For<IGameRepository>();

        gameRepository
            .GetByIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(game);

        var controller = CreateController(gameRepository, adminId);

        var actionResult = await controller.DeactivateAsync(game.Id, CancellationToken.None);

        actionResult.ShouldBeOfType<NoContentResult>();
        game.IsActive.ShouldBeFalse();
        game.UpdatedBy.ShouldBe(adminId);
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ListAsync_DeveExigirUsuarioAutenticado()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.ListAsync));

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        authorizeAttribute.Roles.ShouldBeNull();
    }

    [Fact]
    public void GetByIdAsync_DeveExigirUsuarioAutenticado()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.GetByIdAsync));

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        authorizeAttribute.Roles.ShouldBeNull();
    }

    [Fact]
    public void CreateAsync_DeveExigirRoleAdministrator()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.CreateAsync));

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        authorizeAttribute.Roles.ShouldBe(nameof(UserRole.Administrator));
    }

    [Fact]
    public void UpdateAsync_DeveExigirRoleAdministrator()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.UpdateAsync));

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        authorizeAttribute.Roles.ShouldBe(nameof(UserRole.Administrator));
    }

    [Fact]
    public void DeactivateAsync_DeveExigirRoleAdministrator()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.DeactivateAsync));

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        authorizeAttribute.Roles.ShouldBe(nameof(UserRole.Administrator));
    }

    [Fact]
    public void CreateAsync_DeveDocumentarRespostasEsperadasNoSwagger()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.CreateAsync));

        var responseTypes = GetResponseTypes(method!);

        responseTypes[StatusCodes.Status201Created].Type.ShouldBe(typeof(CreateGameResponse));
        responseTypes[StatusCodes.Status400BadRequest].Type.ShouldBe(typeof(ValidationProblemDetails));
        responseTypes[StatusCodes.Status401Unauthorized].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status403Forbidden].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status409Conflict].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status500InternalServerError].Type.ShouldBe(typeof(ProblemDetails));
    }

    [Fact]
    public void ListAsync_DeveDocumentarRespostasEsperadasNoSwagger()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.ListAsync));

        var responseTypes = GetResponseTypes(method!);

        responseTypes[StatusCodes.Status200OK].Type.ShouldBe(typeof(IReadOnlyCollection<ListGameResponse>));
        responseTypes[StatusCodes.Status401Unauthorized].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status500InternalServerError].Type.ShouldBe(typeof(ProblemDetails));
    }

    [Fact]
    public void GetByIdAsync_DeveDocumentarRespostasEsperadasNoSwagger()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.GetByIdAsync));

        var responseTypes = GetResponseTypes(method!);

        responseTypes[StatusCodes.Status200OK].Type.ShouldBe(typeof(GetGameResponse));
        responseTypes[StatusCodes.Status401Unauthorized].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status404NotFound].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status500InternalServerError].Type.ShouldBe(typeof(ProblemDetails));
    }

    [Fact]
    public void UpdateAsync_DeveDocumentarRespostasEsperadasNoSwagger()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.UpdateAsync));

        var responseTypes = GetResponseTypes(method!);

        responseTypes[StatusCodes.Status204NoContent].Type.ShouldBe(typeof(void));
        responseTypes[StatusCodes.Status400BadRequest].Type.ShouldBe(typeof(ValidationProblemDetails));
        responseTypes[StatusCodes.Status401Unauthorized].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status403Forbidden].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status404NotFound].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status409Conflict].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status500InternalServerError].Type.ShouldBe(typeof(ProblemDetails));
    }

    [Fact]
    public void DeactivateAsync_DeveDocumentarRespostasEsperadasNoSwagger()
    {
        var method = typeof(GamesController).GetMethod(nameof(GamesController.DeactivateAsync));

        var responseTypes = GetResponseTypes(method!);

        responseTypes[StatusCodes.Status204NoContent].Type.ShouldBe(typeof(void));
        responseTypes[StatusCodes.Status401Unauthorized].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status403Forbidden].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status404NotFound].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status500InternalServerError].Type.ShouldBe(typeof(ProblemDetails));
    }

    private static GamesController CreateController(
        IGameRepository gameRepository,
        Guid userId,
        string role = nameof(UserRole.Administrator))
    {
        var createUseCase = new CreateGameUseCase(gameRepository);
        var deactivateUseCase = new DeactivateGameUseCase(gameRepository);
        var getUseCase = new GetGameUseCase(gameRepository, Substitute.For<IGameDetailsRepository>(), NullLogger<GetGameUseCase>.Instance);
        var listUseCase = new ListGamesUseCase(gameRepository);
        var updateUseCase = new UpdateGameUseCase(gameRepository);

        return new GamesController(createUseCase, deactivateUseCase, getUseCase, listUseCase, updateUseCase)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = CreateHttpContext(userId, role)
            }
        };
    }

    private static Dictionary<int, ProducesResponseTypeAttribute> GetResponseTypes(System.Reflection.MethodInfo method)
    {
        return method
            .GetCustomAttributes(typeof(ProducesResponseTypeAttribute), inherit: false)
            .Cast<ProducesResponseTypeAttribute>()
            .ToDictionary(attribute => attribute.StatusCode);
    }

    private static DefaultHttpContext CreateHttpContext(Guid userId, string role)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtClaimNames.Role, role)
        };

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
        };
    }
}
