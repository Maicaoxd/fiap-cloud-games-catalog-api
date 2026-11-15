using System.Text.Json;
using CatalogAPI.Application.Games.Search;
using Microsoft.Extensions.Options;

namespace CatalogAPI.Infrastructure.Search;

public sealed class GameSearchIndexer(IServiceScopeFactory scopes, IOptions<ElasticsearchOptions> options, ILogger<GameSearchIndexer> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled) return;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                for (var count = 0; count < 10; count++)
                {
                    using var scope = scopes.CreateScope();
                    var store = scope.ServiceProvider.GetRequiredService<GameSearchOutboxStore>();
                    var message = await store.ClaimAsync(stoppingToken);
                    if (message is null) break;
                    try
                    {
                        var document = JsonSerializer.Deserialize<GameSearchDocument>(message.Payload, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                            ?? throw new JsonException("Documento da outbox inválido.");
                        await scope.ServiceProvider.GetRequiredService<IGameSearchIndex>().IndexAsync(document, message.Id, stoppingToken);
                        await store.CompleteAsync(message, stoppingToken);
                        logger.LogInformation("Jogo {GameId} indexado. Versão: {Version}.", message.GameId, message.Id);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { throw; }
                    catch (Exception)
                    {
                        await store.RetryAsync(message, stoppingToken);
                        logger.LogWarning("Indexação pendente para o jogo {GameId}. Tentativa: {Attempts}.", message.GameId, message.Attempts);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception) { logger.LogWarning("Não foi possível processar a outbox de busca; nova tentativa em instantes."); }
            try { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
