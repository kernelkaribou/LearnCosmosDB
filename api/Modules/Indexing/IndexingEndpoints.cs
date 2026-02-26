using LearnCosmosDB.Shared.Indexing;
using Microsoft.AspNetCore.Mvc;

namespace LearnCosmosDB.Api.Modules.Indexing;

public static class IndexingEndpoints
{
    private static readonly string[] EventTypes =
        ["search", "view_details", "play_trailer", "add_to_watchlist", "rate_movie", "browse_genre", "view_recommendations"];

    private static readonly string[] MovieTitles =
        ["The Great Adventure", "Neon Horizons", "Shadow of the Deep", "Quantum Drift", "Crimson Tide", "Stellar Odyssey",
         "The Last Frontier", "Echoes of Tomorrow", "Midnight Protocol", "Velvet Underground"];

    private static readonly string[] Genres =
        ["Action", "Drama", "Sci-Fi", "Comedy", "Thriller", "Horror", "Romance", "Documentary", "Animation", "Fantasy"];

    private static readonly string[] Browsers =
        ["Chrome", "Firefox", "Safari", "Edge", "Brave"];

    private static readonly string[] OperatingSystems =
        ["Windows 11", "macOS 15", "Ubuntu 24.04", "iOS 18", "Android 15"];

    private static readonly string[] DeviceTypes =
        ["Desktop", "Mobile", "Tablet"];

    private static readonly string[] Resolutions =
        ["1920x1080", "2560x1440", "3840x2160", "1366x768", "390x844", "820x1180"];

    private static readonly string[] Languages =
        ["en-US", "en-GB", "es-ES", "fr-FR", "de-DE", "ja-JP", "pt-BR", "ko-KR"];

    private static readonly string[] Referrers =
        ["https://google.com/search?q=movies", "https://bing.com/search?q=streaming", "https://reddit.com/r/movies",
         "https://twitter.com", "https://facebook.com", "(direct)", "https://news.ycombinator.com"];

    private static readonly (double Lat, double Lng, string City, string Region, string Country, string Tz)[] Locations =
    [
        (47.6062, -122.3321, "Seattle", "WA", "US", "America/Los_Angeles"),
        (40.7128, -74.0060, "New York", "NY", "US", "America/New_York"),
        (51.5074, -0.1278, "London", "England", "GB", "Europe/London"),
        (35.6762, 139.6503, "Tokyo", "Tokyo", "JP", "Asia/Tokyo"),
        (48.8566, 2.3522, "Paris", "Île-de-France", "FR", "Europe/Paris"),
        (-33.8688, 151.2093, "Sydney", "NSW", "AU", "Australia/Sydney"),
        (52.5200, 13.4050, "Berlin", "Berlin", "DE", "Europe/Berlin"),
        (37.7749, -122.4194, "San Francisco", "CA", "US", "America/Los_Angeles"),
        (43.6532, -79.3832, "Toronto", "ON", "CA", "America/Toronto"),
        (19.4326, -99.1332, "Mexico City", "CDMX", "MX", "America/Mexico_City")
    ];

    private static readonly string[] ExperimentIds =
        ["exp-ab-homepage-v2", "exp-search-ranking-v3", "exp-rec-engine-v1", "exp-player-ui-v4", "exp-none"];

    private static readonly string[] FeatureFlags =
        ["dark-mode", "new-search", "ai-recommendations", "social-sharing", "watchlist-v2", "live-comments", "hdr-player"];

    private static readonly string[] SearchTypes = ["title", "person", "genre"];

    public static void MapIndexingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/indexing")
            .WithTags("Indexing");

        group.MapPost("/event", async ([FromQuery] string? action, IndexingService service) =>
        {
            var evt = GenerateEvent(action);

            try
            {
                var result = await service.WriteEventAsync(evt);
                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to write event: {ex.Message}", statusCode: 502);
            }
        });
    }

    private static BrowsingEvent GenerateEvent(string? requestedAction)
    {
        var rng = Random.Shared;
        var eventType = requestedAction ?? EventTypes[rng.Next(EventTypes.Length)];
        var location = Locations[rng.Next(Locations.Length)];
        var browser = Browsers[rng.Next(Browsers.Length)];
        var os = OperatingSystems[rng.Next(OperatingSystems.Length)];
        var movieTitle = MovieTitles[rng.Next(MovieTitles.Length)];

        // Pick 2-3 random feature flags
        var flagCount = rng.Next(2, 4);
        var flags = FeatureFlags.OrderBy(_ => rng.Next()).Take(flagCount).ToList();

        var eventData = GenerateEventData(eventType, rng, movieTitle);

        return new BrowsingEvent
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            SessionId = Guid.NewGuid().ToString(),
            UserId = $"user-{rng.Next(1000, 9999):x}",
            EventType = eventType,
            Page = "/modules/indexing",
            EventData = eventData,
            Client = new ClientInfo
            {
                IpAddress = $"{rng.Next(1, 224)}.{rng.Next(0, 256)}.{rng.Next(0, 256)}.{rng.Next(1, 255)}",
                UserAgent = $"Mozilla/5.0 ({os}) AppleWebKit/537.36 (KHTML, like Gecko) {browser}/{rng.Next(100, 130)}.0.{rng.Next(0, 9999)}.{rng.Next(0, 200)}",
                Browser = browser,
                BrowserVersion = $"{rng.Next(100, 130)}.0.{rng.Next(0, 9999)}.{rng.Next(0, 200)}",
                Os = os,
                DeviceType = DeviceTypes[rng.Next(DeviceTypes.Length)],
                ScreenResolution = Resolutions[rng.Next(Resolutions.Length)],
                Language = Languages[rng.Next(Languages.Length)],
                Referrer = Referrers[rng.Next(Referrers.Length)]
            },
            Geo = new GeoInfo
            {
                Latitude = location.Lat + (rng.NextDouble() - 0.5) * 0.1,
                Longitude = location.Lng + (rng.NextDouble() - 0.5) * 0.1,
                City = location.City,
                Region = location.Region,
                Country = location.Country,
                Timezone = location.Tz
            },
            Performance = new PerformanceInfo
            {
                PageLoadMs = rng.Next(150, 2000),
                ApiLatencyMs = rng.Next(20, 500),
                RenderTimeMs = rng.Next(10, 300)
            },
            Context = new ContextInfo
            {
                Platform = "web",
                AppVersion = $"{rng.Next(1, 3)}.{rng.Next(0, 10)}.{rng.Next(0, 20)}",
                ExperimentId = ExperimentIds[rng.Next(ExperimentIds.Length)],
                FeatureFlags = flags
            }
        };
    }

    private static Dictionary<string, object> GenerateEventData(string eventType, Random rng, string movieTitle) => eventType switch
    {
        "search" => new()
        {
            ["query"] = movieTitle.Split(' ')[0].ToLowerInvariant(),
            ["resultCount"] = rng.Next(1, 50),
            ["searchType"] = SearchTypes[rng.Next(SearchTypes.Length)]
        },
        "view_details" => new()
        {
            ["movieId"] = $"mov-{rng.Next(1, 5000)}",
            ["title"] = movieTitle,
            ["genre"] = Genres[rng.Next(Genres.Length)],
            ["year"] = rng.Next(2018, 2027)
        },
        "play_trailer" => new()
        {
            ["movieId"] = $"mov-{rng.Next(1, 5000)}",
            ["title"] = movieTitle,
            ["durationSec"] = rng.Next(30, 180)
        },
        "add_to_watchlist" => new()
        {
            ["movieId"] = $"mov-{rng.Next(1, 5000)}",
            ["title"] = movieTitle
        },
        "rate_movie" => new()
        {
            ["movieId"] = $"mov-{rng.Next(1, 5000)}",
            ["title"] = movieTitle,
            ["rating"] = rng.Next(1, 11)
        },
        "browse_genre" => new()
        {
            ["genre"] = Genres[rng.Next(Genres.Length)],
            ["resultCount"] = rng.Next(5, 100)
        },
        "view_recommendations" => new()
        {
            ["sourceMovieId"] = $"mov-{rng.Next(1, 5000)}",
            ["recommendationCount"] = rng.Next(3, 20),
            ["algorithm"] = rng.Next(2) == 0 ? "collaborative" : "content-based"
        },
        _ => new()
        {
            ["detail"] = eventType
        }
    };
}
