using System.Text.Json;
using CatalogAPI.Application.Games.Details;
using MongoDB.Bson;
using MongoDB.Bson.IO;

namespace CatalogAPI.Infrastructure.Persistence.Mongo;

public static class GameDetailsDocument
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly JsonWriterSettings JsonWriter = new() { OutputMode = JsonOutputMode.RelaxedExtendedJson };

    public static BsonDocument SerializeContent(GameDetailsContent content) =>
        BsonDocument.Parse(JsonSerializer.Serialize(content, JsonOptions));
    public static BsonBinaryData Id(Guid gameId) => new(gameId, GuidRepresentation.Standard);

    public static GameDetailsResult Deserialize(BsonDocument document)
    {
        if (document["schemaVersion"].AsInt32 != 1)
            throw new NotSupportedException("Unsupported game details schema version.");
        var content = new BsonDocument(document.Elements.Where(e => e.Name is not ("_id" or "schemaVersion" or "createdAt" or "updatedAt")));
        return new GameDetailsResult(1,
            JsonSerializer.Deserialize<GameDetailsContent>(content.ToJson(JsonWriter), JsonOptions)!.NormalizeAndValidate(),
            document["createdAt"].ToUniversalTime(), document["updatedAt"].ToUniversalTime());
    }
}
