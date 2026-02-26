using System.Diagnostics;
using LearnCosmosDB.Shared.Indexing;
using Microsoft.Azure.Cosmos;

namespace LearnCosmosDB.Api.Modules.Indexing;

public class IndexingService(CosmosClient cosmosClient, IConfiguration configuration, ILogger<IndexingService> logger)
{
    private readonly string _databaseName = configuration["CosmosDB:IndexingDatabaseName"] ?? "Indexing";

    private static readonly string[] ContainerNames = ["Default", "Implicit", "Explicit"];

    /// <summary>
    /// Creates the Indexing database and containers with the appropriate indexing policies.
    /// Safe to call multiple times — uses IfNotExists semantics.
    /// </summary>
    public async Task EnsureContainersAsync()
    {
        var db = (await cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName)).Database;

        foreach (var name in ContainerNames)
        {
            var props = new ContainerProperties(name, "/sessionId")
            {
                IndexingPolicy = GetIndexingPolicy(name)
            };

            await db.CreateContainerIfNotExistsAsync(props, throughput: 400);
            logger.LogInformation("Ensured container {Container} in database {Database}", name, _databaseName);
        }
    }

    /// <summary>
    /// Writes the same event to all 3 containers and returns per-container RU costs.
    /// </summary>
    public async Task<IndexingResponse> WriteEventAsync(BrowsingEvent evt)
    {
        var tasks = ContainerNames.Select(async name =>
        {
            var container = cosmosClient.GetContainer(_databaseName, name);
            var sw = Stopwatch.StartNew();
            var response = await container.CreateItemAsync(evt, new PartitionKey(evt.SessionId));
            sw.Stop();

            return new IndexingWriteResult
            {
                Container = name,
                RequestCharge = response.RequestCharge,
                DurationMs = sw.Elapsed.TotalMilliseconds
            };
        });

        var results = await Task.WhenAll(tasks);

        return new IndexingResponse
        {
            Results = results.ToList(),
            Event = evt
        };
    }

    private static IndexingPolicy GetIndexingPolicy(string containerName) => containerName switch
    {
        "Default" => new IndexingPolicy
        {
            // Cosmos DB default: index everything
            Automatic = true,
            IndexingMode = IndexingMode.Consistent
        },

        "Implicit" => new IndexingPolicy
        {
            Automatic = true,
            IndexingMode = IndexingMode.Consistent,
            IncludedPaths = { new IncludedPath { Path = "/*" } },
            ExcludedPaths =
            {
                new ExcludedPath { Path = "/client/*" },
                new ExcludedPath { Path = "/geo/*" },
                new ExcludedPath { Path = "/performance/*" },
                new ExcludedPath { Path = "/context/*" }
            }
        },

        "Explicit" => new IndexingPolicy
        {
            Automatic = true,
            IndexingMode = IndexingMode.Consistent,
            IncludedPaths = { new IncludedPath { Path = "/timestamp/?" } },
            ExcludedPaths = { new ExcludedPath { Path = "/*" } }
        },

        _ => throw new ArgumentException($"Unknown container: {containerName}")
    };
}
