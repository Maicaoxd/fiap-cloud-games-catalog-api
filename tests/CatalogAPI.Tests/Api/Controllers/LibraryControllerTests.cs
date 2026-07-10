using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CatalogAPI.Api.Contracts.Libraries.List;
using CatalogAPI.Api.Contracts.Purchases;
using CatalogAPI.Api.Controllers;
using CatalogAPI.Application.Abstractions.Messaging;
using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Libraries.List;
using CatalogAPI.Application.Purchases.PurchaseGame;
using CatalogAPI.Domain.Games;
using CatalogAPI.Domain.Orders;
using CatalogAPI.Domain.Users;
using CatalogAPI.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Api.Controllers;

[Trait("Category", "Unit")]
public sealed class LibraryControllerTests
{
    [Fact]
    public async Task ListAsync_QuandoUsuarioAutenticado_DeveRetornarOkComJogosDaBiblioteca()
    {
        var userId = Guid.NewGuid();
        var acquiredAt = DateTime.UtcNow;
        var libraryRepository = Substitute.For<ILibraryRepository>();

        libraryRepository
            .ListGamesByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new LibraryGameReadModel(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Hades",
                    "Roguelike de acao.",
                    49.90m,
                    true,
                    acquiredAt),
                new LibraryGameReadModel(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    "Half-Life",
                    "FPS classico.",
                    29.90m,
                    false,
                    acquiredAt.AddMinutes(-10))
            });

        var controller = CreateController(
            userId,
            libraryRepository: libraryRepository);

        var actionResult = await controller.ListAsync(CancellationToken.None);

        var okResult = actionResult.Result.ShouldBeOfType<OkObjectResult>();
        var response = okResult.Value.ShouldBeOfType<List<ListLibraryGameResponse>>();

        response.Count.ShouldBe(2);
        response[0].Title.ShouldBe("Hades");
        response[0].IsActive.ShouldBeTrue();
        response[1].Title.ShouldBe("Half-Life");
        response[1].IsActive.ShouldBeFalse();
        await libraryRepository.Received(1).ListGamesByUserIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PurchaseAsync_QuandoUsuarioAutenticadoEJogosValidos_DeveRetornarAcceptedComOrderId()
    {
        var userId = Guid.NewGuid();
        var games = new[]
        {
            Game.Create("Hades", "Roguelike de acao.", 49.90m, Guid.NewGuid()),
            Game.Create("Stardew Valley", "Simulador de fazenda.", 24.90m, Guid.NewGuid())
        };
        var gameRepository = Substitute.For<IGameRepository>();
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var publisher = Substitute.For<IOrderPlacedEventPublisher>();
        Order? addedOrder = null;

        gameRepository
            .ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(games);

        libraryRepository
            .ExistsByUserAndGameAsync(userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        orderRepository
            .AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                addedOrder = callInfo.ArgAt<Order>(0);
                return Task.CompletedTask;
            });

        var controller = CreateController(
            userId,
            gameRepository,
            libraryRepository,
            orderRepository,
            publisher);
        var request = new PurchaseGamesRequest(games.Select(game => game.Id).ToArray());

        var actionResult = await controller.PurchaseAsync(request, CancellationToken.None);

        var acceptedResult = actionResult.Result.ShouldBeOfType<AcceptedResult>();
        var response = acceptedResult.Value.ShouldBeOfType<PurchaseGameResponse>();

        response.OrderId.ShouldNotBe(Guid.Empty);
        acceptedResult.Location.ShouldBe($"/api/orders/{response.OrderId}");
        addedOrder.ShouldNotBeNull();
        addedOrder!.UserId.ShouldBe(userId);
        addedOrder.Items.Count.ShouldBe(2);
        addedOrder.TotalPrice.ShouldBe(74.80m);
        await publisher.Received(1).PublishAsync(addedOrder, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ListAsync_DeveExigirUsuarioAutenticado()
    {
        var method = typeof(LibraryController).GetMethod(nameof(LibraryController.ListAsync));

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        authorizeAttribute.Roles.ShouldBeNull();
    }

    [Fact]
    public void PurchaseAsync_DeveExigirUsuarioAutenticado()
    {
        var method = typeof(LibraryController).GetMethod(nameof(LibraryController.PurchaseAsync));

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false)
            .Cast<AuthorizeAttribute>()
            .Single();

        authorizeAttribute.Roles.ShouldBeNull();
    }

    [Fact]
    public void ListAsync_DeveDocumentarRespostasEsperadasNoSwagger()
    {
        var method = typeof(LibraryController).GetMethod(nameof(LibraryController.ListAsync));

        var responseTypes = GetResponseTypes(method!);

        responseTypes[StatusCodes.Status200OK].Type.ShouldBe(typeof(IReadOnlyCollection<ListLibraryGameResponse>));
        responseTypes[StatusCodes.Status401Unauthorized].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status403Forbidden].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status500InternalServerError].Type.ShouldBe(typeof(ProblemDetails));
    }

    [Fact]
    public void PurchaseAsync_DeveDocumentarRespostasEsperadasNoSwagger()
    {
        var method = typeof(LibraryController).GetMethod(nameof(LibraryController.PurchaseAsync));

        var responseTypes = GetResponseTypes(method!);

        responseTypes[StatusCodes.Status202Accepted].Type.ShouldBe(typeof(PurchaseGameResponse));
        responseTypes[StatusCodes.Status400BadRequest].Type.ShouldBe(typeof(ValidationProblemDetails));
        responseTypes[StatusCodes.Status401Unauthorized].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status403Forbidden].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status404NotFound].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status409Conflict].Type.ShouldBe(typeof(ProblemDetails));
        responseTypes[StatusCodes.Status500InternalServerError].Type.ShouldBe(typeof(ProblemDetails));
    }

    private static LibraryController CreateController(
        Guid userId,
        IGameRepository? gameRepository = null,
        ILibraryRepository? libraryRepository = null,
        IOrderRepository? orderRepository = null,
        IOrderPlacedEventPublisher? publisher = null,
        string role = nameof(UserRole.User))
    {
        gameRepository ??= Substitute.For<IGameRepository>();
        libraryRepository ??= Substitute.For<ILibraryRepository>();
        orderRepository ??= Substitute.For<IOrderRepository>();
        publisher ??= Substitute.For<IOrderPlacedEventPublisher>();

        var listUseCase = new ListLibraryGamesUseCase(libraryRepository);
        var purchaseUseCase = new PurchaseGameUseCase(
            gameRepository,
            libraryRepository,
            orderRepository,
            publisher);

        return new LibraryController(listUseCase, purchaseUseCase)
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
