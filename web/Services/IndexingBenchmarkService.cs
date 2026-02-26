namespace LearnCosmosDB.Web.Services;

/// <summary>
/// Tracks cumulative write statistics per indexing container across the session.
/// </summary>
public class IndexingBenchmarkService
{
    private static readonly string[] Containers = ["Default", "Implicit", "Explicit"];

    private readonly Dictionary<string, double> _totalRUs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _writeCounts = new(StringComparer.OrdinalIgnoreCase);

    public int TotalWrites { get; private set; }
    public event Action? OnChange;

    public IReadOnlyList<string> ContainerNames => Containers;

    public double GetTotalRU(string container) =>
        _totalRUs.TryGetValue(container, out var ru) ? ru : 0;

    public int GetWriteCount(string container) =>
        _writeCounts.TryGetValue(container, out var count) ? count : 0;

    public void RecordWrite(string container, double ruCost)
    {
        TotalWrites++;
        _totalRUs.TryGetValue(container, out var existingRU);
        _totalRUs[container] = existingRU + ruCost;

        _writeCounts.TryGetValue(container, out var existingCount);
        _writeCounts[container] = existingCount + 1;

        OnChange?.Invoke();
    }

    public void Clear()
    {
        TotalWrites = 0;
        _totalRUs.Clear();
        _writeCounts.Clear();
        OnChange?.Invoke();
    }
}
