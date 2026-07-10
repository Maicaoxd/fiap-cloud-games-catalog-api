using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Games.Get;
using CatalogAPI.Domain.Games;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Application.Games;

[Trait("Category", "Unit")]
public sealed class GetGameUseCaseTests
{
    [Fact]
    public async Task Deve_Retornar_Jogo_Quando_Jogo_Ativo_Existir()
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

        var useCase = new GetGameUseCase(gameRepository);

        var result = await useCase.ExecuteAsync(game.Id);

        result.GameId.ShouldBe(game.Id);
        result.Title.ShouldBe("Hades");
        result.Description.ShouldBe("Roguelike de acao.");
        result.Price.ShouldBe(49.90m);
        await gameRepository.Received(1).GetByIdAsync(game.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Jogo_Nao_Existir()
    {
        var gameId = Guid.NewGuid();
        var gameRepository = Substitute.For<IGameRepository>();

        gameRepository
            .GetByIdAsync(gameId, Arg.Any<CancellationToken>())
            .Returns((Game?)null);

        var useCase = new GetGameUseCase(gameRepository);

        var excecao = await Should.ThrowAsync<GameNotFoundException>(() => useCase.ExecuteAsync(gameId));

        excecao.Message.ShouldBe(ApplicationMessages.Game.NotFound);
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Jogo_Estiver_Inativo()
    {
        var changedBy = Guid.NewGuid();
        var game = Game.Create(
            "Hades",
            "Roguelike de acao.",
            49.90m,
            changedBy);
        game.Deactivate(changedBy);

        var gameRepository = Substitute.For<IGameRepository>();
        gameRepository
            .GetByIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(game);

        var useCase = new GetGameUseCase(gameRepository);

        var excecao = await Should.ThrowAsync<GameNotFoundException>(() => useCase.ExecuteAsync(game.Id));

        excecao.Message.ShouldBe(ApplicationMessages.Game.NotFound);
    }
}
