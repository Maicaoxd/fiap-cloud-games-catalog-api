using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CatalogAPI.Application.Games.Search;
using Microsoft.Extensions.Options;

namespace CatalogAPI.Infrastructure.Search;

// A API REST do Elasticsearch permite usar a mesma integração local e no Elastic Cloud.
public sealed class ElasticsearchGameIndex(HttpClient client, IOptions<ElasticsearchOptions> configured) : IGameSearchIndex
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private ElasticsearchOptions Options => configured.Value;

    public async Task<GameSearchResult> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await EnsureIndexAsync(cancellationToken);
            var body = new
            {
                from = (page - 1) * pageSize,
                size = pageSize,
                track_total_hits = true,
                query = new
                {
                    @bool = new
                    {
                        filter = new[] { new { term = new { isActive = true } } },
                        must = new[] { new { multi_match = new { query, fields = new[] { "title^3", "description" }, type = "best_fields", fuzziness = "AUTO", max_expansions = 30 } } }
                    }
                },
                sort = new object[] { new { _score = "desc" }, new { gameId = "asc" } }
            };
            using var response = await SendAsync(HttpMethod.Post, $"{Options.IndexName}/_search", body, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
            var hits = json.RootElement.GetProperty("hits");
            var items = hits.GetProperty("hits").EnumerateArray().Select(hit =>
            {
                var game = hit.GetProperty("_source").Deserialize<GameSearchDocument>(Json)!;
                return new GameSearchHit(game.GameId, game.Title, game.Description, game.Price, hit.GetProperty("_score").GetDouble());
            }).ToList();
            return new GameSearchResult(hits.GetProperty("total").GetProperty("value").GetInt64(), page, pageSize, items);
        }, cancellationToken);
    }

    public async Task IndexAsync(GameSearchDocument game, long version, CancellationToken cancellationToken)
    {
        if (game.GameId == Guid.Empty || version <= 0) throw new ArgumentException("Documento ou versão de indexação inválidos.");
        await ExecuteAsync(async () =>
        {
            await EnsureIndexAsync(cancellationToken);
            using var response = await SendAsync(HttpMethod.Put,
                $"{Options.IndexName}/_doc/{game.GameId:D}?version={version}&version_type=external&refresh=wait_for", game, cancellationToken);
            // Mensagens antigas e reentregas não podem sobrescrever versões mais recentes.
            if (response.StatusCode == HttpStatusCode.Conflict &&
                await ErrorIsAsync(response, "version_conflict_engine_exception", cancellationToken)) return true;
            response.EnsureSuccessStatusCode();
            return true;
        }, cancellationToken);
    }

    private async Task EnsureIndexAsync(CancellationToken cancellationToken)
    {
        using var exists = await SendAsync(HttpMethod.Head, Options.IndexName, null, cancellationToken);
        if (exists.IsSuccessStatusCode) return;
        if (exists.StatusCode != HttpStatusCode.NotFound) { exists.EnsureSuccessStatusCode(); }
        var definition = new
        {
            settings = new { number_of_shards = 1, number_of_replicas = 0 },
            mappings = new
            {
                dynamic = "strict",
                properties = new
                {
                    gameId = new { type = "keyword" },
                    title = new { type = "text", analyzer = "standard" },
                    description = new { type = "text", analyzer = "standard" },
                    price = new { type = "scaled_float", scaling_factor = 100 },
                    isActive = new { type = "boolean" },
                    updatedAt = new { type = "date" }
                }
            }
        };
        using var created = await SendAsync(HttpMethod.Put, Options.IndexName, definition, cancellationToken);
        if (created.StatusCode == HttpStatusCode.BadRequest &&
            await ErrorIsAsync(created, "resource_already_exists_exception", cancellationToken)) return;
        created.EnsureSuccessStatusCode();
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body, CancellationToken cancellationToken)
    {
        var endpoint = new Uri(new Uri(Options.Endpoint.TrimEnd('/') + "/"), path);
        using var request = new HttpRequestMessage(method, endpoint);
        if (!string.IsNullOrWhiteSpace(Options.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", Options.ApiKey);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        return await client.SendAsync(request, cancellationToken);
    }

    private static async Task<bool> ErrorIsAsync(HttpResponseMessage response, string type, CancellationToken cancellationToken)
    {
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        return json.RootElement.TryGetProperty("error", out var error) &&
            error.ValueKind == JsonValueKind.Object &&
            error.TryGetProperty("type", out var errorType) && errorType.GetString() == type;
    }

    private async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        if (!Options.Enabled) throw new GameSearchUnavailableException();
        try { return await operation(); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or JsonException or KeyNotFoundException)
        { throw new GameSearchUnavailableException(); }
    }
}
