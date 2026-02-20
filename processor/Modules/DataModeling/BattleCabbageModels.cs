using System.Text.Json.Serialization;

namespace LearnCosmosDB.Processor.Modules.DataModeling;

/// <summary>
/// DTOs matching the Battle Cabbage Media API response shapes.
/// </summary>
public class BattleCabbageMovie
{
    [JsonPropertyName("movie_id")]
    public int? MovieId { get; set; }

    [JsonPropertyName("external_id")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("tagline")]
    public string? Tagline { get; set; }

    [JsonPropertyName("mpaa_rating")]
    public string? MpaaRating { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("popularity_score")]
    public double? PopularityScore { get; set; }

    [JsonPropertyName("genre")]
    public string? Genre { get; set; }

    [JsonPropertyName("poster_url")]
    public string? PosterUrl { get; set; }

    [JsonPropertyName("release_date")]
    public string? ReleaseDate { get; set; }

    [JsonPropertyName("actors")]
    public List<BattleCabbageActor> Actors { get; set; } = [];

    [JsonPropertyName("directors")]
    public List<BattleCabbageDirector> Directors { get; set; } = [];

    [JsonPropertyName("reviews")]
    public List<BattleCabbageReview> Reviews { get; set; } = [];
}

public class BattleCabbageActor
{
    [JsonPropertyName("actor_id")]
    public int ActorId { get; set; }

    [JsonPropertyName("actor")]
    public string Actor { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
}

public class BattleCabbageDirector
{
    [JsonPropertyName("director_id")]
    public int DirectorId { get; set; }

    [JsonPropertyName("director")]
    public string Director { get; set; } = string.Empty;

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }
}

public class BattleCabbageReview
{
    [JsonPropertyName("critic_review_id")]
    public int? CriticReviewId { get; set; }

    [JsonPropertyName("critic_review")]
    public string? CriticReview { get; set; }

    [JsonPropertyName("critic_score")]
    public double? CriticScore { get; set; }
}
