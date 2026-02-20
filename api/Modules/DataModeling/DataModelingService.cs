using System.Text.Json;
using LearnCosmosDB.Shared.DataModeling;
using Microsoft.Azure.Cosmos;

namespace LearnCosmosDB.Api.Modules.DataModeling;

public class DataModelingService(CosmosClient cosmosClient, IConfiguration configuration, ILogger<DataModelingService> logger)
{
    private readonly string _databaseName = configuration["CosmosDB:DatabaseName"] ?? "DataModeling";

    private static readonly IReadOnlySet<string> AllowedModels = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Single", "Embedded", "Reference", "Hybrid"
    };

    public async Task<DataModelingResponse?> QueryAsync(string dataModel, string searchValue, string? searchType, string? docId)
    {
        if (!AllowedModels.Contains(dataModel))
            throw new ArgumentException($"Invalid data model '{dataModel}'. Must be one of: Single, Embedded, Reference, Hybrid.");

        var container = cosmosClient.GetContainer(_databaseName, dataModel);
        var formattedSearch = searchValue.ToLowerInvariant();

        var diagnostics = new RequestDiagnostics
        {
            DataModel = dataModel,
            SubmittedSearchValue = searchValue,
            FormattedSearchValue = formattedSearch
        };

        var response = new DataModelingResponse { RequestDiagnostics = diagnostics };

        // Point Read: both docId and searchValue (partition key) are available
        if (!string.IsNullOrEmpty(docId))
        {
            diagnostics.QueryType = "Point Read";
            diagnostics.DocId = docId;

            try
            {
                using var streamResponse = await container.ReadItemStreamAsync(docId, new PartitionKey(formattedSearch));
                streamResponse.EnsureSuccessStatusCode();
                using var doc = await JsonDocument.ParseAsync(streamResponse.Content);
                response.MediaResults.Add(doc.RootElement.Clone());
                diagnostics.RequestCharge = streamResponse.Headers.RequestCharge.ToString("F2");
                diagnostics.ActivityId = streamResponse.Headers.ActivityId;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                logger.LogWarning("Point read not found: {DocId} / {SearchValue}", docId, formattedSearch);
                return null;
            }
        }
        else
        {
            // SQL Query — use parameterized QueryDefinition to prevent injection
            diagnostics.QueryType = "SQL Query";

            QueryDefinition query;
            string queryText;

            if (searchType == "person" && dataModel.Equals("Single", StringComparison.OrdinalIgnoreCase))
            {
                queryText = "SELECT DISTINCT c.id, c.title, c.originalTitle, c.year, c.posterUrl, c.genres, " +
                            "c.actors, c.directors, c.reviews, c.type, c.tagline, c.description, c.mpaaRating, c.releaseDate " +
                            "FROM c WHERE ARRAY_CONTAINS(c.actors, {\"name\": @search}, true) " +
                            "OR ARRAY_CONTAINS(c.directors, {\"name\": @search}, true)";
                query = new QueryDefinition(queryText).WithParameter("@search", formattedSearch);
            }
            else
            {
                queryText = "SELECT * FROM c WHERE c.title = @title";
                query = new QueryDefinition(queryText).WithParameter("@title", formattedSearch);
            }

            diagnostics.QueryText = queryText;
            logger.LogInformation("Executing query: {QueryText}", queryText);

            double totalCharge = 0;
            using var iterator = container.GetItemQueryStreamIterator(query);

            while (iterator.HasMoreResults)
            {
                using var batch = await iterator.ReadNextAsync();
                totalCharge += batch.Headers.RequestCharge;
                diagnostics.ActivityId = batch.Headers.ActivityId;

                using var doc = await JsonDocument.ParseAsync(batch.Content);
                foreach (var item in doc.RootElement.GetProperty("Documents").EnumerateArray())
                {
                    response.MediaResults.Add(item.Clone());
                }
            }

            if (response.MediaResults.Count == 0)
            {
                logger.LogInformation("No results found for query");
                return null;
            }

            diagnostics.RequestCharge = totalCharge.ToString("F2");
        }

        return response;
    }
}
