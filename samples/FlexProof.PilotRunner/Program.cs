using System.Text.Json;
using System.Text.Json.Serialization;
using FlexProof.Adapter.Local;
using FlexProof.ArtifactEngine;
using FlexProof.Crypto;
using FlexProof.Domain;
using FlexProof.Registry;

// ─── Configuration ──────────────────────────────────────────────

var outputDir = Path.Combine(AppContext.BaseDirectory, "pilot-output");
Directory.CreateDirectory(outputDir);

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = true,
    Converters = { new JsonStringEnumConverter() },
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};

// ─── Services ──────────────────────────────────────────────────

var committer = new Sha256Committer();
using var signer = new EcdsaSigner();
var factory = new ArtifactFactory(committer, signer);
var verifier = new ArtifactVerifier(committer);
var registry = new InMemoryArtifactRegistry();
var aggregator = new ClaimInputAggregator();

// ─── Tariff Windows ─────────────────────────────────────────────

var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
var tariffWindows = new List<TariffWindow>
{
    new() { Label = "Peak", StartTime = new TimeOnly(7, 0), EndTime = new TimeOnly(20, 0), ApplicableDays = weekdays }
};

// ─── Generate Depot Data ───────────────────────────────────────

var depots = new[] {
    (Name: "depot-a", PeakClip: 150.0, BaseLoad: 100.0, Variance: 80.0),
    (Name: "depot-b", PeakClip: 250.0, BaseLoad: 180.0, Variance: 120.0)
};

Console.WriteLine("FlexProof Tariff Pilot Runner");
Console.WriteLine("=============================\n");

var allArtifacts = new List<UsageRightArtifact>();
var allVerifications = new List<VerificationResult>();

var periodStart = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero);
var periodEnd = new DateTimeOffset(2025, 11, 30, 23, 59, 59, TimeSpan.Zero);

var rights = new RightsScope
{
    CounterpartyId = "energy-supplier-01",
    Purpose = "tariff negotiation",
    ValidFrom = DateTimeOffset.UtcNow.AddDays(-1),
    ValidTo = DateTimeOffset.UtcNow.AddYears(1)
};

int artifactNum = 0;

foreach (var depot in depots)
{
    Console.WriteLine($"Processing {depot.Name}...");

    var rng = new Random(depot.Name.GetHashCode());
    var readings = GenerateReadings(periodStart, 30, depot.BaseLoad, depot.Variance, depot.PeakClip, rng);
    var lookbackReadings = GenerateReadings(periodStart.AddDays(-30), 30, depot.BaseLoad, depot.Variance, depot.BaseLoad + depot.Variance, rng);

    // Artifact 1: Peak Window Compliance
    var peakInput = aggregator.AggregatePeakCompliance(readings, tariffWindows, periodStart, periodEnd);
    var peakDraft = factory.CreateDraft(peakInput, "flexproof-pilot", rights);
    var peakIssued = factory.Issue(peakDraft);
    await registry.StoreAsync(peakIssued, "pilot-runner");
    var peakVerification = verifier.Verify(peakIssued);
    await registry.RecordVerificationAsync(peakIssued.ArtifactId, peakVerification, "pilot-runner");

    allArtifacts.Add(peakIssued);
    allVerifications.Add(peakVerification);
    artifactNum++;
    Console.WriteLine($"  [{artifactNum}] Peak Compliance: {peakIssued.Claim.Value} {peakIssued.Claim.Unit} -- {(peakVerification.IsValid ? "VALID" : "INVALID")}");

    // Artifact 2: Demand Charge Delta (Historical Lookback)
    var deltaInput = aggregator.AggregateDemandDelta(readings, lookbackReadings, tariffWindows, BaselineMode.HistoricalLookback, periodStart, periodEnd);
    var deltaDraft = factory.CreateDraft(deltaInput, "flexproof-pilot", rights);
    var deltaIssued = factory.Issue(deltaDraft);
    await registry.StoreAsync(deltaIssued, "pilot-runner");
    var deltaVerification = verifier.Verify(deltaIssued);
    await registry.RecordVerificationAsync(deltaIssued.ArtifactId, deltaVerification, "pilot-runner");

    allArtifacts.Add(deltaIssued);
    allVerifications.Add(deltaVerification);
    artifactNum++;
    Console.WriteLine($"  [{artifactNum}] Demand Delta: {deltaIssued.Claim.Value} {deltaIssued.Claim.Unit} -- {(deltaVerification.IsValid ? "VALID" : "INVALID")}");
}

// Artifact 5: Portfolio-level demand delta across both depots (combined)
var combinedReadings = new List<LoadReading>();
var combinedLookback = new List<LoadReading>();
foreach (var depot in depots)
{
    var rng = new Random(depot.Name.GetHashCode());
    combinedReadings.AddRange(GenerateReadings(periodStart, 30, depot.BaseLoad, depot.Variance, depot.PeakClip, rng));
    combinedLookback.AddRange(GenerateReadings(periodStart.AddDays(-30), 30, depot.BaseLoad, depot.Variance, depot.BaseLoad + depot.Variance, rng));
}
var portfolioInput = aggregator.AggregateDemandDelta(combinedReadings, combinedLookback, tariffWindows, BaselineMode.HistoricalLookback, periodStart, periodEnd);
var portfolioRights = new RightsScope
{
    CounterpartyId = "asset-manager-01",
    Purpose = "portfolio utilization assessment",
    ValidFrom = DateTimeOffset.UtcNow.AddDays(-1),
    ValidTo = DateTimeOffset.UtcNow.AddYears(1)
};
var portfolioDraft = factory.CreateDraft(portfolioInput, "flexproof-pilot", portfolioRights);
var portfolioIssued = factory.Issue(portfolioDraft);
await registry.StoreAsync(portfolioIssued, "pilot-runner");
var portfolioVerification = verifier.Verify(portfolioIssued);
await registry.RecordVerificationAsync(portfolioIssued.ArtifactId, portfolioVerification, "pilot-runner");
allArtifacts.Add(portfolioIssued);
allVerifications.Add(portfolioVerification);
artifactNum++;
Console.WriteLine($"  [{artifactNum}] Portfolio Delta: {portfolioIssued.Claim.Value} {portfolioIssued.Claim.Unit} -- {(portfolioVerification.IsValid ? "VALID" : "INVALID")}");

// ─── Write Acceptance Package ─────────────────────────────────

Console.WriteLine($"\nWriting acceptance package to: {outputDir}\n");

// Write all artifacts
for (int i = 0; i < allArtifacts.Count; i++)
{
    var path = Path.Combine(outputDir, $"artifact-{i + 1}.json");
    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(allArtifacts[i], jsonOptions));
}

// Write verification reports
for (int i = 0; i < allVerifications.Count; i++)
{
    var path = Path.Combine(outputDir, $"verification-{i + 1}.json");
    await File.WriteAllTextAsync(path, JsonSerializer.Serialize(allVerifications[i], jsonOptions));
}

// Write audit trail
var allAuditEntries = new List<AuditEntry>();
foreach (var a in allArtifacts)
{
    var trail = await registry.GetAuditTrailAsync(a.ArtifactId);
    allAuditEntries.AddRange(trail);
}
await File.WriteAllTextAsync(
    Path.Combine(outputDir, "audit-trail.json"),
    JsonSerializer.Serialize(allAuditEntries.OrderBy(e => e.Timestamp).ToList(), jsonOptions));

// Write summary
var summary = new
{
    Title = "FlexProof Tariff Pilot - Counterparty Acceptance Packet",
    GeneratedAt = DateTimeOffset.UtcNow,
    TotalArtifacts = allArtifacts.Count,
    AllValid = allVerifications.All(v => v.IsValid),
    Artifacts = allArtifacts.Select(a => new
    {
        a.ArtifactId,
        a.Claim.Type,
        a.Claim.Value,
        a.Claim.Unit,
        Counterparty = a.Rights.CounterpartyId,
        a.Rights.Purpose
    }),
    Methodology = "See docs/specs/artifact-types/ for claim computation methodology.",
    RawDataTransferred = false,
    DataMinimizationEnforced = true
};
await File.WriteAllTextAsync(
    Path.Combine(outputDir, "acceptance-summary.json"),
    JsonSerializer.Serialize(summary, jsonOptions));

Console.WriteLine($"Pilot complete. {allArtifacts.Count} artifacts generated. All valid: {allVerifications.All(v => v.IsValid)}");

// ─── Data Generation Helpers ──────────────────────────────────

static List<LoadReading> GenerateReadings(
    DateTimeOffset baseDate, int days, double baseLoad, double variance, double peakClip, Random rng)
{
    var readings = new List<LoadReading>();
    for (int day = 0; day < days; day++)
    {
        for (int interval = 0; interval < 96; interval++)
        {
            var start = baseDate.AddDays(day).AddMinutes(interval * 15);
            var hour = start.Hour;
            double load = hour is >= 7 and < 20
                ? baseLoad + rng.NextDouble() * variance
                : 20 + rng.NextDouble() * 30;
            load = Math.Min(load, peakClip);
            readings.Add(new LoadReading
            {
                IntervalStart = start,
                IntervalDuration = TimeSpan.FromMinutes(15),
                AverageKw = Math.Round(load, 2),
                EnergyKwh = Math.Round(load * 0.25, 2)
            });
        }
    }
    return readings;
}
