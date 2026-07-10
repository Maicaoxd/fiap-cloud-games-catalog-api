using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Libraries.List;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Application.Libraries;

[Trait("Category", "Unit")]
public sealed class ListLibraryGamesUseCaseTests
{
    [Fact]
    public async Task Deve_Listar_Jogos_Da_Biblioteca_Quando_Usuario_For_Valido()
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
                    acquiredAt.AddMinutes(-5))
            });

        var useCase = new ListLibraryGamesUseCase(libraryRepository);

        var result = await useCase.ExecuteAsync(userId);

        var libraryGames = result.ToList();
        libraryGames.Count.ShouldBe(2);
        libraryGames[0].Title.ShouldBe("Hades");
        libraryGames[0].IsActive.ShouldBeTrue();
        libraryGames[0].AcquiredAt.ShouldBe(acquiredAt);
        libraryGames[1].Title.ShouldBe("Half-Life");
        libraryGames[1].IsActive.ShouldBeFalse();
        await libraryRepository.Received(1).ListGamesByUserIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deve_Lancar_Excecao_Quando_Usuario_For_Invalido()
    {
        var libraryRepository = Substitute.For<ILibraryRepository>();
        var useCase = new ListLibraryGamesUseCase(libraryRepository);

        var exception = await Should.ThrowAsync<InvalidCredentialsException>(() => useCase.ExecuteAsync(Guid.Empty));

        exception.Message.ShouldBe(ApplicationMessages.Authentication.InvalidCredentials);
        await libraryRepository.DidNotReceive().ListGamesByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
