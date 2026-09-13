using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Games.Deactivate;
using CatalogAPI.Domain.Games;
using NSubstitute;
using CatalogAPI.Application.Abstractions.Caching;
using Shouldly;

namespace CatalogAPI.Tests.Application.Games;

[Trait("Category", "Unit")]
public sealed class DeactivateGameUseCaseTests
{
    [Fact]
    public async Task Deve_Desativar_Jogo_Quando_Ele_Estiver_Ativo()
    {
        var deactivatedBy = Guid.NewGuid();
        var game = Game.Create(
            "Stardew Valley",
            "Simulador de fazenda.",
            24.90m,
            Guid.NewGuid());
        var gameRepository = Substitute.For<IGameRepository>();

        gameRepository
            .GetByIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(game);

        var useCase = new DeactivateGameUseCase(gameRepository, Substitute.For<IGameCache>());
        var command = new DeactivateGameCommand(game.Id, deactivatedBy);

        await useCase.ExecuteAsync(command);

        game.IsActive.ShouldBeFalse();
        game.UpdatedBy.ShouldBe(deactivatedBy);
        game.UpdatedAt.ShouldNotBeNull();
        await gameRepository.Received(1).UpdateAsync(game, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Jogo_Nao_Existir()
    {
        var gameId = Guid.NewGuid();
        var gameRepository = Substitute.For<IGameRepository>();

        gameRepository
            .GetByIdAsync(gameId, Arg.Any<CancellationToken>())
            .Returns((Game?)null);

        var useCase = new DeactivateGameUseCase(gameRepository, Substitute.For<IGameCache>());
        var command = new DeactivateGameCommand(gameId, Guid.NewGuid());

        var exception = await Should.ThrowAsync<GameNotFoundException>(() => useCase.ExecuteAsync(command));

        exception.Message.ShouldBe(ApplicationMessages.Game.NotFound);
        await gameRepository.DidNotReceive().UpdateAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Nao_Persistir_Quando_Jogo_Ja_Estiver_Inativo()
    {
        var game = Game.Create(
            "Stardew Valley",
            "Simulador de fazenda.",
            24.90m,
            Guid.NewGuid());
        game.Deactivate(Guid.NewGuid());

        var updatedAt = game.UpdatedAt;
        var updatedBy = game.UpdatedBy;
        var gameRepository = Substitute.For<IGameRepository>();

        gameRepository
            .GetByIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(game);

        var useCase = new DeactivateGameUseCase(gameRepository, Substitute.For<IGameCache>());
        var command = new DeactivateGameCommand(game.Id, Guid.NewGuid());

        await useCase.ExecuteAsync(command);

        game.IsActive.ShouldBeFalse();
        game.UpdatedAt.ShouldBe(updatedAt);
        game.UpdatedBy.ShouldBe(updatedBy);
        await gameRepository.DidNotReceive().UpdateAsync(Arg.Any<Game>(), Arg.Any<CancellationToken>());
    }
}
