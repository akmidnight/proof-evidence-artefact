using FlexProof.Adapter.Local;
using FlexProof.ArtifactEngine;
using FlexProof.Crypto;
using FlexProof.Domain;

namespace FlexProof.IntegrationTests;

/// <summary>
/// Abuse-case tests validating that the system resists common attack patterns.
/// </summary>
public class ThreatModelTests
{
    private readonly Sha256Committer _committer = new();

    [Fact]
    public void ForgedClaim_CommitmentMismatch_FailsVerification()
    {
        // Attacker creates an artifact and manually sets a commitment
        // that does not match the actual claim content.
        var artifact = CreateIssuedArtifact();
        artifact.DataCommitment = "0000000000000000000000000000000000000000000000000000000000000000";

        var verifier = new ArtifactVerifier(_committer);
        var result = verifier.Verify(artifact);

        Assert.False(result.IsValid);
        Assert.Contains("COMMITMENT_MISMATCH", result.FailureReasons);
    }

    [Fact]
    public void ReplayAttack_DifferentArtifactId_CommitmentMismatch()
    {
        // Attacker copies a valid artifact's commitment/signature to a
        // different artifact with a different ID.
        var original = CreateIssuedArtifact();

        var replay = new UsageRightArtifact
        {
            ArtifactId = Guid.NewGuid().ToString("N"), // different ID
            IssuerId = original.IssuerId,
            CreatedAt = original.CreatedAt,
            State = ArtifactState.Issued,
            PeriodStart = original.PeriodStart,
            PeriodEnd = original.PeriodEnd,
            Claim = original.Claim,
            Rights = original.Rights,
            DataCommitment = original.DataCommitment,
            Signature = original.Signature,
            SignerPublicKey = original.SignerPublicKey
        };

        var verifier = new ArtifactVerifier(_committer);
        var result = verifier.Verify(replay);

        Assert.False(result.IsValid);
        // The commitment won't match because artifactId is part of canonical input
        Assert.Contains("COMMITMENT_MISMATCH", result.FailureReasons);
    }

    [Fact]
    public void KeyMisuse_WrongPublicKey_SignatureInvalid()
    {
        // Artifact signed with one key but presented with a different public key.
        var artifact = CreateIssuedArtifact();

        // Replace the public key with a different one
        using var differentKey = new EcdsaSigner();
        artifact.SignerPublicKey = Convert.ToBase64String(differentKey.GetPublicKey());

        var verifier = new ArtifactVerifier(_committer);
        var result = verifier.Verify(artifact);

        Assert.False(result.IsValid);
        Assert.Contains("SIGNATURE_INVALID", result.FailureReasons);
    }

    [Fact]
    public void MissingCommitment_FailsVerification()
    {
        var artifact = CreateIssuedArtifact();
        artifact.DataCommitment = null;

        var verifier = new ArtifactVerifier(_committer);
        var result = verifier.Verify(artifact);

        Assert.False(result.IsValid);
        Assert.Contains("COMMITMENT_MISSING", result.FailureReasons);
    }

    [Fact]
    public void MissingSignature_FailsVerification()
    {
        var artifact = CreateIssuedArtifact();
        artifact.Signature = null;

        var verifier = new ArtifactVerifier(_committer);
        var result = verifier.Verify(artifact);

        Assert.False(result.IsValid);
        Assert.Contains("SIGNATURE_MISSING", result.FailureReasons);
    }

    [Fact]
    public void ExpiredRights_FailsVerification()
    {
        using var signer = new EcdsaSigner();
        var factory = new ArtifactFactory(_committer, signer);

        var input = new AggregatedClaimInput
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            Value = 100,
            Unit = "kW",
            MetricName = "peak_kw",
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero)
        };

        // Rights that expired in the past
        var rights = new RightsScope
        {
            CounterpartyId = "expired-cp",
            Purpose = "test",
            ValidFrom = DateTimeOffset.UtcNow.AddYears(-2),
            ValidTo = DateTimeOffset.UtcNow.AddYears(-1)
        };

        var draft = factory.CreateDraft(input, "issuer", rights);
        var issued = factory.Issue(draft);

        var verifier = new ArtifactVerifier(_committer);
        var result = verifier.Verify(issued);

        Assert.False(result.IsValid);
        Assert.Contains("RIGHTS_EXPIRED", result.FailureReasons);
    }

    [Fact]
    public void DraftState_FailsVerification()
    {
        // Artifact in draft state should not be accepted
        var artifact = new UsageRightArtifact
        {
            ArtifactId = Guid.NewGuid().ToString("N"),
            IssuerId = "issuer",
            CreatedAt = DateTimeOffset.UtcNow,
            State = ArtifactState.Draft,
            PeriodStart = DateTimeOffset.UtcNow.AddDays(-30),
            PeriodEnd = DateTimeOffset.UtcNow,
            Claim = new ClaimValue
            {
                Type = ClaimType.PeakWindowCompliance,
                MetricName = "peak_kw",
                Value = 100,
                Unit = "kW",
                ComputationVersion = "1.0.0"
            },
            Rights = new RightsScope
            {
                CounterpartyId = "test",
                Purpose = "test",
                ValidFrom = DateTimeOffset.UtcNow.AddDays(-1),
                ValidTo = DateTimeOffset.UtcNow.AddYears(1)
            }
        };

        var verifier = new ArtifactVerifier(_committer);
        var result = verifier.Verify(artifact);

        Assert.False(result.IsValid);
        Assert.Contains("ARTIFACT_NOT_ISSUED", result.FailureReasons);
    }

    private UsageRightArtifact CreateIssuedArtifact()
    {
        using var signer = new EcdsaSigner();
        var factory = new ArtifactFactory(_committer, signer);

        var input = new AggregatedClaimInput
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            Value = 142.5,
            Unit = "kW",
            MetricName = "peak_kw",
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero)
        };

        var rights = new RightsScope
        {
            CounterpartyId = "test",
            Purpose = "test",
            ValidFrom = DateTimeOffset.UtcNow.AddDays(-1),
            ValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        return factory.Issue(factory.CreateDraft(input, "issuer", rights));
    }
}
