using System;
using NexusScholar.Kernel;

namespace NexusScholar.Artifacts;

public sealed class ArtifactTag
{
}

public sealed record ArtifactDescriptor
{
    private const string InvalidArtifactPathCategory = "invalid-artifact-path";

    private static readonly char UriSeparator = ':';

    public static ArtifactDescriptor Create(
        IIdGenerator ids,
        string artifactRef,
        string logicalPath,
        string artifactKind,
        string mediaType,
        long sizeBytes,
        ContentDigest rawByteDigest,
        string schemaId,
        string schemaVersion,
        ContentDigest? sourceRecordDigest = null,
        string? producedByWorkflowNode = null,
        string? provenanceEventId = null,
        ContentDigest? provenanceEventDigest = null,
        string? requiredFor = null)
    {
        return new ArtifactDescriptor(
            EntityId<ArtifactTag>.New(ids),
            NormalizeArtifactRef(artifactRef),
            NormalizeLogicalPath(logicalPath),
            Guard.NotBlank(artifactKind, nameof(artifactKind)),
            Guard.NotBlank(mediaType, nameof(mediaType)),
            sizeBytes,
            rawByteDigest,
            Guard.NotBlank(schemaId, nameof(schemaId)),
            Guard.NotBlank(schemaVersion, nameof(schemaVersion)),
            sourceRecordDigest,
            NormalizeOptional(producedByWorkflowNode),
            NormalizeOptional(provenanceEventId),
            provenanceEventDigest,
            NormalizeOptional(requiredFor));
    }

    public static ContentDigest ComputeRawByteDigest(ReadOnlySpan<byte> contentBytes)
    {
        return ContentDigest.Sha256(contentBytes);
    }

    public static string NormalizeArtifactRef(string value) => Guard.NotBlank(value, nameof(value));

    public static string NormalizeLogicalPath(string value)
    {
        value = Guard.NotBlank(value, nameof(value)).Trim();
        ValidateLogicalPathOrThrow(value);
        return value;
    }

    public static bool TryValidateLogicalPath(string value, out string? reason)
    {
        reason = null;

        if (value is null)
        {
            reason = "Logical path cannot be null.";
            return false;
        }

        var normalized = value.Trim();
        if (normalized.Length == 0)
        {
            reason = "Logical path cannot be blank.";
            return false;
        }

        if (normalized.IndexOf('\\') >= 0)
        {
            reason = "Logical path cannot contain backslash.";
            return false;
        }

        if (normalized.StartsWith("/", StringComparison.Ordinal) ||
            normalized.EndsWith("/", StringComparison.Ordinal))
        {
            reason = "Logical path cannot start or end with '/'.";
            return false;
        }

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
            uri is not null &&
            !string.IsNullOrWhiteSpace(uri.Scheme))
        {
            reason = "Logical path cannot be a URI.";
            return false;
        }

        if (UriSeparator is ':')
        {
            var isDrivePath = normalized.Length >= 3 &&
                char.IsLetter(normalized[0]) &&
                normalized[1] == UriSeparator &&
                (normalized[2] == '/' || normalized[2] == '\\');
            if (isDrivePath)
            {
                reason = "Logical path cannot be a drive-letter path.";
                return false;
            }
        }

        var segments = normalized.Split('/');
        foreach (var segment in segments)
        {
            if (segment.Length == 0)
            {
                reason = "Logical path cannot contain empty segments.";
                return false;
            }

            if (segment == "." || segment == "..")
            {
                reason = "Logical path cannot contain '.' or '..' segments.";
                return false;
            }
        }

        return true;
    }

    public static void ValidateLogicalPathOrThrow(string value)
    {
        if (!TryValidateLogicalPath(value, out var reason))
        {
            throw new ArgumentException(
                reason ?? InvalidArtifactPathCategory,
                nameof(value));
        }
    }

    public static ArtifactDescriptor Create(
        IIdGenerator ids,
        string mediaType,
        long sizeBytes,
        ContentDigest digest,
        string logicalName)
    {
        return Create(
            ids,
            artifactRef: logicalName,
            logicalPath: logicalName,
            artifactKind: "artifact",
            mediaType: mediaType,
            sizeBytes: sizeBytes,
            rawByteDigest: digest,
            schemaId: "nexus.legacy-artifact",
            schemaVersion: "1.0.0",
            sourceRecordDigest: null);
    }

    public ArtifactDescriptor(
        EntityId<ArtifactTag> id,
        string artifactRef,
        string logicalPath,
        string artifactKind,
        string mediaType,
        long sizeBytes,
        ContentDigest rawByteDigest,
        string schemaId,
        string schemaVersion,
        ContentDigest? sourceRecordDigest = null,
        string? producedByWorkflowNode = null,
        string? provenanceEventId = null,
        ContentDigest? provenanceEventDigest = null,
        string? requiredFor = null)
    {
        Id = id;
        ArtifactRef = Guard.NotBlank(artifactRef, nameof(artifactRef));
        LogicalPath = Guard.NotBlank(logicalPath, nameof(logicalPath));
        ArtifactKind = Guard.NotBlank(artifactKind, nameof(artifactKind));
        MediaType = Guard.NotBlank(mediaType, nameof(mediaType));
        SizeBytes = sizeBytes;
        RawByteDigest = rawByteDigest;
        SchemaId = Guard.NotBlank(schemaId, nameof(schemaId));
        SchemaVersion = Guard.NotBlank(schemaVersion, nameof(schemaVersion));
        SourceRecordDigest = sourceRecordDigest;
        ProducedByWorkflowNode = NormalizeOptional(producedByWorkflowNode);
        ProvenanceEventId = NormalizeOptional(provenanceEventId);
        ProvenanceEventDigest = provenanceEventDigest;
        RequiredFor = NormalizeOptional(requiredFor);

        if (string.IsNullOrWhiteSpace(ArtifactRef))
        {
            throw new ArgumentException("Artifact reference cannot be blank.", nameof(artifactRef));
        }

        if (string.IsNullOrWhiteSpace(LogicalPath))
        {
            throw new ArgumentException("Logical path cannot be blank.", nameof(logicalPath));
        }

        NormalizeLogicalPath(logicalPath);
        if (!TryValidateLogicalPath(LogicalPath, out _))
        {
            throw new ArgumentException(InvalidArtifactPathCategory, nameof(logicalPath));
        }
        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size bytes must be non-negative.");
        }

        if (RawByteDigest.Value.Length != 64)
        {
            throw new FormatException("Invalid raw-byte digest value.");
        }
    }

    public EntityId<ArtifactTag> Id { get; }

    public string ArtifactRef { get; }

    public string LogicalPath { get; }

    public string ArtifactKind { get; }

    public string MediaType { get; }

    public long SizeBytes { get; }

    public ContentDigest RawByteDigest { get; }

    public string SchemaId { get; }

    public string SchemaVersion { get; }

    public ContentDigest? SourceRecordDigest { get; }

    public string? ProducedByWorkflowNode { get; }

    public string? ProvenanceEventId { get; }

    public ContentDigest? ProvenanceEventDigest { get; }

    public string? RequiredFor { get; }

    public string LogicalName => LogicalPath;

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
