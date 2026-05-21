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
                // Fall through to stale cache / static fallback
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

        // ── Static fallback — Scryfall CDN SVGs are publicly accessible even when the API is down ──
        _logger.LogWarning("Scryfall symbology: upstream unreachable and no cache — returning static fallback");
        return StaticSymbologyFallback;
    }

    /// <summary>
    /// Hardcoded list of the most common MTG mana symbols using Scryfall's public SVG CDN.
    /// Used as a last-resort fallback when the Scryfall API is unreachable and the cache is cold.
    /// </summary>
    private static readonly SymbologyDto StaticSymbologyFallback = new()
    {
        Symbols =
        [
            // ── Basic mana ──────────────────────────────────────────────────
            new ManaSymbolDto { Symbol = "{W}", SvgUri = "https://svgs.scryfall.io/card-symbols/W.svg",   Description = "one white mana",        Cmc = 1, AppearsInManaCosts = true,  Colors = ["W"] },
            new ManaSymbolDto { Symbol = "{U}", SvgUri = "https://svgs.scryfall.io/card-symbols/U.svg",   Description = "one blue mana",         Cmc = 1, AppearsInManaCosts = true,  Colors = ["U"] },
            new ManaSymbolDto { Symbol = "{B}", SvgUri = "https://svgs.scryfall.io/card-symbols/B.svg",   Description = "one black mana",        Cmc = 1, AppearsInManaCosts = true,  Colors = ["B"] },
            new ManaSymbolDto { Symbol = "{R}", SvgUri = "https://svgs.scryfall.io/card-symbols/R.svg",   Description = "one red mana",          Cmc = 1, AppearsInManaCosts = true,  Colors = ["R"] },
            new ManaSymbolDto { Symbol = "{G}", SvgUri = "https://svgs.scryfall.io/card-symbols/G.svg",   Description = "one green mana",        Cmc = 1, AppearsInManaCosts = true,  Colors = ["G"] },
            new ManaSymbolDto { Symbol = "{C}", SvgUri = "https://svgs.scryfall.io/card-symbols/C.svg",   Description = "one colorless mana",    Cmc = 1, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{S}", SvgUri = "https://svgs.scryfall.io/card-symbols/S.svg",   Description = "one snow mana",         Cmc = 1, AppearsInManaCosts = true,  Colors = []    },
            // ── Generic / variable mana ──────────────────────────────────────
            new ManaSymbolDto { Symbol = "{X}", SvgUri = "https://svgs.scryfall.io/card-symbols/X.svg",   Description = "X generic mana",        Cmc = 0, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{Y}", SvgUri = "https://svgs.scryfall.io/card-symbols/Y.svg",   Description = "Y generic mana",        Cmc = 0, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{Z}", SvgUri = "https://svgs.scryfall.io/card-symbols/Z.svg",   Description = "Z generic mana",        Cmc = 0, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{0}", SvgUri = "https://svgs.scryfall.io/card-symbols/0.svg",   Description = "zero mana",             Cmc = 0, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{½}", SvgUri = "https://svgs.scryfall.io/card-symbols/HALF.svg",Description = "one-half generic mana", Cmc = 0.5m, AppearsInManaCosts = true, Colors = [] },
            new ManaSymbolDto { Symbol = "{1}", SvgUri = "https://svgs.scryfall.io/card-symbols/1.svg",   Description = "one generic mana",      Cmc = 1, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{2}", SvgUri = "https://svgs.scryfall.io/card-symbols/2.svg",   Description = "two generic mana",      Cmc = 2, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{3}", SvgUri = "https://svgs.scryfall.io/card-symbols/3.svg",   Description = "three generic mana",    Cmc = 3, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{4}", SvgUri = "https://svgs.scryfall.io/card-symbols/4.svg",   Description = "four generic mana",     Cmc = 4, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{5}", SvgUri = "https://svgs.scryfall.io/card-symbols/5.svg",   Description = "five generic mana",     Cmc = 5, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{6}", SvgUri = "https://svgs.scryfall.io/card-symbols/6.svg",   Description = "six generic mana",      Cmc = 6, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{7}", SvgUri = "https://svgs.scryfall.io/card-symbols/7.svg",   Description = "seven generic mana",    Cmc = 7, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{8}", SvgUri = "https://svgs.scryfall.io/card-symbols/8.svg",   Description = "eight generic mana",    Cmc = 8, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{9}", SvgUri = "https://svgs.scryfall.io/card-symbols/9.svg",   Description = "nine generic mana",     Cmc = 9, AppearsInManaCosts = true,  Colors = []    },
            new ManaSymbolDto { Symbol = "{10}", SvgUri = "https://svgs.scryfall.io/card-symbols/10.svg", Description = "ten generic mana",      Cmc = 10, AppearsInManaCosts = true, Colors = []    },
            new ManaSymbolDto { Symbol = "{11}", SvgUri = "https://svgs.scryfall.io/card-symbols/11.svg", Description = "eleven generic mana",   Cmc = 11, AppearsInManaCosts = true, Colors = []    },
            new ManaSymbolDto { Symbol = "{12}", SvgUri = "https://svgs.scryfall.io/card-symbols/12.svg", Description = "twelve generic mana",   Cmc = 12, AppearsInManaCosts = true, Colors = []    },
            new ManaSymbolDto { Symbol = "{13}", SvgUri = "https://svgs.scryfall.io/card-symbols/13.svg", Description = "thirteen generic mana", Cmc = 13, AppearsInManaCosts = true, Colors = []    },
            new ManaSymbolDto { Symbol = "{14}", SvgUri = "https://svgs.scryfall.io/card-symbols/14.svg", Description = "fourteen generic mana", Cmc = 14, AppearsInManaCosts = true, Colors = []    },
            new ManaSymbolDto { Symbol = "{15}", SvgUri = "https://svgs.scryfall.io/card-symbols/15.svg", Description = "fifteen generic mana",  Cmc = 15, AppearsInManaCosts = true, Colors = []    },
            new ManaSymbolDto { Symbol = "{16}", SvgUri = "https://svgs.scryfall.io/card-symbols/16.svg", Description = "sixteen generic mana",  Cmc = 16, AppearsInManaCosts = true, Colors = []    },
            new ManaSymbolDto { Symbol = "{20}", SvgUri = "https://svgs.scryfall.io/card-symbols/20.svg", Description = "twenty generic mana",   Cmc = 20, AppearsInManaCosts = true, Colors = []    },
            // ── Hybrid mana ──────────────────────────────────────────────────
            new ManaSymbolDto { Symbol = "{W/U}", SvgUri = "https://svgs.scryfall.io/card-symbols/WU.svg", Description = "one white or blue mana",  Cmc = 1, AppearsInManaCosts = true, Colors = ["W","U"] },
            new ManaSymbolDto { Symbol = "{W/B}", SvgUri = "https://svgs.scryfall.io/card-symbols/WB.svg", Description = "one white or black mana", Cmc = 1, AppearsInManaCosts = true, Colors = ["W","B"] },
            new ManaSymbolDto { Symbol = "{U/B}", SvgUri = "https://svgs.scryfall.io/card-symbols/UB.svg", Description = "one blue or black mana",  Cmc = 1, AppearsInManaCosts = true, Colors = ["U","B"] },
            new ManaSymbolDto { Symbol = "{U/R}", SvgUri = "https://svgs.scryfall.io/card-symbols/UR.svg", Description = "one blue or red mana",    Cmc = 1, AppearsInManaCosts = true, Colors = ["U","R"] },
            new ManaSymbolDto { Symbol = "{B/R}", SvgUri = "https://svgs.scryfall.io/card-symbols/BR.svg", Description = "one black or red mana",   Cmc = 1, AppearsInManaCosts = true, Colors = ["B","R"] },
            new ManaSymbolDto { Symbol = "{B/G}", SvgUri = "https://svgs.scryfall.io/card-symbols/BG.svg", Description = "one black or green mana", Cmc = 1, AppearsInManaCosts = true, Colors = ["B","G"] },
            new ManaSymbolDto { Symbol = "{R/W}", SvgUri = "https://svgs.scryfall.io/card-symbols/RW.svg", Description = "one red or white mana",   Cmc = 1, AppearsInManaCosts = true, Colors = ["R","W"] },
            new ManaSymbolDto { Symbol = "{R/G}", SvgUri = "https://svgs.scryfall.io/card-symbols/RG.svg", Description = "one red or green mana",   Cmc = 1, AppearsInManaCosts = true, Colors = ["R","G"] },
            new ManaSymbolDto { Symbol = "{G/W}", SvgUri = "https://svgs.scryfall.io/card-symbols/GW.svg", Description = "one green or white mana", Cmc = 1, AppearsInManaCosts = true, Colors = ["G","W"] },
            new ManaSymbolDto { Symbol = "{G/U}", SvgUri = "https://svgs.scryfall.io/card-symbols/GU.svg", Description = "one green or blue mana",  Cmc = 1, AppearsInManaCosts = true, Colors = ["G","U"] },
            // ── 2-hybrid mana ────────────────────────────────────────────────
            new ManaSymbolDto { Symbol = "{2/W}", SvgUri = "https://svgs.scryfall.io/card-symbols/2W.svg", Description = "two or one white mana", Cmc = 2, AppearsInManaCosts = true, Colors = ["W"] },
            new ManaSymbolDto { Symbol = "{2/U}", SvgUri = "https://svgs.scryfall.io/card-symbols/2U.svg", Description = "two or one blue mana",  Cmc = 2, AppearsInManaCosts = true, Colors = ["U"] },
            new ManaSymbolDto { Symbol = "{2/B}", SvgUri = "https://svgs.scryfall.io/card-symbols/2B.svg", Description = "two or one black mana", Cmc = 2, AppearsInManaCosts = true, Colors = ["B"] },
            new ManaSymbolDto { Symbol = "{2/R}", SvgUri = "https://svgs.scryfall.io/card-symbols/2R.svg", Description = "two or one red mana",   Cmc = 2, AppearsInManaCosts = true, Colors = ["R"] },
            new ManaSymbolDto { Symbol = "{2/G}", SvgUri = "https://svgs.scryfall.io/card-symbols/2G.svg", Description = "two or one green mana", Cmc = 2, AppearsInManaCosts = true, Colors = ["G"] },
            // ── Phyrexian mana ───────────────────────────────────────────────
            new ManaSymbolDto { Symbol = "{W/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/WP.svg", Description = "one white mana or 2 life", Cmc = 1, AppearsInManaCosts = true, Colors = ["W"] },
            new ManaSymbolDto { Symbol = "{U/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/UP.svg", Description = "one blue mana or 2 life",  Cmc = 1, AppearsInManaCosts = true, Colors = ["U"] },
            new ManaSymbolDto { Symbol = "{B/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/BP.svg", Description = "one black mana or 2 life", Cmc = 1, AppearsInManaCosts = true, Colors = ["B"] },
            new ManaSymbolDto { Symbol = "{R/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/RP.svg", Description = "one red mana or 2 life",   Cmc = 1, AppearsInManaCosts = true, Colors = ["R"] },
            new ManaSymbolDto { Symbol = "{G/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/GP.svg", Description = "one green mana or 2 life", Cmc = 1, AppearsInManaCosts = true, Colors = ["G"] },
            new ManaSymbolDto { Symbol = "{P}",   SvgUri = "https://svgs.scryfall.io/card-symbols/P.svg",  Description = "2 life",                   Cmc = 1, AppearsInManaCosts = true, Colors = []    },
            // ── Hybrid Phyrexian ─────────────────────────────────────────────
            new ManaSymbolDto { Symbol = "{W/U/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/WUP.svg", Description = "one white or blue mana or 2 life", Cmc = 1, AppearsInManaCosts = true, Colors = ["W","U"] },
            new ManaSymbolDto { Symbol = "{U/B/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/UBP.svg", Description = "one blue or black mana or 2 life", Cmc = 1, AppearsInManaCosts = true, Colors = ["U","B"] },
            new ManaSymbolDto { Symbol = "{B/R/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/BRP.svg", Description = "one black or red mana or 2 life",  Cmc = 1, AppearsInManaCosts = true, Colors = ["B","R"] },
            new ManaSymbolDto { Symbol = "{R/G/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/RGP.svg", Description = "one red or green mana or 2 life",  Cmc = 1, AppearsInManaCosts = true, Colors = ["R","G"] },
            new ManaSymbolDto { Symbol = "{G/W/P}", SvgUri = "https://svgs.scryfall.io/card-symbols/GWP.svg", Description = "one green or white mana or 2 life",Cmc = 1, AppearsInManaCosts = true, Colors = ["G","W"] },
            // ── Tap / Untap / Energy / Other ────────────────────────────────
            new ManaSymbolDto { Symbol = "{T}", SvgUri = "https://svgs.scryfall.io/card-symbols/T.svg",  Description = "tap this permanent",   Cmc = 0, AppearsInManaCosts = false, Colors = [] },
            new ManaSymbolDto { Symbol = "{Q}", SvgUri = "https://svgs.scryfall.io/card-symbols/Q.svg",  Description = "untap this permanent", Cmc = 0, AppearsInManaCosts = false, Colors = [] },
            new ManaSymbolDto { Symbol = "{E}", SvgUri = "https://svgs.scryfall.io/card-symbols/E.svg",  Description = "one energy counter",   Cmc = 0, AppearsInManaCosts = false, Colors = [] },
            new ManaSymbolDto { Symbol = "{A}", SvgUri = "https://svgs.scryfall.io/card-symbols/A.svg",  Description = "acorn counter",        Cmc = 0, AppearsInManaCosts = false, Colors = [] },
        ]
    };

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
