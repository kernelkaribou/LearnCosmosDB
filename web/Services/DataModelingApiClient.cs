using System.Net.Http.Json;
using System.Text.Json;
using LearnCosmosDB.Shared.DataModeling;

namespace LearnCosmosDB.Web.Services;

public class DataModelingApiClient(HttpClient httpClient)
{
    public async Task<DataModelingResponse?> SearchAsync(string dataModel, string searchValue, string? searchType = null, string? docId = null)
    {
        var url = $"/api/datamodeling/?dataModel={Uri.EscapeDataString(dataModel)}&searchValue={Uri.EscapeDataString(searchValue)}";

        if (!string.IsNullOrEmpty(searchType))
            url += $"&searchType={Uri.EscapeDataString(searchType)}";

        if (!string.IsNullOrEmpty(docId))
            url += $"&docId={Uri.EscapeDataString(docId)}";

        try
        {
            var httpResponse = await httpClient.GetAsync(url);

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            httpResponse.EnsureSuccessStatusCode();

            var json = await httpResponse.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var response = new DataModelingResponse();

            if (root.TryGetProperty("mediaResults", out var results))
            {
                foreach (var item in results.EnumerateArray())
                    response.MediaResults.Add(item.Clone());
            }

            if (root.TryGetProperty("requestDiagnostics", out var diag))
            {
                response.RequestDiagnostics = JsonSerializer.Deserialize<RequestDiagnostics>(diag.GetRawText(),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            }

            return response;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
