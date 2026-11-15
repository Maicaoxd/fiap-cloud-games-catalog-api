using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CatalogAPI.Api.Controllers;
using CatalogAPI.Application.Games.Search;
using CatalogAPI.Domain.Games;
using CatalogAPI.Infrastructure.Persistence;
using CatalogAPI.Infrastructure.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Infrastructure.Search;

public sealed class GameSearchTests
{
    [Theory]
    [InlineData(null, 1, 20)]
    [InlineData("", 1, 20)]
    [InlineData("   ", 1, 20)]
    [InlineData("jogo", 0, 20)]
    [InlineData("jogo", 1, 51)]
    [InlineData("jogo", 1, 0)]
    [InlineData("jogo", 501, 20)]
    [InlineData("jogo", int.MaxValue, 50)]
    public async Task Busca_invalida_nao_consulta_indice(string? query, int page, int pageSize)
    {
        var index = Substitute.For<IGameSearchIndex>();
        await Should.ThrowAsync<ArgumentException>(() => new SearchGamesUseCase(index).ExecuteAsync(query, page, pageSize));
        await index.DidNotReceiveWithAnyArgs().SearchAsync(default!, default, default, default);
    }

    [Fact]
    public async Task Normaliza_busca_e_preserva_paginacao_e_cancelamento()
    {
        var index = Substitute.For<IGameSearchIndex>();
        var expected = new GameSearchResult(0, 2, 10, []);
        using var cancellation = new CancellationTokenSource();
        index.SearchAsync("hades", 2, 10, cancellation.Token).Returns(expected);
        (await new SearchGamesUseCase(index).ExecuteAsync("  hades  ", 2, 10, cancellation.Token)).ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task Consulta_envia_fuzzy_filtro_de_ativos_pesos_e_relevancia_e_le_resposta()
    {
        var id = Guid.NewGuid();
        var handler = new Handler(async request =>
        {
            if (request.Method == HttpMethod.Head) return Response(HttpStatusCode.OK);
            request.RequestUri!.AbsolutePath.ShouldBe("/fcg-games-v1/_search");
            request.Headers.Authorization.ShouldBe(new AuthenticationHeaderValue("ApiKey", "chave-de-teste"));
            using var query = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            var root = query.RootElement;
            root.GetProperty("from").GetInt32().ShouldBe(10);
            root.GetProperty("size").GetInt32().ShouldBe(10);
            var match = root.GetProperty("query").GetProperty("bool").GetProperty("must")[0].GetProperty("multi_match");
            match.GetProperty("fuzziness").GetString().ShouldBe("AUTO");
            match.GetProperty("fields")[0].GetString().ShouldBe("title^3");
            root.GetProperty("query").GetProperty("bool").GetProperty("filter")[0].GetProperty("term").GetProperty("isActive").GetBoolean().ShouldBeTrue();
            root.GetProperty("sort")[0].GetProperty("_score").GetString().ShouldBe("desc");
            return Response(HttpStatusCode.OK, JsonSerializer.Serialize(new
            {
                hits = new
                {
                    total = new { value = 12 },
                    hits = new[] { new { _score = 4.2, _source = new { gameId = id, title = "Hades", description = "Roguelike", price = 20.5, isActive = true, updatedAt = DateTime.UtcNow } } }
                }
            }));
        });
        var result = await Create(handler, apiKey: "chave-de-teste").SearchAsync("hads", 2, 10, default);
        result.Total.ShouldBe(12);
        result.Items.Single().GameId.ShouldBe(id);
        result.Items.Single().Score.ShouldBe(4.2);
    }

    [Fact]
    public async Task Busca_desabilitada_retorna_indisponibilidade_sem_rede()
    {
        var handler = new Handler(_ => throw new Exception("Não deveria executar HTTP."));
        await Should.ThrowAsync<GameSearchUnavailableException>(() => Create(handler, enabled: false).SearchAsync("hades", 1, 20, default));
    }

    [Fact]
    public async Task Falha_de_transporte_e_convertida_em_indisponibilidade()
    {
        var handler = new Handler(_ => throw new HttpRequestException("falha de conexão"));
        await Should.ThrowAsync<GameSearchUnavailableException>(() => Create(handler).SearchAsync("hades", 1, 20, default));
    }

    [Fact]
    public async Task Cancelamento_do_cliente_nao_e_mascarado()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var handler = new Handler(_ => Task.FromCanceled<HttpResponseMessage>(cancellation.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => Create(handler).SearchAsync("hades", 1, 20, cancellation.Token));
    }

    [Fact]
    public async Task Versao_antiga_nao_sobrescreve_jogo_e_desativacao_permanece_documento()
    {
        var game = Game.Create("Hades", "Roguelike", 30, Guid.NewGuid());
        game.Deactivate(Guid.NewGuid());
        var handler = new Handler(async request =>
        {
            if (request.Method == HttpMethod.Head) return Response(HttpStatusCode.OK);
            request.RequestUri!.Query.ShouldContain("version=42");
            request.RequestUri.Query.ShouldContain("version_type=external");
            request.RequestUri.AbsolutePath.ShouldEndWith(game.Id.ToString("D"));
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            json.RootElement.GetProperty("isActive").GetBoolean().ShouldBeFalse();
            return Response(HttpStatusCode.Conflict, """{"error":{"type":"version_conflict_engine_exception"}}""");
        });
        await Create(handler).IndexAsync(GameSearchDocument.From(game), 42, default);
    }

    [Fact]
    public async Task Outro_conflito_nao_e_tratado_como_versao_antiga()
    {
        var handler = new Handler(request => Task.FromResult(request.Method == HttpMethod.Head
            ? Response(HttpStatusCode.OK)
            : Response(HttpStatusCode.Conflict, """{"error":{"type":"outro_erro"}}""")));
        var game = Game.Create("Hades", "Roguelike", 30, Guid.NewGuid());
        await Should.ThrowAsync<GameSearchUnavailableException>(() => Create(handler).IndexAsync(GameSearchDocument.From(game), 42, default));
    }

    [Fact]
    public async Task Criacao_concorrente_do_indice_e_idempotente()
    {
        var game = Game.Create("Hades", "Roguelike", 30, Guid.NewGuid());
        var calls = 0;
        var handler = new Handler(async request =>
        {
            calls++;
            if (request.Method == HttpMethod.Head) return Response(HttpStatusCode.NotFound);
            if (request.RequestUri!.AbsolutePath == "/fcg-games-v1")
            {
                using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
                json.RootElement.GetProperty("mappings").GetProperty("properties").GetProperty("gameId").GetProperty("type").GetString().ShouldBe("keyword");
                return Response(HttpStatusCode.BadRequest, """{"error":{"type":"resource_already_exists_exception"}}""");
            }
            return Response(HttpStatusCode.Created);
        });
        await Create(handler).IndexAsync(GameSearchDocument.From(game), 1, default);
        calls.ShouldBe(3);
    }

    [Fact]
    public void Outbox_mantem_snapshot_da_alteracao_e_modelo_relacional()
    {
        var game = Game.Create("Hades", "Roguelike", 30, Guid.NewGuid());
        var message = GameSearchOutboxMessage.From(game);
        game.Update("Outro título", "Outra descrição", 50, Guid.NewGuid());
        JsonSerializer.Deserialize<GameSearchDocument>(message.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))!.Title.ShouldBe("Hades");
        using var db = new CatalogDbContext(new DbContextOptionsBuilder<CatalogDbContext>().UseSqlServer("Server=localhost;Database=Modelo;Integrated Security=True").Options);
        var entity = db.Model.FindEntityType(typeof(GameSearchOutboxMessage))!;
        entity.FindProperty(nameof(GameSearchOutboxMessage.Id))!.ValueGenerated.ShouldBe(ValueGenerated.OnAdd);
        entity.GetForeignKeys().Single().DeleteBehavior.ShouldBe(DeleteBehavior.Restrict);
    }

    [Fact]
    public void Rota_exige_autenticacao_e_opcoes_bloqueiam_endpoints_inseguros()
    {
        typeof(SearchController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Length.ShouldBe(1);
        new ElasticsearchOptions { Enabled = true, Endpoint = "http://localhost:9200" }.IsValid(development: true).ShouldBeTrue();
        new ElasticsearchOptions { Enabled = true, Endpoint = "http://localhost:9200" }.IsValid(development: false).ShouldBeFalse();
        new ElasticsearchOptions { Enabled = true, Endpoint = "https://usuario:senha@example.invalid" }.IsValid(development: false).ShouldBeFalse();
        new ElasticsearchOptions { Enabled = true, Endpoint = "https://example.invalid", IndexName = "../_all" }.IsValid(false).ShouldBeFalse();
    }

    private static ElasticsearchGameIndex Create(HttpMessageHandler handler, bool enabled = true, string apiKey = "") =>
        new(new HttpClient(handler), Options.Create(new ElasticsearchOptions
        { Enabled = enabled, Endpoint = "https://example.invalid", ApiKey = apiKey }));

    private static HttpResponseMessage Response(HttpStatusCode status, string content = "{}") =>
        new(status) { Content = new StringContent(content, Encoding.UTF8, "application/json") };

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request);
    }
}
