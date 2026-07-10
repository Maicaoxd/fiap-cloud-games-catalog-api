using CatalogAPI.Application.Abstractions.Messaging;
using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Purchases.PurchaseGame;
using CatalogAPI.Domain.Games;
using CatalogAPI.Domain.Orders;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Application.Purchases;

[Trait("Category", "Unit")]
public sealed class PurchaseGameUseCaseTests
{
    [Fact]
    public async Task Deve_Criar_Pedido_Com_Lista_De_Jogos_E_Publicar_Evento_Quando_Jogos_Forem_Validos()
    {
        var userId = Guid.NewGuid();
        var games = new[]
        {
            CreateGame("Hades", 49.90m, isActive: true),
            CreateGame("Stardew Valley", 24.90m, isActive: true)
        };
        var gameRepository = Substitute.For<IGameRepository>();
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var publisher = Substitute.For<IOrderPlacedEventPublisher>();
        Order? addedOrder = null;
        var gameIds = games.Select(game => game.Id).ToArray();

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

        var useCase = CreateUseCase(gameRepository, libraryRepository, orderRepository, publisher);

        var result = await useCase.ExecuteAsync(new PurchaseGameCommand(userId, gameIds));

        result.OrderId.ShouldNotBe(Guid.Empty);
        addedOrder.ShouldNotBeNull();
        addedOrder!.Id.ShouldBe(result.OrderId);
        addedOrder.UserId.ShouldBe(userId);
        addedOrder.TotalPrice.ShouldBe(74.80m);
        addedOrder.Status.ShouldBe(PaymentStatus.Pending);
        addedOrder.Items.Count.ShouldBe(2);
        addedOrder.Items.ShouldContain(item => item.GameId == games[0].Id && item.Price == games[0].Price);
        addedOrder.Items.ShouldContain(item => item.GameId == games[1].Id && item.Price == games[1].Price);
        await orderRepository.Received(1).AddAsync(addedOrder, Arg.Any<CancellationToken>());
        await publisher.Received(1).PublishAsync(addedOrder, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Ignorar_Ids_Duplicados_Ao_Criar_Pedido()
    {
        var userId = Guid.NewGuid();
        var game = CreateGame("Hades", 49.90m, isActive: true);
        var gameRepository = Substitute.For<IGameRepository>();
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var publisher = Substitute.For<IOrderPlacedEventPublisher>();
        Order? addedOrder = null;

        gameRepository
            .ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { game });

        libraryRepository
            .ExistsByUserAndGameAsync(userId, game.Id, Arg.Any<CancellationToken>())
            .Returns(false);

        orderRepository
            .AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                addedOrder = callInfo.ArgAt<Order>(0);
                return Task.CompletedTask;
            });

        var useCase = CreateUseCase(gameRepository, libraryRepository, orderRepository, publisher);

        await useCase.ExecuteAsync(new PurchaseGameCommand(userId, [game.Id, game.Id]));

        addedOrder.ShouldNotBeNull();
        addedOrder!.Items.Count.ShouldBe(1);
        await gameRepository.Received(1)
            .ListByIdsAsync(Arg.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 1 && ids.Contains(game.Id)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Usuario_For_Invalido()
    {
        var gameRepository = Substitute.For<IGameRepository>();
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var publisher = Substitute.For<IOrderPlacedEventPublisher>();
        var useCase = CreateUseCase(gameRepository, libraryRepository, orderRepository, publisher);

        var exception = await Should.ThrowAsync<InvalidCredentialsException>(() =>
            useCase.ExecuteAsync(new PurchaseGameCommand(Guid.Empty, [Guid.NewGuid()])));

        exception.Message.ShouldBe(ApplicationMessages.Authentication.InvalidCredentials);
        await gameRepository.DidNotReceive().ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>());
        await orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Nenhum_Jogo_For_Informado()
    {
        var userId = Guid.NewGuid();
        var gameRepository = Substitute.For<IGameRepository>();
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var publisher = Substitute.For<IOrderPlacedEventPublisher>();
        var useCase = CreateUseCase(gameRepository, libraryRepository, orderRepository, publisher);

        var exception = await Should.ThrowAsync<GameNotFoundException>(() =>
            useCase.ExecuteAsync(new PurchaseGameCommand(userId, [])));

        exception.Message.ShouldBe(ApplicationMessages.Game.NotFound);
        await orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Algum_Jogo_Nao_Existir()
    {
        var userId = Guid.NewGuid();
        var existingGame = CreateGame("Hades", 49.90m, isActive: true);
        var missingGameId = Guid.NewGuid();
        var gameRepository = Substitute.For<IGameRepository>();
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var publisher = Substitute.For<IOrderPlacedEventPublisher>();

        gameRepository
            .ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { existingGame });

        var useCase = CreateUseCase(gameRepository, libraryRepository, orderRepository, publisher);

        var exception = await Should.ThrowAsync<GameNotFoundException>(() =>
            useCase.ExecuteAsync(new PurchaseGameCommand(userId, [existingGame.Id, missingGameId])));

        exception.Message.ShouldBe(ApplicationMessages.Game.NotFound);
        await libraryRepository.DidNotReceive()
            .ExistsByUserAndGameAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Algum_Jogo_Estiver_Inativo()
    {
        var userId = Guid.NewGuid();
        var games = new[]
        {
            CreateGame("Hades", 49.90m, isActive: true),
            CreateGame("Inactive Game", 10m, isActive: false)
        };
        var gameRepository = Substitute.For<IGameRepository>();
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var publisher = Substitute.For<IOrderPlacedEventPublisher>();

        gameRepository
            .ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(games);

        var useCase = CreateUseCase(gameRepository, libraryRepository, orderRepository, publisher);

        var exception = await Should.ThrowAsync<GameUnavailableException>(() =>
            useCase.ExecuteAsync(new PurchaseGameCommand(userId, games.Select(game => game.Id).ToArray())));

        exception.Message.ShouldBe(ApplicationMessages.Game.InactiveCannotBePurchased);
        await libraryRepository.DidNotReceive()
            .ExistsByUserAndGameAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Usuario_Ja_Possuir_Algum_Jogo()
    {
        var userId = Guid.NewGuid();
        var games = new[]
        {
            CreateGame("Hades", 49.90m, isActive: true),
            CreateGame("Stardew Valley", 24.90m, isActive: true)
        };
        var gameRepository = Substitute.For<IGameRepository>();
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var publisher = Substitute.For<IOrderPlacedEventPublisher>();

        gameRepository
            .ListByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(games);

        libraryRepository
            .ExistsByUserAndGameAsync(userId, games[0].Id, Arg.Any<CancellationToken>())
            .Returns(false);
        libraryRepository
            .ExistsByUserAndGameAsync(userId, games[1].Id, Arg.Any<CancellationToken>())
            .Returns(true);

        var useCase = CreateUseCase(gameRepository, libraryRepository, orderRepository, publisher);

        var exception = await Should.ThrowAsync<GameAlreadyOwnedException>(() =>
            useCase.ExecuteAsync(new PurchaseGameCommand(userId, games.Select(game => game.Id).ToArray())));

        exception.Message.ShouldBe(ApplicationMessages.Library.GameAlreadyOwned);
        await orderRepository.DidNotReceive().AddAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
        await publisher.DidNotReceive().PublishAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    private static PurchaseGameUseCase CreateUseCase(
        IGameRepository gameRepository,
        ILibraryRepository libraryRepository,
        IOrderRepository orderRepository,
        IOrderPlacedEventPublisher publisher)
    {
        return new PurchaseGameUseCase(
            gameRepository,
            libraryRepository,
            orderRepository,
            publisher);
    }

    private static Game CreateGame(string title, decimal price, bool isActive)
    {
        var game = Game.Create(
            title,
            "Descricao do jogo.",
            price,
            Guid.NewGuid());

        if (!isActive)
            game.Deactivate(Guid.NewGuid());

        return game;
    }
}
