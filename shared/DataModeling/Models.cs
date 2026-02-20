namespace LearnCosmosDB.Shared.DataModeling;

public class Genre
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
}

public class Actor
{
    public string Name { get; set; } = string.Empty;
    public string? OriginalName { get; set; }
    public string Id { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class Director
{
    public string Name { get; set; } = string.Empty;
    public string? OriginalName { get; set; }
    public string Id { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}

public class Role
{
    public string MovieId { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string? MpaaRating { get; set; }
    public string? ReleaseDate { get; set; }
    public int? Year { get; set; }
    public string? PosterUrl { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class Review
{
    public string Id { get; set; } = string.Empty;
    public string? CriticReview { get; set; }
    public double? CriticScore { get; set; }
}

/// <summary>
/// Movie document used across all 4 models.
/// </summary>
public class MediaMovie
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "movie";
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? Description { get; set; }
    public string? MpaaRating { get; set; }
    public string? ReleaseDate { get; set; }
    public int? Year { get; set; }
    public string? PosterUrl { get; set; }
    public List<Genre> Genres { get; set; } = [];
    public List<Actor> Actors { get; set; } = [];
    public List<Director> Directors { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
}

/// <summary>
/// Person document in the Embedded model — full movie data embedded alongside person reference.
/// </summary>
public class MediaEmbeddedPerson
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "person";
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string MovieId { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string? Tagline { get; set; }
    public string? Description { get; set; }
    public string? MpaaRating { get; set; }
    public string? ReleaseDate { get; set; }
    public int? Year { get; set; }
    public string? PosterUrl { get; set; }
    public List<Genre> Genres { get; set; } = [];
    public List<Actor> Actors { get; set; } = [];
    public List<Director> Directors { get; set; } = [];
    public List<Review> Reviews { get; set; } = [];
}

/// <summary>
/// Person document in the Reference model — minimal reference data per movie.
/// </summary>
public class MediaReferencePerson
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "person";
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string MovieId { get; set; } = string.Empty;
    public string MovieTitle { get; set; } = string.Empty;
    public string? MpaaRating { get; set; }
    public string? ReleaseDate { get; set; }
    public int? Year { get; set; }
    public string? PosterUrl { get; set; }
}

/// <summary>
/// Person document in the Hybrid model — single doc per person with a roles array.
/// </summary>
public class MediaHybridPerson
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string OriginalTitle { get; set; } = string.Empty;
    public string Type { get; set; } = "person";
    public List<Role> Roles { get; set; } = [];
}

/// <summary>
/// Diagnostics returned with every API response showing query cost and details.
/// </summary>
public class RequestDiagnostics
{
    public string DataModel { get; set; } = string.Empty;
    public string? QueryText { get; set; }
    public string SubmittedSearchValue { get; set; } = string.Empty;
    public string FormattedSearchValue { get; set; } = string.Empty;
    public string? DocId { get; set; }
    public string RequestCharge { get; set; } = "0";
    public string? ActivityId { get; set; }
    public string QueryType { get; set; } = string.Empty;
}

/// <summary>
/// Wrapper for Data Modeling API responses.
/// </summary>
public class DataModelingResponse
{
    public List<System.Text.Json.JsonElement> MediaResults { get; set; } = [];
    public RequestDiagnostics RequestDiagnostics { get; set; } = new();
}
