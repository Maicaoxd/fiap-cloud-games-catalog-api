using System.Text.Json;
using CatalogAPI.Application.Abstractions.Caching;
using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Games.Deactivate;
using CatalogAPI.Application.Games.Details;
using CatalogAPI.Application.Games.Get;
using CatalogAPI.Application.Games.Update;
using CatalogAPI.Domain.Games;
using CatalogAPI.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Application.Games;

[Trait("Category", "Unit")]
public sealed class GameCacheTests
{
    private static RedisGameCache Adapter(IDistributedCache cache, bool enabled = true) =>
        new(cache, Options.Create(new RedisOptions { Enabled = enabled }), NullLogger<RedisGameCache>.Instance);
    private static GetGameResult Result(Guid id, string status = "available") =>
        new(id, "Game", "Description", 10,
            status == "available" ? new GameDetailsResult(1, new() { Developer = "Studio" }, DateTime.UtcNow, DateTime.UtcNow) : null,
            status);

    [Fact]
    public async Task Adapter_round_trip_preserves_composed_response_and_uses_absolute_five_minute_ttl()
    {
        var cache = Substitute.For<IDistributedCache>();
        var adapter = Adapter(cache);
        var result = Result(Guid.NewGuid());
        byte[]? stored = null;
        cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(call => { stored = call.ArgAt<byte[]>(1); return Task.CompletedTask; });
        await adapter.SetAsync(result);
        await cache.Received(1).SetAsync(RedisGameCache.Key(result.GameId), Arg.Any<byte[]>(),
            Arg.Is<DistributedCacheEntryOptions>(o => o.AbsoluteExpirationRelativeToNow == TimeSpan.FromMinutes(5) && o.SlidingExpiration == null),
            Arg.Any<CancellationToken>());
        cache.GetAsync(RedisGameCache.Key(result.GameId), Arg.Any<CancellationToken>()).Returns(stored);
        var restored = await adapter.GetAsync(result.GameId);
        restored!.GameId.ShouldBe(result.GameId); restored.Price.ShouldBe(result.Price);
        restored.Details!.Content.Developer.ShouldBe("Studio"); restored.Details.CreatedAt.ShouldBe(result.Details!.CreatedAt);
        RedisGameCache.Key(result.GameId).ShouldBe($"games:v1:{result.GameId:D}");
    }

    [Fact]
    public async Task Adapter_never_caches_degraded_responses()
    {
        var cache = Substitute.For<IDistributedCache>();
        await Adapter(cache).SetAsync(Result(Guid.NewGuid(), "unavailable"));
        await cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default!);
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("null")]
    [InlineData("wrong-id")]
    [InlineData("unavailable")]
    [InlineData("missing-fields")]
    public async Task Invalid_cache_entry_is_removed_and_treated_as_miss(string kind)
    {
        var cache = Substitute.For<IDistributedCache>();
        var id = Guid.NewGuid();
        var value = kind switch
        {
            "wrong-id" => JsonSerializer.SerializeToUtf8Bytes(Result(Guid.NewGuid()), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            "unavailable" => JsonSerializer.SerializeToUtf8Bytes(Result(id, "unavailable"), new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            "missing-fields" => System.Text.Encoding.UTF8.GetBytes($"{{\"gameId\":\"{id}\",\"detailsStatus\":\"available\"}}"),
            _ => System.Text.Encoding.UTF8.GetBytes(kind)
        };
        cache.GetAsync(RedisGameCache.Key(id), Arg.Any<CancellationToken>()).Returns(value);
        (await Adapter(cache).GetAsync(id)).ShouldBeNull();
        await cache.Received(1).RemoveAsync(RedisGameCache.Key(id), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Disabled_cache_never_accesses_redis()
    {
        var cache = Substitute.For<IDistributedCache>();
        var adapter = Adapter(cache, false);
        var result = Result(Guid.NewGuid());
        (await adapter.GetAsync(result.GameId)).ShouldBeNull();
        await adapter.SetAsync(result); await adapter.InvalidateAsync(result.GameId);
        cache.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task Redis_operational_errors_do_not_fail_reads_writes_or_invalidation()
    {
        var cache = Substitute.For<IDistributedCache>();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<byte[]?>(new TimeoutException()));
        cache.SetAsync(Arg.Any<string>(), Arg.Any<byte[]>(), Arg.Any<DistributedCacheEntryOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new TimeoutException()));
        cache.RemoveAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException(new TimeoutException()));
        var adapter = Adapter(cache);
        var result = Result(Guid.NewGuid());
        (await adapter.GetAsync(result.GameId)).ShouldBeNull();
        await Should.NotThrowAsync(() => adapter.SetAsync(result));
        await Should.NotThrowAsync(() => adapter.InvalidateAsync(result.GameId));
        await cache.DidNotReceiveWithAnyArgs().SetAsync(default!, default!, default!);
        await cache.DidNotReceiveWithAnyArgs().RemoveAsync(default!);
    }

    [Fact]
    public async Task Cache_timeout_is_bounded_even_when_provider_does_not_complete()
    {
        var cache = Substitute.For<IDistributedCache>();
        var pending = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(pending.Task);
        var clock = System.Diagnostics.Stopwatch.StartNew();
        (await Adapter(cache).GetAsync(Guid.NewGuid())).ShouldBeNull();
        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(3));
        pending.TrySetResult(null);
    }

    [Fact]
    public async Task Client_cancellation_and_unexpected_errors_are_not_masked()
    {
        var cache = Substitute.For<IDistributedCache>();
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromCanceled<byte[]?>(cancelled.Token));
        await Should.ThrowAsync<OperationCanceledException>(() => Adapter(cache).GetAsync(Guid.NewGuid(), cancelled.Token));
        cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<byte[]?>(new InvalidOperationException()));
        await Should.ThrowAsync<InvalidOperationException>(() => Adapter(cache).GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task Cache_hit_skips_both_sql_and_mongo()
    {
        var games = Substitute.For<IGameRepository>();
        var details = Substitute.For<IGameDetailsRepository>();
        var cache = Substitute.For<IGameCache>();
        var result = Result(Guid.NewGuid());
        cache.GetAsync(result.GameId, Arg.Any<CancellationToken>()).Returns(result);
        var useCase = new GetGameUseCase(games, details, NullLogger<GetGameUseCase>.Instance, cache);
        (await useCase.ExecuteAsync(result.GameId)).ShouldBe(result);
        await games.DidNotReceiveWithAnyArgs().GetByIdAsync(default);
        await details.DidNotReceiveWithAnyArgs().GetByGameIdAsync(default);
        await cache.DidNotReceiveWithAnyArgs().SetAsync(default!);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Cache_miss_populates_only_non_degraded_responses(bool unavailable)
    {
        var games = Substitute.For<IGameRepository>();
        var details = Substitute.For<IGameDetailsRepository>();
        var cache = Substitute.For<IGameCache>();
        var game = Game.Create("Game", "Description", 10, Guid.NewGuid());
        games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        if (unavailable) details.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<GameDetailsResult?>(new GameDetailsUnavailableException()));
        var result = await new GetGameUseCase(games, details, NullLogger<GetGameUseCase>.Instance, cache).ExecuteAsync(game.Id);
        result.DetailsStatus.ShouldBe(unavailable ? "unavailable" : "notConfigured");
        if (unavailable) await cache.DidNotReceiveWithAnyArgs().SetAsync(default!);
        else await cache.Received(1).SetAsync(result, Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("update")]
    [InlineData("deactivate")]
    [InlineData("details")]
    public async Task Successful_mutation_invalidates_after_commit_even_if_client_disconnects(string operation)
    {
        var games = Substitute.For<IGameRepository>();
        var details = Substitute.For<IGameDetailsRepository>();
        var cache = Substitute.For<IGameCache>();
        var responsible = Guid.NewGuid();
        var game = Game.Create("Game", "Description", 10, responsible);
        games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        using var disconnected = new CancellationTokenSource();
        var sequence = new List<string>();
        games.UpdateAsync(game, Arg.Any<CancellationToken>()).Returns(_ =>
        {
            sequence.Add("commit"); disconnected.Cancel(); return Task.CompletedTask;
        });
        details.UpsertAsync(game.Id, Arg.Any<GameDetailsContent>(), Arg.Any<CancellationToken>()).Returns(_ =>
        {
            sequence.Add("commit"); disconnected.Cancel(); return Task.FromResult(new GameDetailsResult(1, new(), DateTime.UtcNow, DateTime.UtcNow));
        });
        cache.InvalidateAsync(game.Id, Arg.Any<CancellationToken>()).Returns(call =>
        {
            call.Arg<CancellationToken>().CanBeCanceled.ShouldBeFalse();
            sequence.Add("invalidate"); return Task.CompletedTask;
        });
        switch(operation)
        {
            case "update":
                await new UpdateGameUseCase(games, cache).ExecuteAsync(new(game.Id, "Updated", "Updated description", 15, responsible), disconnected.Token); break;
            case "deactivate":
                await new DeactivateGameUseCase(games, cache).ExecuteAsync(new(game.Id, responsible), disconnected.Token); break;
            default:
                await new UpsertGameDetailsUseCase(games, details, cache).ExecuteAsync(game.Id, new(), disconnected.Token); break;
        }
        sequence.ShouldBe(new[] { "commit", "invalidate" });
    }

    [Theory]
    [InlineData("update")]
    [InlineData("deactivate")]
    [InlineData("details")]
    public async Task Failed_persistence_does_not_report_cache_invalidation(string operation)
    {
        var games = Substitute.For<IGameRepository>();
        var details = Substitute.For<IGameDetailsRepository>();
        var cache = Substitute.For<IGameCache>();
        var responsible = Guid.NewGuid();
        var game = Game.Create("Game", "Description", 10, responsible);
        games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        games.UpdateAsync(game, Arg.Any<CancellationToken>()).Returns(_ => Task.FromException(new InvalidOperationException()));
        details.UpsertAsync(game.Id, Arg.Any<GameDetailsContent>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<GameDetailsResult>(new InvalidOperationException()));
        switch(operation)
        {
            case "update":
                await Should.ThrowAsync<InvalidOperationException>(() => new UpdateGameUseCase(games, cache).ExecuteAsync(new(game.Id, "Updated", "Description", 15, responsible))); break;
            case "deactivate":
                await Should.ThrowAsync<InvalidOperationException>(() => new DeactivateGameUseCase(games, cache).ExecuteAsync(new(game.Id, responsible))); break;
            default:
                await Should.ThrowAsync<InvalidOperationException>(() => new UpsertGameDetailsUseCase(games, details, cache).ExecuteAsync(game.Id, new())); break;
        }
        await cache.DidNotReceiveWithAnyArgs().InvalidateAsync(default);
    }

    [Fact]
    public async Task Repeated_deactivation_still_invalidates_without_updating_sql()
    {
        var games = Substitute.For<IGameRepository>();
        var cache = Substitute.For<IGameCache>();
        var user = Guid.NewGuid();
        var game = Game.Create("Game", "Description", 10, user);
        game.Deactivate(user);
        games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        await new DeactivateGameUseCase(games, cache).ExecuteAsync(new(game.Id, user));
        await games.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
        await cache.Received(1).InvalidateAsync(game.Id, CancellationToken.None);
    }
}
