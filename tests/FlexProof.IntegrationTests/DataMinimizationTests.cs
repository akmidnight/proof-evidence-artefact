using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlexProof.Adapter.Local;
using FlexProof.Api.Contracts;
using FlexProof.Domain;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FlexProof.IntegrationTests;

/// <summary>
/// Enforces the "no raw export" data minimization policy (ADR-0002).
/// Verifies that API endpoints never expose raw session-level data.
/// </summary>
public class DataMinimizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    /// Fields that must NEVER appear in any API response.
    /// These represent raw session-level data that violates the minimization policy.
    /// </summary>
    private static readonly string[] ForbiddenFields =
    [
        "vehicleId", "driverId", "plugId", "sessionId",
        "rawReadings", "sessionLogs", "telemetry",
        "chargingSession", "perPlugData"
    ];

    public DataMinimizationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task IssuedArtifact_ContainsNoRawDataFields()
    {
        var request = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "minimization-test",
            Purpose = "data minimization validation",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        var response = await _client.PostAsJsonAsync("/api/artifacts/issue", request, JsonOptions);
        var body = await response.Content.ReadAsStringAsync();

        foreach (var field in ForbiddenFields)
        {
            Assert.DoesNotContain(field, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task GetArtifact_ContainsNoRawDataFields()
    {
        // Issue an artifact first
        var request = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "minimization-get-test",
            Purpose = "data minimization validation",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        var issueResponse = await _client.PostAsJsonAsync("/api/artifacts/issue", request, JsonOptions);
        var artifact = await issueResponse.Content.ReadFromJsonAsync<UsageRightArtifact>(JsonOptions);

        var getResponse = await _client.GetAsync($"/api/artifacts/{artifact!.ArtifactId}");
        var body = await getResponse.Content.ReadAsStringAsync();

        foreach (var field in ForbiddenFields)
        {
            Assert.DoesNotContain(field, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task ListArtifacts_ContainsNoRawDataFields()
    {
        var response = await _client.GetAsync("/api/artifacts");
        var body = await response.Content.ReadAsStringAsync();

        foreach (var field in ForbiddenFields)
        {
            Assert.DoesNotContain(field, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task VerificationResult_ContainsNoRawDataFields()
    {
        var issueReq = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "minimization-verify-test",
            Purpose = "data minimization validation",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        var issueResponse = await _client.PostAsJsonAsync("/api/artifacts/issue", issueReq, JsonOptions);
        var artifact = await issueResponse.Content.ReadFromJsonAsync<UsageRightArtifact>(JsonOptions);

        var verifyResponse = await _client.PostAsJsonAsync(
            "/api/artifacts/verify",
            new { artifactId = artifact!.ArtifactId },
            JsonOptions);
        var body = await verifyResponse.Content.ReadAsStringAsync();

        foreach (var field in ForbiddenFields)
        {
            Assert.DoesNotContain(field, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task AuditTrail_ContainsNoRawDataFields()
    {
        var issueReq = new IssueArtifactRequest
        {
            ClaimType = ClaimType.PeakWindowCompliance,
            PeriodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero),
            PeriodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero),
            CounterpartyId = "minimization-audit-test",
            Purpose = "data minimization validation",
            RightsValidFrom = DateTimeOffset.UtcNow,
            RightsValidTo = DateTimeOffset.UtcNow.AddYears(1)
        };

        var issueResponse = await _client.PostAsJsonAsync("/api/artifacts/issue", issueReq, JsonOptions);
        var artifact = await issueResponse.Content.ReadFromJsonAsync<UsageRightArtifact>(JsonOptions);

        var auditResponse = await _client.GetAsync($"/api/artifacts/{artifact!.ArtifactId}/audit");
        var body = await auditResponse.Content.ReadAsStringAsync();

        foreach (var field in ForbiddenFields)
        {
            Assert.DoesNotContain(field, body, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void AggregatedClaimInput_DoesNotExposeRawReadings()
    {
        // Verify that AggregatedClaimInput contains only scalar aggregate values,
        // not collections of raw readings.
        var inputType = typeof(AggregatedClaimInput);
        var properties = inputType.GetProperties();

        foreach (var prop in properties)
        {
            // No collection-of-reading properties should exist
            Assert.False(
                prop.PropertyType.IsAssignableTo(typeof(System.Collections.IEnumerable))
                && prop.PropertyType != typeof(string),
                $"AggregatedClaimInput.{prop.Name} is a collection type, " +
                "which violates the data minimization policy.");
        }
    }

    [Fact]
    public void ILocalDataSource_NeverExposesRawSessions()
    {
        // Verify the data source interface only has methods returning aggregated data types
        var sourceType = typeof(ILocalDataSource);
        var methods = sourceType.GetMethods();

        foreach (var method in methods)
        {
            // Method names should not contain "Session", "Raw", "Telemetry"
            Assert.DoesNotContain("Session", method.Name);
            Assert.DoesNotContain("Raw", method.Name);
            Assert.DoesNotContain("Telemetry", method.Name);
        }
    }
}
