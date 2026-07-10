using CatalogAPI.Domain.Games;
using CatalogAPI.Domain.Shared;
using Shouldly;

namespace CatalogAPI.Tests.Domain.Games;

[Trait("Category", "Unit")]
public sealed class GameTests
{
    [Fact]
    public void Deve_Criar_Jogo_Quando_Dados_Forem_Validos()
    {
        var criadoPor = Guid.NewGuid();

        var jogo = Game.Create(
            "Stardew Valley",
            "Simulador de fazenda e vida no campo.",
            24.90m,
            criadoPor);

        jogo.Id.ShouldNotBe(Guid.Empty);
        jogo.Title.ShouldBe("Stardew Valley");
        jogo.Description.ShouldBe("Simulador de fazenda e vida no campo.");
        jogo.Price.ShouldBe(24.90m);
        jogo.IsActive.ShouldBeTrue();
        jogo.CreatedAt.ShouldNotBe(default);
        jogo.CreatedBy.ShouldBe(criadoPor);
        jogo.UpdatedAt.ShouldBeNull();
        jogo.UpdatedBy.ShouldBeNull();
    }

    [Fact]
    public void Deve_Criar_Jogo_Gratuito_Quando_Preco_For_Zero()
    {
        var jogo = Game.Create(
            "FIAP Quest",
            "Jogo gratuito para alunos.",
            0,
            Guid.NewGuid());

        jogo.Price.ShouldBe(0);
    }

    [Fact]
    public void Deve_Normalizar_Titulo_Para_Unicidade_Mantendo_Valor_Limpo_Para_Exibicao()
    {
        var jogo = Game.Create(
            "  Stardew Valley  ",
            "  Simulador de fazenda e vida no campo.  ",
            24.90m,
            Guid.NewGuid());

        jogo.Title.ShouldBe("Stardew Valley");
        jogo.Description.ShouldBe("Simulador de fazenda e vida no campo.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Deve_Lancar_Excecao_Quando_Titulo_For_Obrigatorio_E_Nao_For_Informado(string? titulo)
    {
        var acao = () => Game.Create(
            titulo!,
            "Descricao valida.",
            10,
            Guid.NewGuid());

        var excecao = Should.Throw<ArgumentException>(acao);

        excecao.Message.ShouldBe(DomainMessages.Game.TitleRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Deve_Lancar_Excecao_Quando_Descricao_For_Obrigatoria_E_Nao_For_Informada(string? descricao)
    {
        var acao = () => Game.Create(
            "Stardew Valley",
            descricao!,
            10,
            Guid.NewGuid());

        var excecao = Should.Throw<ArgumentException>(acao);

        excecao.Message.ShouldBe(DomainMessages.Game.DescriptionRequired);
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_Preco_For_Negativo()
    {
        var acao = () => Game.Create(
            "Stardew Valley",
            "Descricao valida.",
            -1,
            Guid.NewGuid());

        var excecao = Should.Throw<ArgumentException>(acao);

        excecao.Message.ShouldBe(DomainMessages.Game.PriceCannotBeNegative);
    }

    [Fact]
    public void Deve_Lancar_Excecao_Quando_Criar_Jogo_Sem_Responsavel_Valido()
    {
        var acao = () => Game.Create(
            "Stardew Valley",
            "Descricao valida.",
            10,
            Guid.Empty);

        var excecao = Should.Throw<ArgumentException>(acao);

        excecao.Message.ShouldBe(DomainMessages.Entity.ResponsibleForChangeRequired);
    }

    [Fact]
    public void Deve_Atualizar_Jogo_Quando_Dados_Forem_Validos()
    {
        var atualizadoPor = Guid.NewGuid();
        var jogo = Game.Create(
            "Stardew Valley",
            "Simulador de fazenda.",
            24.90m,
            Guid.NewGuid());

        jogo.Update(
            "Stardew Valley Deluxe",
            "Simulador de fazenda com conteudo extra.",
            39.90m,
            atualizadoPor);

        jogo.Title.ShouldBe("Stardew Valley Deluxe");
        jogo.Description.ShouldBe("Simulador de fazenda com conteudo extra.");
        jogo.Price.ShouldBe(39.90m);
        jogo.UpdatedAt.ShouldNotBeNull();
        jogo.UpdatedBy.ShouldBe(atualizadoPor);
    }

    [Fact]
    public void Deve_Desativar_Jogo_Quando_Jogo_Estiver_Ativo()
    {
        var desativadoPor = Guid.NewGuid();
        var jogo = Game.Create(
            "Stardew Valley",
            "Simulador de fazenda.",
            24.90m,
            Guid.NewGuid());

        jogo.Deactivate(desativadoPor);

        jogo.IsActive.ShouldBeFalse();
        jogo.UpdatedAt.ShouldNotBeNull();
        jogo.UpdatedBy.ShouldBe(desativadoPor);
    }

    [Fact]
    public void Deve_Reativar_Jogo_Quando_Jogo_Estiver_Inativo()
    {
        var desativadoPor = Guid.NewGuid();
        var ativadoPor = Guid.NewGuid();
        var jogo = Game.Create(
            "Stardew Valley",
            "Simulador de fazenda.",
            24.90m,
            Guid.NewGuid());

        jogo.Deactivate(desativadoPor);

        jogo.Activate(ativadoPor);

        jogo.IsActive.ShouldBeTrue();
        jogo.UpdatedAt.ShouldNotBeNull();
        jogo.UpdatedBy.ShouldBe(ativadoPor);
    }
}
