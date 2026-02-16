using System.Collections.Concurrent;
using FlexProof.Domain;

namespace FlexProof.Registry;

/// <summary>
/// In-memory append-only artifact registry for local deployment and testing.
/// All mutations are append-only -- existing entries are never modified in place.
/// </summary>
public sealed class InMemoryArtifactRegistry : IArtifactRegistry
{
    private readonly ConcurrentDictionary<string, UsageRightArtifact> _artifacts = new();
    private readonly ConcurrentBag<AuditEntry> _auditLog = [];

    public Task StoreAsync(UsageRightArtifact artifact, string actorId, CancellationToken ct = default)
    {
        if (!_artifacts.TryAdd(artifact.ArtifactId, artifact))
            throw new InvalidOperationException($"Artifact {artifact.ArtifactId} already exists.");

        AppendAudit(artifact.ArtifactId, "Issued", actorId, "Artifact stored and issued.");
        return Task.CompletedTask;
    }

    public Task<UsageRightArtifact?> GetAsync(string artifactId, CancellationToken ct = default)
    {
        _artifacts.TryGetValue(artifactId, out var artifact);
        return Task.FromResult(artifact);
    }

    public Task<IReadOnlyList<UsageRightArtifact>> ListAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<UsageRightArtifact>>(_artifacts.Values.ToList());
    }

    public Task RevokeAsync(string artifactId, string reason, string actorId, CancellationToken ct = default)
    {
        if (!_artifacts.TryGetValue(artifactId, out var artifact))
            throw new KeyNotFoundException($"Artifact {artifactId} not found.");

        artifact.State = ArtifactState.Revoked;
        artifact.RevocationRef = reason;
        AppendAudit(artifactId, "Revoked", actorId, reason);
        return Task.CompletedTask;
    }

    public Task SupersedeAsync(string oldArtifactId, UsageRightArtifact replacement, string actorId, CancellationToken ct = default)
    {
        if (!_artifacts.TryGetValue(oldArtifactId, out var old))
            throw new KeyNotFoundException($"Artifact {oldArtifactId} not found.");

        old.State = ArtifactState.Superseded;
        old.SupersededBy = replacement.ArtifactId;

        if (!_artifacts.TryAdd(replacement.ArtifactId, replacement))
            throw new InvalidOperationException($"Replacement artifact {replacement.ArtifactId} already exists.");

        AppendAudit(oldArtifactId, "Superseded", actorId, $"Superseded by {replacement.ArtifactId}");
        AppendAudit(replacement.ArtifactId, "Issued", actorId, $"Supersedes {oldArtifactId}");
        return Task.CompletedTask;
    }

    public Task RecordVerificationAsync(string artifactId, VerificationResult result, string actorId, CancellationToken ct = default)
    {
        var detail = result.IsValid ? "Verification passed" : $"Verification failed: {string.Join(", ", result.FailureReasons)}";
        AppendAudit(artifactId, "Verified", actorId, detail);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<AuditEntry>> GetAuditTrailAsync(string artifactId, CancellationToken ct = default)
    {
        var entries = _auditLog
            .Where(e => e.ArtifactId == artifactId)
            .OrderBy(e => e.Timestamp)
            .ToList();

        return Task.FromResult<IReadOnlyList<AuditEntry>>(entries);
    }

    private void AppendAudit(string artifactId, string eventType, string actorId, string? detail)
    {
        _auditLog.Add(new AuditEntry
        {
            EntryId = Guid.NewGuid().ToString("N"),
            ArtifactId = artifactId,
            Timestamp = DateTimeOffset.UtcNow,
            EventType = eventType,
            ActorId = actorId,
            Detail = detail
        });
    }
}
