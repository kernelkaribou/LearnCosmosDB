using System.Net.Http.Json;

namespace LearnCosmosDB.Processor.Modules.DataModeling;

/// <summary>
/// HTTP client for the Battle Cabbage Media API.
/// </summary>
public class BattleCabbageClient(HttpClient httpClient)
{
    public async Task<List<BattleCabbageMovie>> GetMoviesAsync(int skip = 0, int limit = 5, CancellationToken cancellationToken = default)
    {
        var movies = await httpClient.GetFromJsonAsync<List<BattleCabbageMovie>>(
            $"/movies?skip={skip}&limit={limit}&sort_by=movie_id&order=asc", cancellationToken);
        return movies ?? [];
    }
}
