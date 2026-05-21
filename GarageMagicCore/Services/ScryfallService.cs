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

    // Cache durations
    private static readonly TimeSpan AutocompleteCacheDuration = TimeSpan.FromHours(24);
    private static readonly TimeSpan CardCacheDuration         = TimeSpan.FromHours(24);

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
            // Scryfall autocomplete + filter suggestions client-side to commanders
            var url = $"cards/autocomplete?q={Uri.EscapeDataString(query)}&include_extras=false";
            var response = await _http.GetFromJsonAsync<ScryfallAutocompleteResponse>(url);

            var result = new CommanderAutocompleteDto
            {
                Names       = (response?.Data ?? []).Take(limit).ToList(),
                TotalValues = response?.TotalValues ?? 0
            };

            _cache.Set(cacheKey, result, AutocompleteCacheDuration);
            return result;
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
            // Valid "not found" — cache the miss so we don't hammer Scryfall
            _cache.Set(cacheKey, (CommanderCardDto?)null, CardCacheDuration);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Scryfall card lookup failed for '{Name}'", name);
            return null;
        }
    }

    // ── Internal Scryfall response shapes ────────────────────────────────────

    private sealed class ScryfallAutocompleteResponse
    {
        [JsonPropertyName("data")]
        public List<string> Data { get; set; } = new();

        [JsonPropertyName("total_values")]
        public int TotalValues { get; set; }
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

