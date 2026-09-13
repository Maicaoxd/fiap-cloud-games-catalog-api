using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Games.Details;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CatalogAPI.Infrastructure.Persistence.Mongo;

public sealed class MongoGameDetailsRepository : IGameDetailsRepository
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly TimeSpan _timeout;

    public MongoGameDetailsRepository(IMongoClient client, IOptions<MongoDbOptions> options)
    {
        var settings = options.Value;
        _collection = client.GetDatabase(settings.DatabaseName).GetCollection<BsonDocument>(settings.CollectionName);
        _timeout = TimeSpan.FromSeconds(settings.OperationTimeoutSeconds);
    }

    public Task<GameDetailsResult?> GetByGameIdAsync(Guid gameId, CancellationToken cancellationToken = default) =>
        ExecuteAsync<GameDetailsResult?>(async token =>
        {
            var document = await _collection.Find(Filter(gameId)).FirstOrDefaultAsync(token);
            return document is null ? null : GameDetailsDocument.Deserialize(document);
        }, cancellationToken);

    public Task<GameDetailsResult> UpsertAsync(Guid gameId, GameDetailsContent content, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async token =>
        {
            var document = GameDetailsDocument.SerializeContent(content.NormalizeAndValidate());
            var now = DateTime.UtcNow;
            document["schemaVersion"] = 1;
            document["updatedAt"] = new BsonDateTime(now);
            var update = new BsonDocument
            {
                { "$set", document },
                { "$setOnInsert", new BsonDocument("createdAt", new BsonDateTime(now)) }
            };
            var stored = await _collection.FindOneAndUpdateAsync(Filter(gameId), update,
                new FindOneAndUpdateOptions<BsonDocument> { IsUpsert = true, ReturnDocument = ReturnDocument.After }, token);
            return GameDetailsDocument.Deserialize(stored);
        }, cancellationToken);

    private static FilterDefinition<BsonDocument> Filter(Guid gameId) =>
        new BsonDocument("_id", GameDetailsDocument.Id(gameId));

    private async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(_timeout);
        try { return await operation(deadline.Token); }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        { throw new GameDetailsUnavailableException(exception); }
        catch (Exception exception) when (exception is MongoConnectionException or MongoAuthenticationException or MongoExecutionTimeoutException or TimeoutException)
        { throw new GameDetailsUnavailableException(exception); }
    }
}
