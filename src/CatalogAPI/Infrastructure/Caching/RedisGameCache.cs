using System.Text.Json;
using CatalogAPI.Application.Abstractions.Caching;
using CatalogAPI.Application.Games.Get;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace CatalogAPI.Infrastructure.Caching;

public sealed class RedisGameCache(IDistributedCache cache, IOptions<RedisOptions> options, ILogger<RedisGameCache> logger) : IGameCache
{
    public static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan OperationTimeout = TimeSpan.FromSeconds(1);
    private bool _unavailableForRequest;
    public static string Key(Guid gameId) => $"games:v1:{gameId:D}";

    public async Task<GetGameResult?> GetAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled || _unavailableForRequest) return null;
        var bytes = await BestEffortAsync(token => cache.GetAsync(Key(gameId), token), gameId, "leitura", cancellationToken);
        if (bytes is not null)
        {
            try
            {
                var value = JsonSerializer.Deserialize<GetGameResult>(bytes, JsonOptions);
                if (IsValid(value, gameId))
                {
                    logger.LogInformation("CACHE ENCONTRADO {GameId}", gameId);
                    return value;
                }
            }
            catch (JsonException) { logger.LogWarning("Conteúdo inválido no cache do jogo {GameId}; consultando os bancos de dados.", gameId); }
            await InvalidateAsync(gameId, cancellationToken);
        }
        logger.LogInformation("CACHE NÃO ENCONTRADO {GameId}", gameId);
        return null;
    }

    public async Task SetAsync(GetGameResult game, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled || _unavailableForRequest || game.DetailsStatus is not ("available" or "notConfigured")) return;
        var bytes = JsonSerializer.SerializeToUtf8Bytes(game, JsonOptions);
        await BestEffortAsync(async token =>
        {
            await cache.SetAsync(Key(game.GameId), bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeToLive }, token);
            return true;
        }, game.GameId, "gravação", cancellationToken);
    }

    public async Task InvalidateAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        if (!options.Value.Enabled || _unavailableForRequest) return;
        var removed = await BestEffortAsync(async token =>
        {
            await cache.RemoveAsync(Key(gameId), token);
            return true;
        }, gameId, "invalidação", cancellationToken);
        if (removed) logger.LogInformation("CACHE INVALIDADO {GameId}", gameId);
    }

    private static bool IsValid(GetGameResult? value, Guid gameId)
    {
        if (value is null || value.GameId != gameId || string.IsNullOrWhiteSpace(value.Title) ||
            string.IsNullOrWhiteSpace(value.Description) || value.Price < 0) return false;
        if (value.DetailsStatus == "notConfigured") return value.Details is null;
        if (value.DetailsStatus != "available" || value.Details is not { SchemaVersion: 1, Content: not null }) return false;
        try { value.Details.Content.NormalizeAndValidate(); return true; }
        catch (ArgumentException) { return false; }
    }

    private async Task<T?> BestEffortAsync<T>(Func<CancellationToken, Task<T>> action, Guid gameId, string operation, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(OperationTimeout);
        try { return await action(deadline.Token).WaitAsync(deadline.Token); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _unavailableForRequest = true;
            logger.LogWarning("Tempo limite do Redis excedido durante {Operation} do cache do jogo {GameId}; continuando sem cache.", operation, gameId);
        }
        catch (Exception exception) when (exception is RedisException or TimeoutException)
        {
            _unavailableForRequest = true;
            logger.LogWarning("Redis indisponível durante {Operation} do cache do jogo {GameId}; continuando sem cache.", operation, gameId);
        }
        return default;
    }
}
