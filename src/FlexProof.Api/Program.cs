using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FlexProof.Adapter.Local;
using FlexProof.Api.Contracts;
using FlexProof.ArtifactEngine;
using FlexProof.Crypto;
using FlexProof.Domain;
using FlexProof.Registry;

var builder = WebApplication.CreateBuilder(args);

// Structured logging configuration -- never log sensitive payloads
builder.Logging.AddJsonConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
});

builder.Services.AddOpenApi();

// JSON options: use camelCase and serialize enums as strings
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

// Register crypto services
builder.Services.AddSingleton<IArtifactCommitter, Sha256Committer>();
builder.Services.AddSingleton<IArtifactSigner, EcdsaSigner>();
builder.Services.AddSingleton<IArtifactVerifier, ArtifactVerifier>();

// Register adapter / data source
builder.Services.AddSingleton<InMemoryDataSource>();
builder.Services.AddSingleton<ILocalDataSource>(sp => sp.GetRequiredService<InMemoryDataSource>());
builder.Services.AddSingleton<ClaimInputAggregator>();

// Register artifact engine and registry
builder.Services.AddSingleton<ArtifactFactory>();
builder.Services.AddSingleton<IArtifactRegistry, InMemoryArtifactRegistry>();

// CORS for Angular dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    SeedDemoData(app.Services);
}

app.UseCors();
app.UseDefaultFiles();
app.UseStaticFiles();

var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("FlexProof.Api");

const string IssuerId = "flexproof-local";
const string ActorId = "system";

// ─── Artifact Endpoints ──────────────────────────────────────────

app.MapPost("/api/artifacts/issue", async (
    IssueArtifactRequest req,
    ILocalDataSource dataSource,
    ClaimInputAggregator aggregator,
    ArtifactFactory factory,
    IArtifactRegistry registry) =>
{
    var sw = Stopwatch.StartNew();
    logger.LogInformation("Issuing artifact: ClaimType={ClaimType}, Counterparty={Counterparty}, Period={PeriodStart}..{PeriodEnd}",
        req.ClaimType, req.CounterpartyId, req.PeriodStart, req.PeriodEnd);

    var readings = await dataSource.GetLoadReadingsAsync(req.PeriodStart, req.PeriodEnd);
    var tariffWindows = await dataSource.GetTariffWindowsAsync();

    AggregatedClaimInput input;
    if (req.ClaimType == ClaimType.PeakWindowCompliance)
    {
        input = aggregator.AggregatePeakCompliance(readings, tariffWindows, req.PeriodStart, req.PeriodEnd);
    }
    else
    {
        var lookbackStart = req.LookbackStart ?? req.PeriodStart.AddDays(-30);
        var lookbackReadings = await dataSource.GetLoadReadingsAsync(lookbackStart, req.PeriodStart);
        input = aggregator.AggregateDemandDelta(readings, lookbackReadings, tariffWindows, req.BaselineMode, req.PeriodStart, req.PeriodEnd);
    }

    var rights = new RightsScope
    {
        CounterpartyId = req.CounterpartyId,
        Purpose = req.Purpose,
        ValidFrom = req.RightsValidFrom,
        ValidTo = req.RightsValidTo
    };

    var draft = factory.CreateDraft(input, IssuerId, rights);
    var issued = factory.Issue(draft);
    await registry.StoreAsync(issued, ActorId);

    sw.Stop();
    logger.LogInformation("Artifact issued: ArtifactId={ArtifactId}, ClaimType={ClaimType}, ElapsedMs={ElapsedMs}",
        issued.ArtifactId, issued.Claim.Type, sw.ElapsedMilliseconds);

    return Results.Created($"/api/artifacts/{issued.ArtifactId}", issued);
})
.WithName("IssueArtifact")
.WithTags("Artifacts");

app.MapPost("/api/artifacts/verify", async (
    VerifyArtifactRequest req,
    IArtifactVerifier verifier,
    IArtifactRegistry registry) =>
{
    var sw = Stopwatch.StartNew();
    logger.LogInformation("Verification requested: ArtifactId={ArtifactId}, InlinePayload={HasPayload}",
        req.ArtifactId ?? "(inline)", req.Artifact is not null);

    UsageRightArtifact? artifact = req.Artifact;

    if (artifact is null && !string.IsNullOrEmpty(req.ArtifactId))
    {
        artifact = await registry.GetAsync(req.ArtifactId);
        if (artifact is null)
        {
            logger.LogWarning("Verification failed: artifact not found, ArtifactId={ArtifactId}", req.ArtifactId);
            return Results.NotFound(new { error = "Artifact not found." });
        }
    }

    if (artifact is null)
        return Results.BadRequest(new { error = "Provide either artifactId or artifact payload." });

    var result = verifier.Verify(artifact);
    await registry.RecordVerificationAsync(artifact.ArtifactId, result, ActorId);

    sw.Stop();
    logger.LogInformation("Verification complete: ArtifactId={ArtifactId}, IsValid={IsValid}, Checks={CheckCount}, ElapsedMs={ElapsedMs}",
        artifact.ArtifactId, result.IsValid, result.Checks.Count, sw.ElapsedMilliseconds);

    return Results.Ok(result);
})
.WithName("VerifyArtifact")
.WithTags("Artifacts");

app.MapGet("/api/artifacts/{id}", async (string id, IArtifactRegistry registry) =>
{
    var artifact = await registry.GetAsync(id);
    return artifact is not null ? Results.Ok(artifact) : Results.NotFound();
})
.WithName("GetArtifact")
.WithTags("Artifacts");

app.MapGet("/api/artifacts", async (IArtifactRegistry registry) =>
{
    var artifacts = await registry.ListAsync();
    return Results.Ok(artifacts);
})
.WithName("ListArtifacts")
.WithTags("Artifacts");

app.MapGet("/api/artifacts/{id}/audit", async (string id, IArtifactRegistry registry) =>
{
    var trail = await registry.GetAuditTrailAsync(id);
    return Results.Ok(trail);
})
.WithName("GetAuditTrail")
.WithTags("Artifacts");

app.MapPost("/api/artifacts/revoke", async (
    RevokeArtifactRequest req,
    IArtifactRegistry registry) =>
{
    logger.LogInformation("Revocation requested: ArtifactId={ArtifactId}", req.ArtifactId);

    try
    {
        await registry.RevokeAsync(req.ArtifactId, req.Reason, ActorId);
        logger.LogInformation("Artifact revoked: ArtifactId={ArtifactId}", req.ArtifactId);
        return Results.Ok(new { status = "revoked", artifactId = req.ArtifactId });
    }
    catch (KeyNotFoundException)
    {
        logger.LogWarning("Revocation failed: artifact not found, ArtifactId={ArtifactId}", req.ArtifactId);
        return Results.NotFound(new { error = "Artifact not found." });
    }
})
.WithName("RevokeArtifact")
.WithTags("Artifacts");

app.Run();

// ─── Demo Data Seed ──────────────────────────────────────────────

void SeedDemoData(IServiceProvider services)
{
    var ds = services.GetRequiredService<InMemoryDataSource>();

    // Standard weekday peak tariff windows
    var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
    ds.AddTariffWindows([
        new TariffWindow
        {
            Label = "Peak",
            StartTime = new TimeOnly(7, 0),
            EndTime = new TimeOnly(20, 0),
            ApplicableDays = weekdays
        }
    ]);

    // Generate 30 days of synthetic 15-minute load readings
    var baseDate = new DateTimeOffset(2025, 11, 1, 0, 0, 0, TimeSpan.Zero);
    var rng = new Random(42);
    var readings = new List<LoadReading>();

    for (int day = 0; day < 30; day++)
    {
        for (int interval = 0; interval < 96; interval++) // 96 x 15min = 24h
        {
            var start = baseDate.AddDays(day).AddMinutes(interval * 15);
            var hour = start.Hour;

            // Simulate: higher load during peak hours, with optimization reducing peaks
            double baseLoad = hour is >= 7 and < 20 ? 100 + rng.NextDouble() * 80 : 20 + rng.NextDouble() * 30;

            // Simulate optimization: clip peaks above 150 kW
            var optimizedLoad = Math.Min(baseLoad, 150);

            readings.Add(new LoadReading
            {
                IntervalStart = start,
                IntervalDuration = TimeSpan.FromMinutes(15),
                AverageKw = Math.Round(optimizedLoad, 2),
                EnergyKwh = Math.Round(optimizedLoad * 0.25, 2) // 15 min = 0.25 hours
            });
        }
    }

    ds.AddReadings(readings);
}

// Required for WebApplicationFactory in integration tests
public partial class Program;
