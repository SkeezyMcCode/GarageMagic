using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GarageMagicCore.DTOs.Scryfall;
using Microsoft.Extensions.Caching.Memory;

namespace GarageMagicCore.Services;

/// <summary>
/// Thin proxy over the Scryfall REST API.
/// Respects Scryfall's guidelines: 50-100 ms between requests, User-Agent header, 24-hour cache.
/// See https://scryfall.com/docs/api
/// </summary>
public class ScryfallService : IScryfallService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ScryfallService> _logger;

    private static readonly TimeSpan AutocompleteCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan CardCacheDuration         = TimeSpan.FromHours(24);
    private static readonly TimeSpan SymbologyCacheDuration    = TimeSpan.FromDays(7);

    public ScryfallService(HttpClient http, IMemoryCache cache, ILogger<ScryfallService> logger)
    {
        _http   = http;
        _cache  = cache;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<CommanderAutocompleteDto> AutocompleteCommanderAsync(string query, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return new CommanderAutocompleteDto();

        var cacheKey = $"scryfall:autocomplete:{query.ToLowerInvariant()}";
        if (_cache.TryGetValue(cacheKey, out CommanderAutocompleteDto? cached) && cached != null)
            return cached;

        try
        {
            // Search restricted to commander-legal cards only.
            // "is:commander" matches legendary creatures + planeswalkers with the commander rule.
            var searchQuery = $"is:commander name:{query}";
            var url = $"cards/search?q={Uri.EscapeDataString(searchQuery)}&order=name&unique=cards";
            var response = await _http.GetFromJsonAsync<ScryfallSearchResponse>(url);

            var names = (response?.Data ?? [])
                .Select(c => c.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Take(limit)
                .ToList();

            var result = new CommanderAutocompleteDto
            {
                Names       = names,
                TotalValues = response?.TotalCards ?? names.Count
            };

            _cache.Set(cacheKey, result, AutocompleteCacheDuration);
            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // 404 from Scryfall search = no cards matched — not an error
            var empty = new CommanderAutocompleteDto();
            _cache.Set(cacheKey, empty, AutocompleteCacheDuration);
            return empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scryfall autocomplete failed for query '{Query}'", query);
            return new CommanderAutocompleteDto();
        }
    }

    /// <inheritdoc/>
    public async Task<CommanderCardDto?> LookupCommanderAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;

        var cacheKey = $"scryfall:card:{name.ToLowerInvariant().Trim()}";
        if (_cache.TryGetValue(cacheKey, out CommanderCardDto? cached))
            return cached;

        try
        {
            // Fuzzy search so minor typos don't fail
            var url      = $"cards/named?fuzzy={Uri.EscapeDataString(name)}";
            var response = await _http.GetFromJsonAsync<ScryfallCardResponse>(url);

            if (response == null || response.Object == "error")
            {
                _cache.Set(cacheKey, (CommanderCardDto?)null, CardCacheDuration);
                return null;
            }

            // Resolve image — handle DFC (double-faced) cards
            string? imageUri = null;
            if (response.ImageUris?.TryGetValue("normal", out var frontUri) == true)
                imageUri = frontUri;
            else if (response.CardFaces?.Count > 0)
                response.CardFaces[0].ImageUris?.TryGetValue("normal", out imageUri);

            var dto = new CommanderCardDto
            {
                ScryfallId    = response.Id,
                Name          = response.Name,
                ImageUri      = imageUri,
                ManaCost      = response.ManaCost,
                ColorIdentity = response.ColorIdentity ?? [],
                TypeLine      = response.TypeLine,
                OracleText    = response.OracleText
            };

            _cache.Set(cacheKey, dto, CardCacheDuration);
            return dto;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _cache.Set(cacheKey, (CommanderCardDto?)null, CardCacheDuration);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scryfall card lookup failed for '{Name}'", name);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<SymbologyDto?> GetSymbologyAsync()
    {
        const string cacheKey      = "scryfall:symbology:v1";
        const string staleCacheKey = "scryfall:symbology:v1:stale";

        // ── Cache hit ────────────────────────────────────────────────────────
        if (_cache.TryGetValue(cacheKey, out SymbologyDto? cached) && cached != null)
        {
            _logger.LogDebug("Scryfall symbology: cache hit ({Count} symbols)", cached.Symbols.Count);
            return cached;
        }

        // ── Fetch from Scryfall ───────────────────────────────────────────────
        _logger.LogInformation("Scryfall symbology: cache miss — fetching from Scryfall");
        try
        {
            var response = await _http.GetFromJsonAsync<ScryfallListResponse<ScryfallSymbolResponse>>("symbology");

            if (response?.Data is not { Count: > 0 })
            {
                _logger.LogWarning("Scryfall symbology: upstream returned empty or null data list");
                // Fall through to stale cache / null return
            }
            else
            {
                var symbols = response.Data
                    .Where(s => !string.IsNullOrWhiteSpace(s.Symbol) && !string.IsNullOrWhiteSpace(s.SvgUri))
                    .Select(s => new ManaSymbolDto
                    {
                        Symbol             = s.Symbol,
                        SvgUri             = s.SvgUri,
                        Description        = s.Description,
                        Cmc                = s.Cmc,
                        AppearsInManaCosts = s.AppearsInManaCosts,
                        Colors             = s.Colors ?? []
                    })
                    .ToList();

                _logger.LogInformation("Scryfall symbology: fetched {Count} symbols from upstream", symbols.Count);

                var result = new SymbologyDto { Symbols = symbols };

                // Store as both live cache and stale fallback
                _cache.Set(cacheKey,      result, SymbologyCacheDuration);
                _cache.Set(staleCacheKey, result, TimeSpan.FromDays(30));

                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scryfall symbology: upstream fetch failed");
        }

        // ── Stale cache fallback ─────────────────────────────────────────────
        if (_cache.TryGetValue(staleCacheKey, out SymbologyDto? stale) && stale != null)
        {
            _logger.LogWarning("Scryfall symbology: returning stale cached data ({Count} symbols)", stale.Symbols.Count);
            return stale;
        }

        // ── No data at all ───────────────────────────────────────────────────
        _logger.LogError("Scryfall symbology: no cache and upstream failed — returning null (502)");
        return null;
    }

    // ── Internal Scryfall response shapes ────────────────────────────────────

    private sealed class ScryfallListResponse<T>
    {
        [JsonPropertyName("data")]
        public List<T> Data { get; set; } = new();

        [JsonPropertyName("total_cards")]
        public int TotalCards { get; set; }
    }

    private sealed class ScryfallSearchResponse
    {
        [JsonPropertyName("data")]
        public List<ScryfallCardResponse> Data { get; set; } = new();

        [JsonPropertyName("total_cards")]
        public int TotalCards { get; set; }
    }

    private sealed class ScryfallSymbolResponse
    {
        [JsonPropertyName("symbol")]
        public string Symbol { get; set; } = string.Empty;

        [JsonPropertyName("svg_uri")]
        public string SvgUri { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("cmc")]
        public decimal Cmc { get; set; }

        [JsonPropertyName("appears_in_mana_costs")]
        public bool AppearsInManaCosts { get; set; }

        [JsonPropertyName("colors")]
        public List<string>? Colors { get; set; }
    }

    private sealed class ScryfallCardResponse
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("mana_cost")]
        public string? ManaCost { get; set; }

        [JsonPropertyName("type_line")]
        public string? TypeLine { get; set; }

        [JsonPropertyName("oracle_text")]
        public string? OracleText { get; set; }

        [JsonPropertyName("color_identity")]
        public List<string>? ColorIdentity { get; set; }

        [JsonPropertyName("image_uris")]
        public Dictionary<string, string>? ImageUris { get; set; }

        [JsonPropertyName("card_faces")]
        public List<ScryfallCardFace>? CardFaces { get; set; }
    }

    private sealed class ScryfallCardFace
    {
        [JsonPropertyName("image_uris")]
        public Dictionary<string, string>? ImageUris { get; set; }
    }
}
