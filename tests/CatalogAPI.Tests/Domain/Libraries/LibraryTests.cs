using CatalogAPI.Domain.Libraries;
using CatalogAPI.Domain.Shared;
using Shouldly;

namespace CatalogAPI.Tests.Domain.Libraries;

[Trait("Category", "Unit")]
public sealed class LibraryTests
{
    [Fact]
    public void Deve_Criar_Item_De_Biblioteca_Quando_Dados_Forem_Validos()
    {
        var userId = Guid.NewGuid();
        var gameId = Guid.NewGuid();

        var library = Library.Create(userId, gameId);

        library.Id.ShouldNotBe(Guid.Empty);
        library.UserId.ShouldBe(userId);
        library.GameId.ShouldBe(gameId);
        library.IsActive.ShouldBeTrue();
        library.CreatedBy.ShouldBe(userId);
        library.CreatedAt.ShouldNotBe(default);
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_Usuario_Do_Item_De_Biblioteca_For_Invalido()
    {
        var action = () => Library.Create(Guid.Empty, Guid.NewGuid());

        var exception = Should.Throw<ArgumentException>(action);

        exception.Message.ShouldBe(DomainMessages.Library.UserIdRequired);
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_Jogo_Do_Item_De_Biblioteca_For_Invalido()
    {
        var action = () => Library.Create(Guid.NewGuid(), Guid.Empty);

        var exception = Should.Throw<ArgumentException>(action);

        exception.Message.ShouldBe(DomainMessages.Library.GameIdRequired);
    }
}
