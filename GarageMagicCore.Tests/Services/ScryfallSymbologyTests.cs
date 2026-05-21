using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GarageMagicCore.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace GarageMagicCore.Tests.Services;

/// <summary>
/// Tests for ScryfallService.GetSymbologyAsync and the ScryfallController symbology endpoint.
/// Uses a fake HttpMessageHandler to avoid real network calls.
/// </summary>
public class ScryfallSymbologyTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Sample minimal Scryfall symbology JSON with three symbols.</summary>
    private const string ValidScryfallJson = """
        {
          "object": "list",
          "has_more": false,
          "data": [
            {
              "object": "card_symbol",
              "symbol": "{W}",
              "svg_uri": "https://svgs.scryfall.io/card-symbols/W.svg",
              "description": "one white mana",
              "cmc": 1.0,
              "appears_in_mana_costs": true,
              "colors": ["W"]
            },
            {
              "object": "card_symbol",
              "symbol": "{U}",
              "svg_uri": "https://svgs.scryfall.io/card-symbols/U.svg",
              "description": "one blue mana",
              "cmc": 1.0,
              "appears_in_mana_costs": true,
              "colors": ["U"]
            },
            {
              "object": "card_symbol",
              "symbol": "{T}",
              "svg_uri": "https://svgs.scryfall.io/card-symbols/T.svg",
              "description": "tap this permanent",
              "cmc": 0.0,
              "appears_in_mana_costs": false,
              "colors": []
            }
          ]
        }
        """;

    /// <summary>Scryfall JSON where some entries have missing/empty required fields.</summary>
    private const string MalformedScryfallJson = """
        {
          "object": "list",
          "has_more": false,
          "data": [
            { "symbol": "{W}", "svg_uri": "https://svgs.scryfall.io/card-symbols/W.svg", "cmc": 1.0, "appears_in_mana_costs": true, "colors": ["W"] },
            { "symbol": "",    "svg_uri": "https://svgs.scryfall.io/card-symbols/X.svg", "cmc": 0.0, "appears_in_mana_costs": false, "colors": [] },
            { "symbol": "{B}", "svg_uri": "",                                             "cmc": 1.0, "appears_in_mana_costs": true, "colors": ["B"] }
          ]
        }
        """;

    private static ScryfallService CreateService(
        HttpResponseMessage httpResponse,
        IMemoryCache? cache = null)
    {
        var handler = new FakeHttpMessageHandler(httpResponse);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("https://api.scryfall.com/") };
        return new ScryfallService(client, cache ?? new MemoryCache(new MemoryCacheOptions()), NullLogger<ScryfallService>.Instance);
    }

    private static HttpResponseMessage OkJson(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage ServerError() =>
        new(HttpStatusCode.InternalServerError);

    // ── Service tests ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSymbology_ValidUpstream_ReturnsMappedSymbols()
    {
        var svc = CreateService(OkJson(ValidScryfallJson));

        var result = await svc.GetSymbologyAsync();

        result.Should().NotBeNull();
        result!.Symbols.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetSymbology_MapssvgUriToSvgUri_Correctly()
    {
        var svc = CreateService(OkJson(ValidScryfallJson));

        var result = await svc.GetSymbologyAsync();

        var white = result!.Symbols.First(s => s.Symbol == "{W}");
        white.SvgUri.Should().Be("https://svgs.scryfall.io/card-symbols/W.svg");
        white.Colors.Should().Contain("W");
        white.AppearsInManaCosts.Should().BeTrue();
        white.Cmc.Should().Be(1.0m);
        white.Description.Should().Be("one white mana");
    }

    [Fact]
    public async Task GetSymbology_FiltersOutEntriesWithEmptySymbolOrSvgUri()
    {
        var svc = CreateService(OkJson(MalformedScryfallJson));

        var result = await svc.GetSymbologyAsync();

        // {W} is valid; the empty-symbol and empty-svgUri entries should be dropped
        result!.Symbols.Should().HaveCount(1);
        result.Symbols[0].Symbol.Should().Be("{W}");
    }

    [Fact]
    public async Task GetSymbology_CachesResult_SecondCallDoesNotRefetch()
    {
        var callCount = 0;
        var handler = new CountingFakeHandler(OkJson(ValidScryfallJson), () => callCount++);
        var client  = new HttpClient(handler) { BaseAddress = new Uri("https://api.scryfall.com/") };
        var svc     = new ScryfallService(client, new MemoryCache(new MemoryCacheOptions()), NullLogger<ScryfallService>.Instance);

        await svc.GetSymbologyAsync();
        await svc.GetSymbologyAsync();

        callCount.Should().Be(1, "second call should be served from cache");
    }

    [Fact]
    public async Task GetSymbology_UpstreamFails_NoCache_ReturnsNull()
    {
        var svc = CreateService(ServerError());

        var result = await svc.GetSymbologyAsync();

        result.Should().BeNull("no cache + upstream failure should yield null so caller returns 502");
    }

    [Fact]
    public async Task GetSymbology_UpstreamFails_StaleCache_ReturnsStaleData()
    {
        var cache = new MemoryCache(new MemoryCacheOptions());

        // First call succeeds and populates the cache
        var goodSvc = CreateService(OkJson(ValidScryfallJson), cache);
        await goodSvc.GetSymbologyAsync();

        // Simulate live cache expiry by removing the live key but keeping stale
        cache.Remove("scryfall:symbology:v1");

        // Second call — upstream fails, but stale cache is present
        var failSvc = CreateService(ServerError(), cache);
        var result  = await failSvc.GetSymbologyAsync();

        result.Should().NotBeNull("stale cache should be returned when upstream fails");
        result!.Symbols.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetSymbology_NonEmptyResult_ContainsAtLeastBasicColors()
    {
        var svc = CreateService(OkJson(ValidScryfallJson));

        var result = await svc.GetSymbologyAsync();

        result!.Symbols.Select(s => s.Symbol)
            .Should().Contain(new[] { "{W}", "{U}" });
    }

    // ── Controller tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task Controller_GetSymbology_Returns200_WithSymbols()
    {
        var svc        = CreateService(OkJson(ValidScryfallJson));
        var controller = new GarageMagicCore.Controllers.ScryfallController(svc);

        var actionResult = await controller.GetSymbology();

        var ok = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<GarageMagicCore.DTOs.Scryfall.SymbologyDto>().Subject;
        dto.Symbols.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Controller_GetSymbology_Returns502_WhenUpstreamFailsAndNoCache()
    {
        var svc        = CreateService(ServerError());
        var controller = new GarageMagicCore.Controllers.ScryfallController(svc);

        var actionResult = await controller.GetSymbology();

        var status = actionResult.Should().BeOfType<ObjectResult>().Subject;
        status.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    // ── Fake handlers ─────────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }

    private sealed class CountingFakeHandler(HttpResponseMessage response, Action onCall) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            onCall();
            // Return a fresh clone so the content stream isn't exhausted on second call
            var clone = new HttpResponseMessage(response.StatusCode)
            {
                Content = new StringContent(
                    response.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                    Encoding.UTF8, "application/json")
            };
            return Task.FromResult(clone);
        }
    }
}

