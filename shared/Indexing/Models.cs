namespace LearnCosmosDB.Shared.Indexing;

/// <summary>
/// A browsing analytics event written to each indexing container.
/// </summary>
public class BrowsingEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Page { get; set; } = string.Empty;
    public Dictionary<string, object> EventData { get; set; } = new();
    public ClientInfo Client { get; set; } = new();
    public GeoInfo Geo { get; set; } = new();
    public PerformanceInfo Performance { get; set; } = new();
    public ContextInfo Context { get; set; } = new();
}

public class ClientInfo
{
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
    public string Os { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string ScreenResolution { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Referrer { get; set; } = string.Empty;
}

public class GeoInfo
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Timezone { get; set; } = string.Empty;
}

public class PerformanceInfo
{
    public int PageLoadMs { get; set; }
    public int ApiLatencyMs { get; set; }
    public int RenderTimeMs { get; set; }
}

public class ContextInfo
{
    public string Platform { get; set; } = "web";
    public string AppVersion { get; set; } = string.Empty;
    public string ExperimentId { get; set; } = string.Empty;
    public List<string> FeatureFlags { get; set; } = [];
}

/// <summary>
/// Result of writing an event to a single container.
/// </summary>
public class IndexingWriteResult
{
    public string Container { get; set; } = string.Empty;
    public double RequestCharge { get; set; }
    public double DurationMs { get; set; }
}

/// <summary>
/// Response from the indexing write endpoint.
/// </summary>
public class IndexingResponse
{
    public List<IndexingWriteResult> Results { get; set; } = [];
    public BrowsingEvent? Event { get; set; }
}
