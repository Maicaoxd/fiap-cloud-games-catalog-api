using System.Reflection;
using System.Text.Json;
using CatalogAPI.Api.Controllers;
using CatalogAPI.Application.Abstractions.Persistence;
using CatalogAPI.Application.Common.Exceptions;
using CatalogAPI.Application.Games.Details;
using CatalogAPI.Application.Games.Get;
using CatalogAPI.Domain.Games;
using CatalogAPI.Infrastructure.Persistence.Mongo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using NSubstitute;
using Shouldly;

namespace CatalogAPI.Tests.Application.Games;

[Trait("Category", "Unit")]
public sealed class GameDetailsTests
{
    [Fact]
    public void Normalizes_missing_arrays_and_dictionaries()
    {
        var content = new GameDetailsContent { Genres = null, Platforms = null, Languages = null, Tags = null, Attributes = null, SystemRequirements = null, Media = new GameMedia() }.NormalizeAndValidate();
        content.Genres.ShouldBeEmpty(); content.Platforms.ShouldBeEmpty(); content.Languages.ShouldBeEmpty(); content.Tags.ShouldBeEmpty();
        content.Attributes.ShouldBeEmpty(); content.SystemRequirements.ShouldBeEmpty(); content.Media!.ScreenshotUrls.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ftp://example.com/cover")]
    [InlineData("javascript:alert(1)")]
    [InlineData("/cover.jpg")]
    [InlineData("")]
    public void Rejects_invalid_urls(string url) =>
        Should.Throw<ArgumentException>(() => new GameDetailsContent { Media = new GameMedia(CoverUrl: url) }.NormalizeAndValidate());

    [Theory]
    [InlineData("https://example.com/cover.jpg")]
    [InlineData("http://example.com/cover.jpg")]
    public void Accepts_http_urls(string url) =>
        Should.NotThrow(() => new GameDetailsContent { Media = new GameMedia(CoverUrl: url) }.NormalizeAndValidate());

    [Theory]
    [InlineData("bad.key")]
    [InlineData("$operator")]
    [InlineData("")]
    public void Rejects_unsafe_attribute_keys(string key) =>
        Should.Throw<ArgumentException>(() => new GameDetailsContent { Attributes = new() { [key] = JsonSerializer.SerializeToElement(true) } }.NormalizeAndValidate());

    [Theory]
    [InlineData("{\"nested\":true}")]
    [InlineData("[{\"nested\":true}]")]
    [InlineData("[[1]]")]
    public void Rejects_nested_attributes(string json) =>
        Should.Throw<ArgumentException>(() => new GameDetailsContent { Attributes = new() { ["value"] = JsonSerializer.Deserialize<JsonElement>(json) } }.NormalizeAndValidate());

    [Theory]
    [InlineData(-1, 20)]
    [InlineData(8, -1)]
    public void Rejects_negative_requirements(int memory, int storage) =>
        Should.Throw<ArgumentException>(() => new GameDetailsContent
        {
            SystemRequirements = new() { ["windows"] = new PlatformRequirements(new HardwareRequirements(MemoryGb: memory, StorageGb: storage)) }
        }.NormalizeAndValidate());

    [Fact]
    public void Rejects_excessive_collections_and_text()
    {
        Should.Throw<ArgumentException>(() => new GameDetailsContent { Developer = new string('x', 151) }.NormalizeAndValidate());
        Should.Throw<ArgumentException>(() => new GameDetailsContent { Tags = Enumerable.Repeat("tag", 51).ToArray() }.NormalizeAndValidate());
        Should.Throw<ArgumentException>(() => new GameDetailsContent { Media = new GameMedia(ScreenshotUrls: Enumerable.Repeat("https://example.com/x", 21).ToArray()) }.NormalizeAndValidate());
        Should.Throw<ArgumentException>(() => new GameDetailsContent { Genres = [""] }.NormalizeAndValidate());
    }

    [Fact]
    public void Bson_contract_preserves_guid_dates_and_flexible_values()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var content = new GameDetailsContent
        {
            Developer = "Studio", Genres = ["Action"], Media = new GameMedia("https://example.com/cover.jpg"),
            Attributes = new() { ["crossPlay"] = JsonSerializer.SerializeToElement(false), ["maxPlayers"] = JsonSerializer.SerializeToElement(1), ["modes"] = JsonSerializer.SerializeToElement(new[] { "solo", "coop" }) },
            SystemRequirements = new() { ["windows"] = new PlatformRequirements(new HardwareRequirements(MemoryGb: 8)) }
        }.NormalizeAndValidate();
        var document = GameDetailsDocument.SerializeContent(content);
        document["_id"] = GameDetailsDocument.Id(id);
        document["schemaVersion"] = 1;
        document["createdAt"] = new BsonDateTime(now);
        document["updatedAt"] = new BsonDateTime(now);
        document["_id"].AsBsonBinaryData.SubType.ShouldBe(BsonBinarySubType.UuidStandard);
        document["_id"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard).ShouldBe(id);
        document.Contains("title").ShouldBeFalse(); document.Contains("price").ShouldBeFalse();
        var restored = GameDetailsDocument.Deserialize(document);
        restored.Content.Developer.ShouldBe("Studio");
        restored.Content.Attributes!["maxPlayers"].GetInt32().ShouldBe(1);
        restored.Content.Attributes["crossPlay"].GetBoolean().ShouldBeFalse();
        restored.Content.SystemRequirements!["windows"].Minimum!.MemoryGb.ShouldBe(8);
        restored.CreatedAt.Kind.ShouldBe(DateTimeKind.Utc);
        (now - restored.CreatedAt).TotalMilliseconds.ShouldBeLessThan(1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Sql_absence_or_inactive_game_never_queries_mongo(bool inactive)
    {
        var games = Substitute.For<IGameRepository>();
        var details = Substitute.For<IGameDetailsRepository>();
        var game = Game.Create("Game", "Description", 10, Guid.NewGuid());
        if (inactive) game.Deactivate(Guid.NewGuid());
        games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(inactive ? game : null);
        await Should.ThrowAsync<GameNotFoundException>(() => new GetGameUseCase(games, details, NullLogger<GetGameUseCase>.Instance).ExecuteAsync(game.Id));
        await details.DidNotReceiveWithAnyArgs().GetByGameIdAsync(default);
        await Should.ThrowAsync<GameNotFoundException>(() => new UpsertGameDetailsUseCase(games, details).ExecuteAsync(game.Id, new()));
        await details.DidNotReceiveWithAnyArgs().UpsertAsync(default, default!);
    }

    [Theory]
    [InlineData("available")]
    [InlineData("notConfigured")]
    [InlineData("unavailable")]
    public async Task Individual_query_reports_details_state_without_changing_sql_fields(string state)
    {
        var games = Substitute.For<IGameRepository>();
        var details = Substitute.For<IGameDetailsRepository>();
        var game = Game.Create("Game", "Description", 10, Guid.NewGuid());
        games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        var result = new GameDetailsResult(1, new GameDetailsContent { Developer = "Studio" }, DateTime.UtcNow, DateTime.UtcNow);
        if (state == "unavailable")
            details.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<GameDetailsResult?>(new GameDetailsUnavailableException()));
        else
            details.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(state == "available" ? result : null);
        var response = await new GetGameUseCase(games, details, NullLogger<GetGameUseCase>.Instance).ExecuteAsync(game.Id);
        response.DetailsStatus.ShouldBe(state); response.Price.ShouldBe(10); response.Title.ShouldBe(game.Title);
        if (state == "available") response.Details.ShouldBe(result); else response.Details.ShouldBeNull();
    }

    [Fact]
    public async Task Query_does_not_hide_cancellation_or_unexpected_errors()
    {
        var games = Substitute.For<IGameRepository>();
        var details = Substitute.For<IGameDetailsRepository>();
        var game = Game.Create("Game", "Description", 10, Guid.NewGuid());
        games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        var useCase = new GetGameUseCase(games, details, NullLogger<GetGameUseCase>.Instance);
        details.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<GameDetailsResult?>(new OperationCanceledException()));
        await Should.ThrowAsync<OperationCanceledException>(() => useCase.ExecuteAsync(game.Id));
        details.GetByGameIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<GameDetailsResult?>(new InvalidOperationException()));
        await Should.ThrowAsync<InvalidOperationException>(() => useCase.ExecuteAsync(game.Id));
    }

    [Fact]
    public async Task Invalid_content_never_writes_and_valid_content_does_not_update_sql()
    {
        var games = Substitute.For<IGameRepository>();
        var details = Substitute.For<IGameDetailsRepository>();
        var game = Game.Create("Game", "Description", 10, Guid.NewGuid());
        games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        var useCase = new UpsertGameDetailsUseCase(games, details);
        await Should.ThrowAsync<ArgumentException>(() => useCase.ExecuteAsync(game.Id, new() { Media = new GameMedia(CoverUrl: "ftp://example.com") }));
        await details.DidNotReceiveWithAnyArgs().UpsertAsync(default, default!);
        var expected = new GameDetailsResult(1, new(), DateTime.UtcNow, DateTime.UtcNow);
        details.UpsertAsync(game.Id, Arg.Any<GameDetailsContent>(), Arg.Any<CancellationToken>()).Returns(expected);
        (await useCase.ExecuteAsync(game.Id, new() { Genres = null })).ShouldBe(expected);
        await details.Received(1).UpsertAsync(game.Id, Arg.Is<GameDetailsContent>(c => c.Genres != null && c.Genres.Length == 0), Arg.Any<CancellationToken>());
        await games.DidNotReceiveWithAnyArgs().UpdateAsync(default!);
    }

    [Fact]
    public async Task Writes_propagate_unavailability_instead_of_reporting_success()
    {
        var games = Substitute.For<IGameRepository>();
        var details = Substitute.For<IGameDetailsRepository>();
        var game = Game.Create("Game", "Description", 10, Guid.NewGuid());
        games.GetByIdAsync(game.Id, Arg.Any<CancellationToken>()).Returns(game);
        details.UpsertAsync(game.Id, Arg.Any<GameDetailsContent>(), Arg.Any<CancellationToken>()).Returns(_ => Task.FromException<GameDetailsResult>(new GameDetailsUnavailableException()));
        await Should.ThrowAsync<GameDetailsUnavailableException>(() => new UpsertGameDetailsUseCase(games, details).ExecuteAsync(game.Id, new()));
    }

    [Fact]
    public void Details_endpoint_requires_administrator_and_limits_request_size()
    {
        var method = typeof(GameDetailsController).GetMethod(nameof(GameDetailsController.UpsertAsync))!;
        method.GetCustomAttribute<AuthorizeAttribute>()!.Roles.ShouldBe("Administrator");
        ((Microsoft.AspNetCore.Http.Metadata.IRequestSizeLimitMetadata)method.GetCustomAttribute<RequestSizeLimitAttribute>()!).MaxRequestBodySize.ShouldBe(65536);
    }
}
