using System.Net.Http.Json;

namespace LearnCosmosDB.Processor.Modules.DataModeling;

/// <summary>
/// HTTP client for the Battle Cabbage Media API.
/// </summary>
public class BattleCabbageClient(HttpClient httpClient)
{
    public async Task<List<BattleCabbageMovie>> GetMoviesAsync(int skip = 0, int limit = 50, int? startId = null, CancellationToken cancellationToken = default)
    {
        var url = $"/movies?skip={skip}&limit={limit}&sort_by=movie_id&order=asc";
        if (startId.HasValue)
            url += $"&start_id={startId.Value}";
        var movies = await httpClient.GetFromJsonAsync<List<BattleCabbageMovie>>(url, cancellationToken);
        return movies ?? [];
    }
}
