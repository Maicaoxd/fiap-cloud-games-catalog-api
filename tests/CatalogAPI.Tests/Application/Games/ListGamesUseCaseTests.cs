using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Games.List;
using CatalogAPI.Domain.Games;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Application.Games;

[Trait("Category", "Unit")]
public sealed class ListGamesUseCaseTests
{
    [Fact]
    public async Task Deve_Listar_Jogos_Ativos()
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

        var useCase = new ListGamesUseCase(gameRepository);

        var result = await useCase.ExecuteAsync();

        result.Count.ShouldBe(2);
        result.ShouldContain(game => game.GameId == firstGame.Id);
        result.ShouldContain(game => game.GameId == secondGame.Id);
        result.ShouldContain(game =>
            game.Title == "Hades" &&
            game.Description == "Roguelike de acao." &&
            game.Price == 49.90m);
        await gameRepository.Received(1).ListActiveAsync(Arg.Any<CancellationToken>());
    }
}
