using LearnCosmosDB.Api.Modules.DataModeling;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                ?? ["http://localhost:5200", "https://localhost:5200"])
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHealthChecks();

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var endpoint = builder.Configuration["CosmosDB:Endpoint"]
        ?? throw new InvalidOperationException("CosmosDB:Endpoint configuration is required");

    var key = builder.Configuration["CosmosDB:Key"];

    var options = new CosmosClientOptions
    {
        SerializerOptions = new CosmosSerializationOptions
        {
            PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
        },
        ConnectionMode = ConnectionMode.Gateway
    };

    // Only bypass certificate validation for the local Cosmos DB emulator
    if (endpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
        endpoint.Contains("cosmos-emulator", StringComparison.OrdinalIgnoreCase))
    {
        options.ServerCertificateCustomValidationCallback = (_, _, _) => true;
    }

    if (!string.IsNullOrEmpty(key))
    {
        return new CosmosClient(endpoint, key, options);
    }

    return new CosmosClient(endpoint, new Azure.Identity.DefaultAzureCredential(), options);
});

builder.Services.AddScoped<DataModelingService>();

var app = builder.Build();

app.UseCors();
app.MapHealthChecks("/healthz");
app.MapDataModelingEndpoints();

app.Run();
