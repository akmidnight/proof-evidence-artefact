using System.Security.Cryptography;
using FlexProof.Domain;

namespace FlexProof.Crypto;

/// <summary>
/// Verifies artifact integrity by re-computing the commitment and checking the signature.
/// </summary>
public sealed class ArtifactVerifier : IArtifactVerifier
{
    private readonly IArtifactCommitter _committer;

    public ArtifactVerifier(IArtifactCommitter committer)
    {
        _committer = committer;
    }

    public VerificationResult Verify(UsageRightArtifact artifact)
    {
        var checks = new List<VerificationCheck>();
        var failures = new List<string>();

        // Check 1: artifact must be in Issued state
        var stateCheck = artifact.State == ArtifactState.Issued;
        checks.Add(new VerificationCheck
        {
            CheckName = "StateIsIssued",
            Passed = stateCheck,
            Detail = stateCheck ? null : $"Expected Issued, got {artifact.State}"
        });
        if (!stateCheck) failures.Add("ARTIFACT_NOT_ISSUED");

        // Check 2: commitment must be present and match recomputation
        var commitmentPresent = !string.IsNullOrEmpty(artifact.DataCommitment);
        bool commitmentMatch = false;
        if (commitmentPresent)
        {
            var recomputed = _committer.ComputeCommitment(artifact);
            commitmentMatch = string.Equals(recomputed, artifact.DataCommitment, StringComparison.OrdinalIgnoreCase);
        }
        checks.Add(new VerificationCheck
        {
            CheckName = "CommitmentMatch",
            Passed = commitmentPresent && commitmentMatch,
            Detail = !commitmentPresent ? "No commitment present" :
                     !commitmentMatch ? "Recomputed commitment does not match" : null
        });
        if (!commitmentPresent) failures.Add("COMMITMENT_MISSING");
        else if (!commitmentMatch) failures.Add("COMMITMENT_MISMATCH");

        // Check 3: signature must be present and valid
        var signaturePresent = !string.IsNullOrEmpty(artifact.Signature) &&
                               !string.IsNullOrEmpty(artifact.SignerPublicKey);
        bool signatureValid = false;
        if (signaturePresent)
        {
            try
            {
                var signatureBytes = Convert.FromBase64String(artifact.Signature!);
                var publicKeyBytes = Convert.FromBase64String(artifact.SignerPublicKey!);
                var dataToVerify = Canonicalizer.Canonicalize(artifact);

                using var ecdsa = ECDsa.Create();
                ecdsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
                signatureValid = ecdsa.VerifyData(dataToVerify, signatureBytes, HashAlgorithmName.SHA256);
            }
            catch
            {
                signatureValid = false;
            }
        }
        checks.Add(new VerificationCheck
        {
            CheckName = "SignatureValid",
            Passed = signaturePresent && signatureValid,
            Detail = !signaturePresent ? "No signature or public key present" :
                     !signatureValid ? "Signature verification failed" : null
        });
        if (!signaturePresent) failures.Add("SIGNATURE_MISSING");
        else if (!signatureValid) failures.Add("SIGNATURE_INVALID");

        // Check 4: rights scope must be within valid period
        var now = DateTimeOffset.UtcNow;
        var rightsValid = now >= artifact.Rights.ValidFrom && now <= artifact.Rights.ValidTo;
        checks.Add(new VerificationCheck
        {
            CheckName = "RightsInPeriod",
            Passed = rightsValid,
            Detail = rightsValid ? null : "Current time is outside the rights validity window"
        });
        if (!rightsValid) failures.Add("RIGHTS_EXPIRED");

        return new VerificationResult
        {
            IsValid = failures.Count == 0,
            VerificationId = Guid.NewGuid().ToString("N"),
            ArtifactId = artifact.ArtifactId,
            VerifiedAt = now,
            Checks = checks,
            FailureReasons = failures
        };
    }
}
