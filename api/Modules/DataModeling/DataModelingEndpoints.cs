using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;

namespace LearnCosmosDB.Api.Modules.DataModeling;

public static class DataModelingEndpoints
{
    public static void MapDataModelingEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/datamodeling")
            .WithTags("DataModeling");

        group.MapGet("/", async (
            [FromQuery] string dataModel,
            [FromQuery] string searchValue,
            [FromQuery] string? searchType,
            [FromQuery] string? docId,
            DataModelingService service) =>
        {
            if (string.IsNullOrWhiteSpace(dataModel))
                return Results.BadRequest("Please provide a valid data model: 'Single','Embedded','Reference','Hybrid'");

            if (string.IsNullOrWhiteSpace(searchValue))
                return Results.BadRequest("Please provide a valid search value");

            try
            {
                var result = await service.QueryAsync(dataModel, searchValue, searchType, docId);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: 502);
            }
        });

        group.MapGet("/hints", async (IHttpClientFactory httpClientFactory, IConfiguration config, DataModelingService service) =>
        {
            var baseUrl = config["MediaApi:BaseUrl"] ?? "https://api.battlecabbage.com";
            using var http = httpClientFactory.CreateClient();

            string? movieTitle = null, actorName = null, topActorName = null;
            Dictionary<string, int>? counts = null;

            try
            {
                var movieTask = http.GetFromJsonAsync<JsonArray>($"{baseUrl}/movies/random");
                var actorTask = http.GetFromJsonAsync<JsonArray>($"{baseUrl}/actors/top");

                await Task.WhenAll(movieTask, actorTask);

                var movies = movieTask.Result;
                var actors = actorTask.Result;

                movieTitle = movies?.FirstOrDefault()?["title"]?.GetValue<string>();
                actorName = movies?.FirstOrDefault()?["actors"]?.AsArray().FirstOrDefault()?["actor"]?.GetValue<string>();
                if (actors is { Count: > 0 })
                    topActorName = actors[Random.Shared.Next(actors.Count)]?["actor"]?.GetValue<string>();
            }
            catch { }

            try
            {
                counts = await service.GetContainerCountsAsync();
            }
            catch { }

            return Results.Ok(new { movieTitle, actorName, topActorName, containerCounts = counts });
        });
    }
}
