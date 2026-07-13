namespace NexusScholar.FullText;

public sealed record UnverifiedFullTextChain(
    FullTextInput Input,
    FullTextAcquisitionRecord Acquisition,
    FullTextArtifactEvidence Artifact,
    byte[] AcceptedBytes,
    long MaximumBytes);

public sealed class VerifiedFullTextChain
{
    internal VerifiedFullTextChain(
        FullTextInput input,
        FullTextAcquisitionRecord acquisition,
        FullTextArtifactEvidence artifact)
    {
        Input = input;
        Acquisition = acquisition;
        Artifact = artifact;
    }

    public FullTextInput Input { get; }
    public FullTextAcquisitionRecord Acquisition { get; }
    public FullTextArtifactEvidence Artifact { get; }
}

public sealed class VerifiedFullTextExtraction
{
    internal VerifiedFullTextExtraction(VerifiedFullTextChain source, FullTextExtractionRecord record)
    {
        Source = source;
        Record = record;
    }

    public VerifiedFullTextChain Source { get; }
    public FullTextExtractionRecord Record { get; }
}

public static class FullTextRehydrator
{
    public static VerifiedFullTextChain Rehydrate(UnverifiedFullTextChain chain)
    {
        ArgumentNullException.ThrowIfNull(chain);
        ArgumentNullException.ThrowIfNull(chain.Input);
        ArgumentNullException.ThrowIfNull(chain.Acquisition);
        ArgumentNullException.ThrowIfNull(chain.Artifact);
        ArgumentNullException.ThrowIfNull(chain.AcceptedBytes);

        if (!FullTextSourceKinds.IsAllowedInput(chain.Input.SourceKind) ||
            !FullTextEligibility.IsAllowed(chain.Input.Eligibility))
        {
            throw Invalid("Full Text input source kind or eligibility is not allowed.");
        }

        var acquisition = chain.Acquisition;
        var artifact = chain.Artifact;
        if (!FullTextAuthorityValidator.SameInput(chain.Input, acquisition.InputRef) ||
            !FullTextAuthorityValidator.SameInput(chain.Input, artifact.InputRef) ||
            !string.Equals(artifact.CandidateId, chain.Input.CandidateId, StringComparison.Ordinal) ||
            !string.Equals(artifact.CandidateSetId, chain.Input.CandidateSetId, StringComparison.Ordinal) ||
            !string.Equals(artifact.ScreeningDecisionId, chain.Input.ScreeningDecisionId, StringComparison.Ordinal) ||
            !string.Equals(artifact.WorkId, chain.Input.WorkId, StringComparison.Ordinal) ||
            !string.Equals(artifact.DedupClusterId, chain.Input.DedupClusterId, StringComparison.Ordinal) ||
            !string.Equals(artifact.AcquisitionId, acquisition.AcquisitionId, StringComparison.Ordinal) ||
            !string.Equals(artifact.AcquisitionKind, acquisition.AcquisitionKind, StringComparison.Ordinal) ||
            !string.Equals(artifact.SourceAlias, acquisition.SourceAlias, StringComparison.Ordinal) ||
            (acquisition.ArtifactEvidenceId is not null &&
                !string.Equals(acquisition.ArtifactEvidenceId, artifact.ArtifactId, StringComparison.Ordinal)))
        {
            throw Invalid("Full Text input, acquisition, attempt, and artifact bindings do not match.");
        }

        ValidateAcquisitionState(acquisition, artifact);
        FullTextArtifactValidator.Validate(
            artifact.ArtifactKind,
            chain.AcceptedBytes,
            chain.MaximumBytes,
            artifact.MediaType);

        var expectedDigest = NexusScholar.Kernel.ContentDigest.Sha256(chain.AcceptedBytes).ToString();
        if (artifact.SizeBytes < 0 || artifact.SizeBytes != chain.AcceptedBytes.LongLength ||
            !string.Equals(artifact.RawByteDigest, expectedDigest, StringComparison.Ordinal) ||
            !string.Equals(artifact.RawByteDigestScope, NexusScholar.Kernel.DigestScope.RawArtifactBytes.ToString(), StringComparison.Ordinal))
        {
            throw Invalid("Full Text artifact bytes, size, or digest do not match persisted evidence.");
        }

        return new VerifiedFullTextChain(chain.Input, acquisition, artifact);
    }

    private static void ValidateAcquisitionState(
        FullTextAcquisitionRecord acquisition,
        FullTextArtifactEvidence artifact)
    {
        var attempts = acquisition.SourceAttempts.OrderBy(item => item.AttemptOrder).ToArray();
        var successfulAttempts = attempts
            .Where(item => string.Equals(item.Status, FullTextAttemptStatuses.Success, StringComparison.Ordinal))
            .ToArray();
        if (attempts.Length == 0 ||
            !attempts.Select(item => item.AttemptId).AllDistinct() ||
            !attempts.Select(item => item.AttemptOrder).SequenceEqual(Enumerable.Range(1, attempts.Length)) ||
            successfulAttempts.Length != 1 ||
            !string.Equals(acquisition.Status, FullTextAttemptStatuses.Success, StringComparison.Ordinal) ||
            !string.Equals(artifact.ValidationStatus, FullTextAttemptStatuses.Success, StringComparison.Ordinal))
        {
            throw new FullTextRuleException(
                FullTextErrorCodes.InvalidAcquisitionState,
                "Successful Full Text artifact authority requires a contiguous attempt history with exactly one accepted success.");
        }

        var acceptedAttempt = successfulAttempts[0];
        if (!string.Equals(acceptedAttempt.AcquisitionKind, acquisition.AcquisitionKind, StringComparison.Ordinal) ||
            !string.Equals(acceptedAttempt.SourceAlias, acquisition.SourceAlias, StringComparison.Ordinal) ||
            attempts.Any(item => !ReferenceEquals(item, acceptedAttempt) && item.ArtifactEvidenceId is not null) ||
            (acceptedAttempt.ArtifactEvidenceId is not null &&
                !string.Equals(acceptedAttempt.ArtifactEvidenceId, artifact.ArtifactId, StringComparison.Ordinal)) ||
            (acceptedAttempt.ArtifactKind is not null &&
                !string.Equals(acceptedAttempt.ArtifactKind, artifact.ArtifactKind, StringComparison.Ordinal)) ||
            (acceptedAttempt.MediaType is not null &&
                !string.Equals(acceptedAttempt.MediaType, artifact.MediaType, StringComparison.Ordinal)))
        {
            throw Invalid("Full Text attempt artifact binding does not match artifact evidence.");
        }
    }

    private static FullTextRuleException Invalid(string message) =>
        new(FullTextErrorCodes.InvalidAuthorityChain, message);

    private static bool AllDistinct<T>(this IEnumerable<T> values) where T : notnull
    {
        var seen = new HashSet<T>();
        return values.All(seen.Add);
    }
}

internal static class FullTextAuthorityValidator
{
    internal static bool SameInput(FullTextInput left, FullTextInput right) =>
        string.Equals(left.InputId, right.InputId, StringComparison.Ordinal) &&
        string.Equals(left.SourceKind, right.SourceKind, StringComparison.Ordinal) &&
        string.Equals(left.CandidateSetId, right.CandidateSetId, StringComparison.Ordinal) &&
        string.Equals(left.CandidateId, right.CandidateId, StringComparison.Ordinal) &&
        string.Equals(left.Eligibility, right.Eligibility, StringComparison.Ordinal) &&
        string.Equals(left.ScreeningDecisionId, right.ScreeningDecisionId, StringComparison.Ordinal) &&
        string.Equals(left.ScreeningStage, right.ScreeningStage, StringComparison.Ordinal) &&
        string.Equals(left.DedupResultId, right.DedupResultId, StringComparison.Ordinal) &&
        string.Equals(left.DedupClusterId, right.DedupClusterId, StringComparison.Ordinal) &&
        string.Equals(left.WorkId, right.WorkId, StringComparison.Ordinal) &&
        left.SourceRefs.SequenceEqual(right.SourceRefs) &&
        left.NonClaims.SequenceEqual(right.NonClaims, StringComparer.Ordinal);
}

public static class FullTextExtractionRehydrator
{
    public static VerifiedFullTextExtraction Rehydrate(
        VerifiedFullTextChain source,
        FullTextExtractionRecord record)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(record);
        if (!string.Equals(record.SourceArtifactId, source.Artifact.ArtifactId, StringComparison.Ordinal) ||
            !string.Equals(record.SourceRawByteDigest, source.Artifact.RawByteDigest, StringComparison.Ordinal) ||
            !string.Equals(record.SourceRawByteDigestScope, source.Artifact.RawByteDigestScope, StringComparison.Ordinal))
        {
            throw new FullTextRuleException(
                FullTextErrorCodes.ExtractionSourceMismatch,
                "Extraction record does not bind the verified source artifact.");
        }

        return new VerifiedFullTextExtraction(source, record);
    }
}
