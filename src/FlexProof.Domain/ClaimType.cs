namespace FlexProof.Domain;

/// <summary>
/// Types of claims an artifact can attest to.
/// Extensible as new pilot scenarios are added.
/// </summary>
public enum ClaimType
{
    /// <summary>
    /// Peak load stayed below a contractual threshold during defined windows.
    /// </summary>
    PeakWindowCompliance,

    /// <summary>
    /// Controlled charging reduced demand charge exposure relative to baseline.
    /// </summary>
    DemandChargeDeltaEstimate
}
