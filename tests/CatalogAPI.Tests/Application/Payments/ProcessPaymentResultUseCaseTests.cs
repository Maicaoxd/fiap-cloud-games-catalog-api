using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Payments.ProcessPaymentResult;
using CatalogAPI.Domain.Libraries;
using CatalogAPI.Domain.Orders;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Application.Payments;

[Trait("Category", "Unit")]
public sealed class ProcessPaymentResultUseCaseTests
{
    [Fact]
    public async Task Deve_Adicionar_Todos_Os_Jogos_Do_Pedido_Na_Biblioteca_Quando_Pagamento_For_Aprovado()
    {
        var userId = Guid.NewGuid();
        var firstGameId = Guid.NewGuid();
        var secondGameId = Guid.NewGuid();
        var order = CreateOrder(userId, [(firstGameId, 49.90m), (secondGameId, 24.90m)]);
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var addedLibraries = new List<Library>();

        orderRepository
            .GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        libraryRepository
            .ExistsByUserAndGameAsync(userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(false);

        libraryRepository
            .AddAsync(Arg.Any<Library>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                addedLibraries.Add(callInfo.ArgAt<Library>(0));
                return Task.CompletedTask;
            });

        var useCase = new ProcessPaymentResultUseCase(libraryRepository, orderRepository);
        var command = new ProcessPaymentResultCommand(
            order.Id,
            userId,
            order.TotalPrice,
            "Approved",
            DateTime.UtcNow);

        await useCase.ExecuteAsync(command);

        addedLibraries.Count.ShouldBe(2);
        addedLibraries.ShouldContain(library => library.UserId == userId && library.GameId == firstGameId);
        addedLibraries.ShouldContain(library => library.UserId == userId && library.GameId == secondGameId);
        order.Status.ShouldBe(PaymentStatus.Approved);
        order.ProcessedAt.ShouldNotBeNull();
        await orderRepository.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Nao_Deve_Duplicar_Biblioteca_Quando_Pagamento_Aprovado_E_Jogo_Ja_Existir()
    {
        var userId = Guid.NewGuid();
        var firstGameId = Guid.NewGuid();
        var secondGameId = Guid.NewGuid();
        var order = CreateOrder(userId, [(firstGameId, 49.90m), (secondGameId, 24.90m)]);
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var addedLibraries = new List<Library>();

        orderRepository
            .GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        libraryRepository
            .ExistsByUserAndGameAsync(userId, firstGameId, Arg.Any<CancellationToken>())
            .Returns(true);
        libraryRepository
            .ExistsByUserAndGameAsync(userId, secondGameId, Arg.Any<CancellationToken>())
            .Returns(false);

        libraryRepository
            .AddAsync(Arg.Any<Library>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                addedLibraries.Add(callInfo.ArgAt<Library>(0));
                return Task.CompletedTask;
            });

        var useCase = new ProcessPaymentResultUseCase(libraryRepository, orderRepository);
        var command = new ProcessPaymentResultCommand(
            order.Id,
            userId,
            order.TotalPrice,
            "Approved",
            DateTime.UtcNow);

        await useCase.ExecuteAsync(command);

        addedLibraries.Count.ShouldBe(1);
        addedLibraries.Single().GameId.ShouldBe(secondGameId);
        order.Status.ShouldBe(PaymentStatus.Approved);
        await orderRepository.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Nao_Deve_Adicionar_Jogos_Na_Biblioteca_Quando_Pagamento_For_Rejeitado()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, [(Guid.NewGuid(), 49.90m), (Guid.NewGuid(), 24.90m)]);
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();

        orderRepository
            .GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var useCase = new ProcessPaymentResultUseCase(libraryRepository, orderRepository);
        var command = new ProcessPaymentResultCommand(
            order.Id,
            userId,
            order.TotalPrice,
            "Rejected",
            DateTime.UtcNow);

        await useCase.ExecuteAsync(command);

        order.Status.ShouldBe(PaymentStatus.Rejected);
        await libraryRepository.DidNotReceive().AddAsync(Arg.Any<Library>(), Arg.Any<CancellationToken>());
        await orderRepository.Received(1).UpdateAsync(order, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Ignorar_Quando_Pedido_Nao_Existir()
    {
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var orderId = Guid.NewGuid();

        orderRepository
            .GetByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns((Order?)null);

        var useCase = new ProcessPaymentResultUseCase(libraryRepository, orderRepository);
        var command = new ProcessPaymentResultCommand(
            orderId,
            Guid.NewGuid(),
            49.90m,
            "Approved",
            DateTime.UtcNow);

        await useCase.ExecuteAsync(command);

        await libraryRepository.DidNotReceive().AddAsync(Arg.Any<Library>(), Arg.Any<CancellationToken>());
        await orderRepository.DidNotReceive().UpdateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Ignorar_Quando_Pedido_Ja_Tiver_Status_Final()
    {
        var userId = Guid.NewGuid();
        var order = CreateOrder(userId, [(Guid.NewGuid(), 49.90m)]);
        order.MarkPaymentProcessed(PaymentStatus.Approved);
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();

        orderRepository
            .GetByIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns(order);

        var useCase = new ProcessPaymentResultUseCase(libraryRepository, orderRepository);
        var command = new ProcessPaymentResultCommand(
            order.Id,
            userId,
            order.TotalPrice,
            "Approved",
            DateTime.UtcNow);

        await useCase.ExecuteAsync(command);

        await libraryRepository.DidNotReceive().AddAsync(Arg.Any<Library>(), Arg.Any<CancellationToken>());
        await orderRepository.DidNotReceive().UpdateAsync(Arg.Any<Order>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Status_For_Invalido()
    {
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var orderRepository = Substitute.For<IOrderRepository>();
        var useCase = new ProcessPaymentResultUseCase(libraryRepository, orderRepository);
        var command = new ProcessPaymentResultCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            49.90m,
            "Pending",
            DateTime.UtcNow);

        await Should.ThrowAsync<InvalidPaymentStatusException>(() => useCase.ExecuteAsync(command));
    }

    private static Order CreateOrder(Guid userId, IReadOnlyCollection<(Guid GameId, decimal Price)> games)
    {
        var items = games
            .Select(game => OrderItem.Create(game.GameId, game.Price, userId))
            .ToList();

        return Order.Place(userId, items);
    }
}
