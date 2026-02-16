using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlexProof.Adapter.Local;
using FlexProof.Api.Contracts;
using FlexProof.ArtifactEngine;
using FlexProof.Crypto;
using FlexProof.Domain;
using FlexProof.Registry;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FlexProof.IntegrationTests;

public class ArtifactApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public ArtifactApiTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Issue_PeakCompliance_ReturnsIssuedArtifact()
    {
        var request = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "energy-supplier-01",
            Purpose = "tariff negotiation",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        var response = await _client.PostAsJsonAsync("/api/artifacts/issue", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var artifact = await response.Content.ReadFromJsonAsync<UsageRightArtifact>(JsonOptions);
        Assert.NotNull(artifact);
        Assert.Equal(ArtifactState.Issued, artifact.State);
        Assert.Equal("PeakWindowCompliance", artifact.Claim.Type.ToString());
        Assert.NotNull(artifact.DataCommitment);
        Assert.NotNull(artifact.Signature);
        Assert.NotNull(artifact.SignerPublicKey);
    }

    [Fact]
    public async Task Issue_DemandDelta_ReturnsIssuedArtifact()
    {
        var request = new IssueArtifactRequest
        {
            ClaimType = ClaimType.DemandChargeDeltaEstimate,
            PeriodStart = new DateTimeOffset(2025, 11, 15, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            BaselineMode = BaselineMode.HistoricalLookback,
            LookbackStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            CounterpartyId = "investor-alpha",
            Purpose = "financing due diligence",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        var response = await _client.PostAsJsonAsync("/api/artifacts/issue", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var artifact = await response.Content.ReadFromJsonAsync<UsageRightArtifact>(JsonOptions);
        Assert.NotNull(artifact);
        Assert.Equal(ArtifactState.Issued, artifact.State);
        Assert.Equal("DemandChargeDeltaEstimate", artifact.Claim.Type.ToString());
        Assert.NotNull(artifact.Claim.BaselineRef);
    }

    [Fact]
    public async Task Verify_IssuedArtifact_Passes()
    {
        // First issue an artifact
        var issueReq = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "dso-grid-01",
            Purpose = "congestion compliance",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        var issueResponse = await _client.PostAsJsonAsync("/api/artifacts/issue", issueReq, JsonOptions);
        var artifact = await issueResponse.Content.ReadFromJsonAsync<UsageRightArtifact>(JsonOptions);

        // Now verify it
        var verifyReq = new VerifyArtifactRequest { ArtifactId = artifact!.ArtifactId };
        var verifyResponse = await _client.PostAsJsonAsync("/api/artifacts/verify", verifyReq, JsonOptions);

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var result = await verifyResponse.Content.ReadFromJsonAsync<VerificationResult>(JsonOptions);
        Assert.NotNull(result);
        Assert.True(result.IsValid, $"Verification failed: {string.Join(", ", result.FailureReasons)}");
    }

    [Fact]
    public async Task GetArtifact_ExistingId_ReturnsOk()
    {
        // Issue
        var issueReq = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "test-cp",
            Purpose = "test",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };
        var issueResponse = await _client.PostAsJsonAsync("/api/artifacts/issue", issueReq, JsonOptions);
        var artifact = await issueResponse.Content.ReadFromJsonAsync<UsageRightArtifact>(JsonOptions);

        // Get by ID
        var getResponse = await _client.GetAsync($"/api/artifacts/{artifact!.ArtifactId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetAuditTrail_AfterIssueAndVerify_HasEntries()
    {
        // Issue
        var issueReq = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "audit-test",
            Purpose = "audit test",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };
        var issueResponse = await _client.PostAsJsonAsync("/api/artifacts/issue", issueReq, JsonOptions);
        var artifact = await issueResponse.Content.ReadFromJsonAsync<UsageRightArtifact>(JsonOptions);

        // Verify
        var verifyReq = new VerifyArtifactRequest { ArtifactId = artifact!.ArtifactId };
        await _client.PostAsJsonAsync("/api/artifacts/verify", verifyReq, JsonOptions);

        // Get audit trail
        var auditResponse = await _client.GetAsync($"/api/artifacts/{artifact.ArtifactId}/audit");
        Assert.Equal(HttpStatusCode.OK, auditResponse.StatusCode);

        var entries = await auditResponse.Content.ReadFromJsonAsync<List<AuditEntry>>(JsonOptions);
        Assert.NotNull(entries);
        Assert.True(entries.Count >= 2, "Expected at least Issued + Verified audit entries.");
    }

    [Fact]
    public async Task Revoke_IssuedArtifact_Succeeds()
    {
        // Issue
        var issueReq = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "revoke-test",
            Purpose = "revoke test",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };
        var issueResponse = await _client.PostAsJsonAsync("/api/artifacts/issue", issueReq, JsonOptions);
        var artifact = await issueResponse.Content.ReadFromJsonAsync<UsageRightArtifact>(JsonOptions);

        // Revoke
        var revokeReq = new RevokeArtifactRequest
        {
            ArtifactId = artifact!.ArtifactId,
            Reason = "Data correction needed"
        };
        var revokeResponse = await _client.PostAsJsonAsync("/api/artifacts/revoke", revokeReq, JsonOptions);
        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);
    }

    [Fact]
    public async Task ListArtifacts_ReturnsNonEmpty()
    {
        // Issue at least one
        var issueReq = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "list-test",
            Purpose = "list test",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };
        await _client.PostAsJsonAsync("/api/artifacts/issue", issueReq, JsonOptions);

        var listResponse = await _client.GetAsync("/api/artifacts");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var artifacts = await listResponse.Content.ReadFromJsonAsync<List<UsageRightArtifact>>(JsonOptions);
        Assert.NotNull(artifacts);
        Assert.NotEmpty(artifacts);
    }
}

public class CryptoUnitTests
{
    [Fact]
    public void Sha256Committer_ProducesDeterministicHash()
    {
        var committer = new Sha256Committer();
        var artifact = CreateSampleArtifact();

        var hash1 = committer.ComputeCommitment(artifact);
        var hash2 = committer.ComputeCommitment(artifact);

        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length); // SHA-256 hex = 64 chars
    }

    [Fact]
    public void EcdsaSigner_SignAndVerifyRoundTrip()
    {
        using var signer = new EcdsaSigner();
        var data = System.Text.Encoding.UTF8.GetBytes("test data");

        var signature = signer.Sign(data);
        var pubKey = signer.GetPublicKey();

        Assert.NotEmpty(signature);
        Assert.NotEmpty(pubKey);
    }

    [Fact]
    public void ArtifactVerifier_ValidArtifact_Passes()
    {
        var committer = new Sha256Committer();
        using var signer = new EcdsaSigner();
        var factory = new ArtifactFactory(committer, signer);

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

        var draft = factory.CreateDraft(input, "issuer", rights);
        var issued = factory.Issue(draft);

        var verifier = new ArtifactVerifier(committer);
        var result = verifier.Verify(issued);

        Assert.True(result.IsValid, $"Failed: {string.Join(", ", result.FailureReasons)}");
    }

    [Fact]
    public void ArtifactVerifier_TamperedClaim_Fails()
    {
        var committer = new Sha256Committer();
        using var signer = new EcdsaSigner();
        var factory = new ArtifactFactory(committer, signer);

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

        var draft = factory.CreateDraft(input, "issuer", rights);
        var issued = factory.Issue(draft);

        // Tamper with the claim value after signing
        var tampered = new UsageRightArtifact
        {
            ArtifactId = issued.ArtifactId,
            IssuerId = issued.IssuerId,
            CreatedAt = issued.CreatedAt,
            State = issued.State,
            PeriodStart = issued.PeriodStart,
            PeriodEnd = issued.PeriodEnd,
            Claim = new ClaimValue
            {
                Type = issued.Claim.Type,
                MetricName = issued.Claim.MetricName,
                Value = 99.0, // tampered!
                Unit = issued.Claim.Unit,
                ComputationVersion = issued.Claim.ComputationVersion
            },
            Rights = issued.Rights,
            DataCommitment = issued.DataCommitment,
            Signature = issued.Signature,
            SignerPublicKey = issued.SignerPublicKey
        };

        var verifier = new ArtifactVerifier(committer);
        var result = verifier.Verify(tampered);

        Assert.False(result.IsValid);
        Assert.Contains("COMMITMENT_MISMATCH", result.FailureReasons);
    }

    private static UsageRightArtifact CreateSampleArtifact()
    {
        return new UsageRightArtifact
        {
            ArtifactId = "test-001",
            IssuerId = "issuer-01",
            CreatedAt = new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            Claim = new ClaimValue
            {
                Type = ClaimType.PeakWindowCompliance,
                MetricName = "peak_kw",
                Value = 142.5,
                Unit = "kW",
                ComputationVersion = "1.0.0"
            },
            Rights = new RightsScope
            {
                CounterpartyId = "energy-supplier-01",
                Purpose = "tariff negotiation",
                ValidFrom = new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
                ValidTo = new DateTimeOffset(2026, 12, 1, 0, 0, 0, TimeSpan.Zero)
            }
        };
    }
}
