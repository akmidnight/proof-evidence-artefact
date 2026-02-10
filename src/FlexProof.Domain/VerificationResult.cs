namespace FlexProof.Domain;

/// <summary>
/// Result of verifying a usage-right artifact.
/// </summary>
public sealed class VerificationResult
{
    /// <summary>Whether verification passed.</summary>
    public required bool IsValid { get; init; }

    /// <summary>Unique identifier of this verification run.</summary>
    public required string VerificationId { get; init; }

    /// <summary>Artifact that was verified.</summary>
    public required string ArtifactId { get; init; }

    /// <summary>When verification was performed.</summary>
    public required DateTimeOffset VerifiedAt { get; init; }

    /// <summary>Individual checks that were executed.</summary>
    public required IReadOnlyList<VerificationCheck> Checks { get; init; }

    /// <summary>Reason codes for any failures.</summary>
    public IReadOnlyList<string> FailureReasons { get; init; } = [];
}

/// <summary>
/// A single check within a verification run.
/// </summary>
public sealed class VerificationCheck
{
    /// <summary>Name of the check (e.g. "CommitmentMatch", "SignatureValid").</summary>
    public required string CheckName { get; init; }

    /// <summary>Whether this check passed.</summary>
    public required bool Passed { get; init; }

    /// <summary>Detail or reason if failed.</summary>
    public string? Detail { get; init; }
}
