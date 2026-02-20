using LearnCosmosDB.Processor.Modules.DataModeling;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

Console.WriteLine("LearnCosmosDB Processor");
Console.WriteLine("=======================");

var apiBaseUrl = config["MediaApi:BaseUrl"] ?? "https://api.battlecabbage.com";
var cosmosEndpoint = config["CosmosDB:Endpoint"];
var cosmosKey = config["CosmosDB:Key"];
var cosmosDatabase = config["CosmosDB:DatabaseName"] ?? "DataModeling";

if (string.IsNullOrEmpty(cosmosEndpoint))
{
    Console.Error.WriteLine("Error: CosmosDB:Endpoint is required.");
    return 1;
}

Console.WriteLine($"Media API: {apiBaseUrl}");
Console.WriteLine($"Cosmos DB: {cosmosEndpoint}");
Console.WriteLine($"Database:  {cosmosDatabase}");
Console.WriteLine();

using var httpClient = new HttpClient { BaseAddress = new Uri(apiBaseUrl) };
var apiClient = new BattleCabbageClient(httpClient);

var cosmosOptions = new CosmosClientOptions
{
    SerializerOptions = new CosmosSerializationOptions { PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase },
    ConnectionMode = ConnectionMode.Gateway
};

// Only bypass certificate validation for the local Cosmos DB emulator
if (cosmosEndpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
    cosmosEndpoint.Contains("cosmos-emulator", StringComparison.OrdinalIgnoreCase))
{
    cosmosOptions.ServerCertificateCustomValidationCallback = (_, _, _) => true;
}
using var cosmosClient = string.IsNullOrEmpty(cosmosKey)
    ? new CosmosClient(cosmosEndpoint, new Azure.Identity.DefaultAzureCredential(), cosmosOptions)
    : new CosmosClient(cosmosEndpoint, cosmosKey, cosmosOptions);

var processor = new DataModelingProcessor(apiClient, cosmosClient, cosmosDatabase, apiBaseUrl);

// Wait for Cosmos DB to be reachable (emulator can take time to start)
const int maxRetries = 12;
for (int i = 1; i <= maxRetries; i++)
{
    try
    {
        await cosmosClient.ReadAccountAsync();
        break;
    }
    catch (HttpRequestException) when (i < maxRetries)
    {
        Console.WriteLine($"Waiting for Cosmos DB to be ready... (attempt {i}/{maxRetries})");
        await Task.Delay(5000);
    }
}

await processor.ProcessAsync(movieCount: 5);

return 0;
