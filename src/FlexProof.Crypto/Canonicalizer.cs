using System.Text;
using System.Text.Json;
using FlexProof.Domain;

namespace FlexProof.Crypto;

/// <summary>
/// Produces a deterministic canonical byte representation of an artifact's claim inputs.
/// Uses sorted-key JSON serialization to ensure reproducibility.
/// </summary>
public static class Canonicalizer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Returns a deterministic UTF-8 byte representation of the fields that define the claim.
    /// Only claim-relevant fields are included (no mutable state like signatures).
    /// </summary>
    public static byte[] Canonicalize(UsageRightArtifact artifact)
    {
        var canonical = new
        {
            artifact.ArtifactId,
            artifact.IssuerId,
            artifact.SchemaVersion,
            PeriodStart = artifact.PeriodStart.ToUniversalTime().ToString("O"),
            PeriodEnd = artifact.PeriodEnd.ToUniversalTime().ToString("O"),
            Claim = new
            {
                Type = artifact.Claim.Type.ToString(),
                artifact.Claim.MetricName,
                artifact.Claim.Value,
                artifact.Claim.Unit,
                artifact.Claim.BaselineRef,
                artifact.Claim.ComputationVersion
            },
            Rights = new
            {
                artifact.Rights.CounterpartyId,
                artifact.Rights.Purpose,
                ValidFrom = artifact.Rights.ValidFrom.ToUniversalTime().ToString("O"),
                ValidTo = artifact.Rights.ValidTo.ToUniversalTime().ToString("O"),
                artifact.Rights.Constraints
            }
        };

        var json = JsonSerializer.Serialize(canonical, Options);
        return Encoding.UTF8.GetBytes(json);
    }
}
