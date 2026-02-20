namespace LearnCosmosDB.Web.Services;

/// <summary>
/// Tracks cumulative query statistics across the session.
/// </summary>
public class BenchmarkService
{
    public int SelectQueries { get; private set; }
    public int PointReads { get; private set; }
    public double TotalRUs { get; private set; }

    private static readonly string[] Models = ["Single", "Embedded", "Reference", "Hybrid"];
    private static readonly string[] Actions = ["SQL Query", "Point Read"];

    // model -> action -> cumulative RU
    private readonly Dictionary<string, Dictionary<string, double>> _modelCosts = new(StringComparer.OrdinalIgnoreCase);

    public event Action? OnChange;

    public IReadOnlyList<string> ModelNames => Models;
    public IReadOnlyList<string> ActionNames => Actions;

    public double GetCost(string model, string action)
    {
        if (_modelCosts.TryGetValue(model, out var actions) && actions.TryGetValue(action, out var cost))
            return cost;
        return 0;
    }

    public double GetModelTotal(string model)
    {
        if (!_modelCosts.TryGetValue(model, out var actions)) return 0;
        return actions.Values.Sum();
    }

    public void RecordQuery(string queryType, double requestCharge, string? model = null)
    {
        if (queryType == "Point Read")
            PointReads++;
        else
            SelectQueries++;

        TotalRUs += requestCharge;

        if (!string.IsNullOrEmpty(model))
        {
            if (!_modelCosts.TryGetValue(model, out var actions))
            {
                actions = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                _modelCosts[model] = actions;
            }
            actions.TryGetValue(queryType, out var existing);
            actions[queryType] = existing + requestCharge;
        }

        OnChange?.Invoke();
    }

    public void Clear()
    {
        SelectQueries = 0;
        PointReads = 0;
        TotalRUs = 0;
        _modelCosts.Clear();
        OnChange?.Invoke();
    }

    public void ClearModelCosts()
    {
        _modelCosts.Clear();
        OnChange?.Invoke();
    }
}
