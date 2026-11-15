using System.Text.Json;
using CatalogAPI.Application.Games.Search;
using CatalogAPI.Domain.Games;

namespace CatalogAPI.Infrastructure.Search;

public sealed class GameSearchOutboxMessage
{
    public long Id { get; set; }
    public Guid GameId { get; set; }
    public string Payload { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime NextAttemptAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? LeaseId { get; set; }
    public DateTime? LeaseUntil { get; set; }
    public int Attempts { get; set; }

    public static GameSearchOutboxMessage From(Game game) => new()
    {
        GameId = game.Id,
        Payload = JsonSerializer.Serialize(GameSearchDocument.From(game), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
        CreatedAt = DateTime.UtcNow,
        NextAttemptAt = DateTime.UtcNow
    };
}
