using FlexProof.Adapter.Local;
using FlexProof.Crypto;
using FlexProof.Domain;

namespace FlexProof.ArtifactEngine;

/// <summary>
/// Creates, commits, and signs usage-right artifacts from aggregated claim inputs.
/// </summary>
public sealed class ArtifactFactory
{
    private readonly IArtifactCommitter _committer;
    private readonly IArtifactSigner _signer;

    public ArtifactFactory(IArtifactCommitter committer, IArtifactSigner signer)
    {
        _committer = committer;
        _signer = signer;
    }

    /// <summary>
    /// Creates a draft artifact from an aggregated claim input and rights scope.
    /// </summary>
    public UsageRightArtifact CreateDraft(
        AggregatedClaimInput input,
        string issuerId,
        RightsScope rights)
    {
        return new UsageRightArtifact
        {
            ArtifactId = Guid.NewGuid().ToString("N"),
            IssuerId = issuerId,
            CreatedAt = DateTimeOffset.UtcNow,
            State = ArtifactState.Draft,
            PeriodStart = input.PeriodStart,
            PeriodEnd = input.PeriodEnd,
            Claim = new ClaimValue
            {
                Type = input.ClaimType,
                MetricName = input.MetricName,
                Value = input.Value,
                Unit = input.Unit,
                BaselineRef = input.BaselineRef,
                ComputationVersion = "1.0.0"
            },
            Rights = rights
        };
    }

    /// <summary>
    /// Commits and signs a draft artifact, transitioning it to Issued state.
    /// </summary>
    public UsageRightArtifact Issue(UsageRightArtifact draft)
    {
        if (draft.State != ArtifactState.Draft)
            throw new InvalidOperationException($"Cannot issue artifact in {draft.State} state.");

        // Compute commitment
        draft.DataCommitment = _committer.ComputeCommitment(draft);

        // Sign the canonical representation
        var canonical = Canonicalizer.Canonicalize(draft);
        var signature = _signer.Sign(canonical);
        draft.Signature = Convert.ToBase64String(signature);
        draft.SignerPublicKey = Convert.ToBase64String(_signer.GetPublicKey());

        // Transition state
        draft.State = ArtifactState.Issued;

        return draft;
    }
}
