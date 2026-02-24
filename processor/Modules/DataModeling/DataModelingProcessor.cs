using LearnCosmosDB.Shared.DataModeling;
using Microsoft.Azure.Cosmos;

namespace LearnCosmosDB.Processor.Modules.DataModeling;

/// <summary>
/// Data Modeling processor module.
/// Reads movie data from the Battle Cabbage Media API and builds the 4 Cosmos document models
/// (Single, Embedded, Reference, Hybrid), creating the database and containers as needed.
/// </summary>
public class DataModelingProcessor(BattleCabbageClient apiClient, CosmosClient cosmosClient, string databaseName, string apiBaseUrl)
{
    private static readonly string[] ModelNames = ["Single", "Embedded", "Reference", "Hybrid"];

    public async Task ProcessAsync(int movieCount = 50, CancellationToken cancellationToken = default)
    {
        const int batchSize = 50;

        // Create database and containers
        var db = (await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken: cancellationToken)).Database;
        Console.WriteLine($"[DataModeling] Database '{databaseName}' ready.");

        foreach (var model in ModelNames)
        {
            await db.CreateContainerIfNotExistsAsync(model, "/title", cancellationToken: cancellationToken);
            Console.WriteLine($"[DataModeling] Container '{model}' ready.");
        }

        // Find the highest movie_id already stored in the Single container
        var maxApiId = await GetMaxApiIdAsync(db, cancellationToken);
        if (maxApiId.HasValue)
            Console.WriteLine($"[DataModeling] Highest apiId in Cosmos: {maxApiId.Value}. Fetching newer movies...");
        else
            Console.WriteLine("[DataModeling] No existing movies in Cosmos. Fetching all movies...");

        int totalProcessed = 0;
        while (true)
        {
            var movies = maxApiId.HasValue
                ? await apiClient.GetMoviesAsync(skip: 1, limit: batchSize, startId: maxApiId.Value, cancellationToken: cancellationToken)
                : await apiClient.GetMoviesAsync(skip: 0, limit: batchSize, cancellationToken: cancellationToken);

            if (movies.Count == 0)
                break;

            Console.WriteLine($"[DataModeling] Received batch of {movies.Count} movies.");

            // Seed each model
            await SeedSingleModelAsync(db, movies, cancellationToken);
            await SeedEmbeddedModelAsync(db, movies, cancellationToken);
            await SeedReferenceModelAsync(db, movies, cancellationToken);
            await SeedHybridModelAsync(db, movies, cancellationToken);

            totalProcessed += movies.Count;

            // Update maxApiId to the last movie in this batch for the next iteration
            var lastApiId = movies.Max(m => m.MovieId);
            if (lastApiId.HasValue)
                maxApiId = lastApiId.Value;

            if (movies.Count < batchSize)
                break;
        }

        Console.WriteLine($"[DataModeling] Processing complete. {totalProcessed} new movies processed.");
    }

    private async Task<int?> GetMaxApiIdAsync(Database db, CancellationToken ct)
    {
        var container = db.GetContainer("Single");
        var query = new QueryDefinition("SELECT VALUE MAX(c.apiId) FROM c WHERE c.type = 'movie'");
        using var iterator = container.GetItemQueryIterator<int?>(query);
        if (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync(ct);
            return response.FirstOrDefault();
        }
        return null;
    }

    /// <summary>
    /// Single model: one movie document per movie with actors/directors embedded.
    /// </summary>
    private async Task SeedSingleModelAsync(Database db, List<BattleCabbageMovie> movies, CancellationToken ct)
    {
        var container = db.GetContainer("Single");
        int count = 0;

        foreach (var movie in movies)
        {
            var doc = MapToMediaMovie(movie, lowercasePersonNames: true);
            await container.UpsertItemAsync(doc, new PartitionKey(doc.Title), cancellationToken: ct);
            count++;
        }

        Console.WriteLine($"[DataModeling] Single: upserted {count} movie documents.");
    }

    /// <summary>
    /// Embedded model: movie documents + person documents with full movie data embedded.
    /// </summary>
    private async Task SeedEmbeddedModelAsync(Database db, List<BattleCabbageMovie> movies, CancellationToken ct)
    {
        var container = db.GetContainer("Embedded");
        int movieCount = 0, personCount = 0;

        foreach (var movie in movies)
        {
            var movieDoc = MapToMediaMovie(movie, lowercasePersonNames: true);
            await container.UpsertItemAsync(movieDoc, new PartitionKey(movieDoc.Title), cancellationToken: ct);
            movieCount++;

            foreach (var actor in movie.Actors)
            {
                var personDoc = MapToEmbeddedPerson(movie, actor.ActorId.ToString(), actor.Actor, "act");
                await container.UpsertItemAsync(personDoc, new PartitionKey(personDoc.Title), cancellationToken: ct);
                personCount++;
            }

            foreach (var director in movie.Directors)
            {
                var personDoc = MapToEmbeddedPerson(movie, director.DirectorId.ToString(), director.Director, "dir");
                await container.UpsertItemAsync(personDoc, new PartitionKey(personDoc.Title), cancellationToken: ct);
                personCount++;
            }
        }

        Console.WriteLine($"[DataModeling] Embedded: upserted {movieCount} movie + {personCount} person documents.");
    }

    /// <summary>
    /// Reference model: movie documents + person documents with minimal reference data.
    /// </summary>
    private async Task SeedReferenceModelAsync(Database db, List<BattleCabbageMovie> movies, CancellationToken ct)
    {
        var container = db.GetContainer("Reference");
        int movieCount = 0, personCount = 0;

        foreach (var movie in movies)
        {
            var movieDoc = MapToMediaMovie(movie, lowercasePersonNames: true);
            await container.UpsertItemAsync(movieDoc, new PartitionKey(movieDoc.Title), cancellationToken: ct);
            movieCount++;

            foreach (var actor in movie.Actors)
            {
                var personDoc = MapToReferencePerson(movie, actor.ActorId.ToString(), actor.Actor, "act");
                await container.UpsertItemAsync(personDoc, new PartitionKey(personDoc.Title), cancellationToken: ct);
                personCount++;
            }

            foreach (var director in movie.Directors)
            {
                var personDoc = MapToReferencePerson(movie, director.DirectorId.ToString(), director.Director, "dir");
                await container.UpsertItemAsync(personDoc, new PartitionKey(personDoc.Title), cancellationToken: ct);
                personCount++;
            }
        }

        Console.WriteLine($"[DataModeling] Reference: upserted {movieCount} movie + {personCount} person documents.");
    }

    /// <summary>
    /// Hybrid model: movie documents + one person document per unique person with a roles array.
    /// </summary>
    private async Task SeedHybridModelAsync(Database db, List<BattleCabbageMovie> movies, CancellationToken ct)
    {
        var container = db.GetContainer("Hybrid");
        int movieCount = 0;

        // Collect roles per person across all movies
        var personRoles = new Dictionary<string, MediaHybridPerson>();

        foreach (var movie in movies)
        {
            var movieDoc = MapToMediaMovie(movie, lowercasePersonNames: true);
            await container.UpsertItemAsync(movieDoc, new PartitionKey(movieDoc.Title), cancellationToken: ct);
            movieCount++;

            int? movieYear = null;
            if (DateOnly.TryParse(movie.ReleaseDate, out var movieDate))
                movieYear = movieDate.Year;

            foreach (var actor in movie.Actors)
            {
                var key = $"act{actor.ActorId}";
                if (!personRoles.TryGetValue(key, out var person))
                {
                    person = new MediaHybridPerson
                    {
                        Id = key,
                        Title = actor.Actor.ToLowerInvariant(),
                        OriginalTitle = actor.Actor,
                        Type = "person"
                    };
                    personRoles[key] = person;
                }
                person.Roles.Add(new Role
                {
                    MovieId = movie.ExternalId,
                    MovieTitle = movie.Title,
                    MpaaRating = movie.MpaaRating,
                    ReleaseDate = movie.ReleaseDate,
                    Year = movieYear,
                    PosterUrl = FullPosterUrl(movie.PosterUrl),
                    RoleName = "Actor"
                });
            }

            foreach (var director in movie.Directors)
            {
                var key = $"dir{director.DirectorId}";
                if (!personRoles.TryGetValue(key, out var person))
                {
                    person = new MediaHybridPerson
                    {
                        Id = key,
                        Title = director.Director.ToLowerInvariant(),
                        OriginalTitle = director.Director,
                        Type = "person"
                    };
                    personRoles[key] = person;
                }
                person.Roles.Add(new Role
                {
                    MovieId = movie.ExternalId,
                    MovieTitle = movie.Title,
                    MpaaRating = movie.MpaaRating,
                    ReleaseDate = movie.ReleaseDate,
                    Year = movieYear,
                    PosterUrl = FullPosterUrl(movie.PosterUrl),
                    RoleName = "Director"
                });
            }
        }

        foreach (var person in personRoles.Values)
        {
            // Point read to check for existing person document and merge roles
            try
            {
                var existing = await container.ReadItemAsync<MediaHybridPerson>(
                    person.Id, new PartitionKey(person.Title), cancellationToken: ct);

                var existingMovieIds = existing.Resource.Roles.Select(r => r.MovieId).ToHashSet();
                foreach (var role in person.Roles)
                {
                    if (!existingMovieIds.Contains(role.MovieId))
                        existing.Resource.Roles.Add(role);
                }

                await container.UpsertItemAsync(existing.Resource, new PartitionKey(person.Title), cancellationToken: ct);
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                await container.UpsertItemAsync(person, new PartitionKey(person.Title), cancellationToken: ct);
            }
        }

        Console.WriteLine($"[DataModeling] Hybrid: upserted {movieCount} movie + {personRoles.Count} person documents.");
    }

    private MediaMovie MapToMediaMovie(BattleCabbageMovie src, bool lowercasePersonNames = false)
    {
        int? year = null;
        if (DateOnly.TryParse(src.ReleaseDate, out var date))
            year = date.Year;

        return new MediaMovie
        {
            Id = src.ExternalId,
            ApiId = src.MovieId,
            Type = "movie",
            Title = src.Title.ToLowerInvariant(),
            OriginalTitle = src.Title,
            Tagline = src.Tagline,
            Description = src.Description,
            MpaaRating = src.MpaaRating,
            ReleaseDate = src.ReleaseDate,
            Year = year,
            PosterUrl = FullPosterUrl(src.PosterUrl),
            Genres = string.IsNullOrEmpty(src.Genre)
                ? []
                : [new Genre { Id = src.Genre.ToLowerInvariant(), Name = src.Genre }],
            Actors = src.Actors.Select(a => new Actor
            {
                Id = a.ActorId.ToString(),
                Name = lowercasePersonNames ? a.Actor.ToLowerInvariant() : a.Actor,
                OriginalName = lowercasePersonNames ? a.Actor : null,
                ImageUrl = FullPosterUrl(a.ImageUrl)
            }).ToList(),
            Directors = src.Directors.Select(d => new Director
            {
                Id = d.DirectorId.ToString(),
                Name = lowercasePersonNames ? d.Director.ToLowerInvariant() : d.Director,
                OriginalName = lowercasePersonNames ? d.Director : null,
                ImageUrl = FullPosterUrl(d.ImageUrl)
            }).ToList(),
            Reviews = src.Reviews.Select(r => new Review
            {
                Id = r.CriticReviewId?.ToString() ?? "",
                CriticReview = r.CriticReview,
                CriticScore = r.CriticScore
            }).ToList()
        };
    }

    private MediaEmbeddedPerson MapToEmbeddedPerson(BattleCabbageMovie movie, string personId, string personName, string personType)
    {
        int? year = null;
        if (DateOnly.TryParse(movie.ReleaseDate, out var date))
            year = date.Year;

        return new MediaEmbeddedPerson
        {
            Id = $"mov{movie.ExternalId}{personType}{personId}",
            Type = "person",
            Title = personName.ToLowerInvariant(),
            OriginalTitle = personName,
            MovieId = movie.ExternalId,
            MovieTitle = movie.Title,
            Tagline = movie.Tagline,
            Description = movie.Description,
            MpaaRating = movie.MpaaRating,
            ReleaseDate = movie.ReleaseDate,
            Year = year,
            PosterUrl = FullPosterUrl(movie.PosterUrl),
            Genres = string.IsNullOrEmpty(movie.Genre)
                ? []
                : [new Genre { Id = movie.Genre.ToLowerInvariant(), Name = movie.Genre }],
            Actors = movie.Actors.Select(a => new Actor { Id = a.ActorId.ToString(), Name = a.Actor, ImageUrl = FullPosterUrl(a.ImageUrl) }).ToList(),
            Directors = movie.Directors.Select(d => new Director { Id = d.DirectorId.ToString(), Name = d.Director, ImageUrl = FullPosterUrl(d.ImageUrl) }).ToList(),
            Reviews = movie.Reviews.Select(r => new Review
            {
                Id = r.CriticReviewId?.ToString() ?? "",
                CriticReview = r.CriticReview,
                CriticScore = r.CriticScore
            }).ToList()
        };
    }

    private MediaReferencePerson MapToReferencePerson(BattleCabbageMovie movie, string personId, string personName, string personType)
    {
        int? year = null;
        if (DateOnly.TryParse(movie.ReleaseDate, out var date))
            year = date.Year;

        return new MediaReferencePerson
        {
            Id = $"mov{movie.ExternalId}{personType}{personId}",
            Type = "person",
            Title = personName.ToLowerInvariant(),
            OriginalTitle = personName,
            MovieId = movie.ExternalId,
            MovieTitle = movie.Title,
            MpaaRating = movie.MpaaRating,
            ReleaseDate = movie.ReleaseDate,
            Year = year,
            PosterUrl = FullPosterUrl(movie.PosterUrl)
        };
    }

    private string? FullPosterUrl(string? path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return path;
        return apiBaseUrl.TrimEnd('/') + "/" + path.TrimStart('/');
    }
}
