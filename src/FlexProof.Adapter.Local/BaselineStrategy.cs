namespace FlexProof.Adapter.Local;

/// <summary>
/// Identifies the baseline computation strategy.
/// </summary>
public enum BaselineMode
{
    /// <summary>Average peak demand over a lookback period before optimization.</summary>
    HistoricalLookback,

    /// <summary>Counterfactual model: estimated uncontrolled demand.</summary>
    CounterfactualModel
}
