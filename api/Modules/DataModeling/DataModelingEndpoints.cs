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
    }
}
