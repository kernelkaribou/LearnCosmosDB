using System.Net.Http.Json;
using LearnCosmosDB.Shared.Indexing;

namespace LearnCosmosDB.Web.Services;

public class IndexingApiClient(HttpClient httpClient)
{
    public async Task<IndexingResponse?> SimulateEventAsync(string? action = null)
    {
        try
        {
            var url = "/api/indexing/event";
            if (!string.IsNullOrEmpty(action))
                url += $"?action={Uri.EscapeDataString(action)}";

            var response = await httpClient.PostAsync(url, null);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<IndexingResponse>();
        }
        catch
        {
            return null;
        }
    }
}
