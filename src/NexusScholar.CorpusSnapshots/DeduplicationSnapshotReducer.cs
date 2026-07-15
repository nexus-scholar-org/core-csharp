using NexusScholar.Deduplication;
using NexusScholar.Kernel;

namespace NexusScholar.CorpusSnapshots;

public sealed class DeduplicationSnapshotReduction
{
    internal DeduplicationSnapshotReduction(
        IReadOnlyList<CorpusSnapshotGroup> groups,
        IReadOnlyList<CorpusSnapshotUnresolvedCandidate> unresolvedCandidates)
    {
        Groups = Array.AsReadOnly(groups.Select(group => group with
        {
            MemberCandidateIds = Array.AsReadOnly(group.MemberCandidateIds.ToArray()),
            EvidenceReferences = Array.AsReadOnly(group.EvidenceReferences.Select(reference => reference with { }).ToArray())
        }).ToArray());
        UnresolvedCandidates = Array.AsReadOnly(unresolvedCandidates.Select(candidate => candidate with
        {
            RawSightingReferences = Array.AsReadOnly(candidate.RawSightingReferences.ToArray())
        }).ToArray());
    }

    public IReadOnlyList<CorpusSnapshotGroup> Groups { get; }

    public IReadOnlyList<CorpusSnapshotUnresolvedCandidate> UnresolvedCandidates { get; }
}

public static class DeduplicationSnapshotReducer
{
    private const string ReviewTargetKind = "review-candidate-pair";

    public static DeduplicationSnapshotReduction Reduce(
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedCorpusSnapshot predecessorSnapshot,
        VerifiedDeduplicationAuthorityDecision decision,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> activeDecisions,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> knownDecisions)
    {
        ArgumentNullException.ThrowIfNull(sourceResult);
        ArgumentNullException.ThrowIfNull(predecessorSnapshot);
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(activeDecisions);
        ArgumentNullException.ThrowIfNull(knownDecisions);

        if (activeDecisions.Any(item => item is null) || knownDecisions.Any(item => item is null))
        {
            throw Invalid("Decision collections cannot contain null entries.");
        }

        ValidateLineage(sourceResult, predecessorSnapshot, decision);
        var target = ResolveTarget(sourceResult, decision);
        var predecessorActive = ResolvePredecessorActiveDecisions(predecessorSnapshot, knownDecisions);
        ValidateActiveTransition(predecessorActive, activeDecisions, knownDecisions, decision);

        var locations = target.CandidateIds.Select(candidateId => FindLocation(predecessorSnapshot, candidateId)).ToArray();
        if (locations[0].Group is not null && ReferenceEquals(locations[0].Group, locations[1].Group))
        {
            if (string.Equals(decision.ActionType, DeduplicationAuthorityPolicyConstants.MergeAction, StringComparison.Ordinal))
            {
                throw Invalid("A review pair already in one group cannot be merged again.");
            }

            if (string.Equals(decision.ActionType, DeduplicationAuthorityPolicyConstants.KeepSeparateAction, StringComparison.Ordinal))
            {
                throw Invalid("A review pair already in one group cannot be kept separate because FE-02 does not split groups.");
            }
        }

        if (!string.Equals(decision.ActionType, DeduplicationAuthorityPolicyConstants.MergeAction, StringComparison.Ordinal))
        {
            return CopyUnchanged(predecessorSnapshot);
        }

        ValidateSeparationConstraints(sourceResult, activeDecisions, knownDecisions, decision, locations);
        return Merge(sourceResult, predecessorSnapshot, target, decision, locations);
    }

    private static DeduplicationSnapshotReduction Merge(
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedCorpusSnapshot predecessorSnapshot,
        VerifiedDeduplicationAuthorityReviewTargetDigest target,
        VerifiedDeduplicationAuthorityDecision decision,
        IReadOnlyList<CandidateLocation> locations)
    {
        var groupsToRemove = locations.Where(location => location.Group is not null)
            .Select(location => location.Group!)
            .Distinct()
            .ToArray();
        var unresolvedToRemove = locations.Where(location => location.Unresolved is not null)
            .Select(location => location.Unresolved!.CandidateId)
            .ToHashSet(StringComparer.Ordinal);
        var memberIds = groupsToRemove.SelectMany(group => group.MemberCandidateIds)
            .Concat(target.CandidateIds)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(candidateId => candidateId, StringComparer.Ordinal)
            .ToArray();

        var candidatesById = sourceResult.Result.RawCandidates.ToDictionary(candidate => candidate.CandidateId, StringComparer.Ordinal);
        var representative = DeduplicationRepresentativeSelector.Select(
            memberIds.Select(candidateId => candidatesById[candidateId]).ToArray());
        var evidence = groupsToRemove.SelectMany(group => group.EvidenceReferences)
            .Concat(decision.EvidenceReferences.Select(reference => new CorpusSnapshotEvidenceReference(
                reference.Kind,
                reference.EvidenceId,
                reference.DigestScope,
                reference.Digest)))
            .Distinct(new EvidenceReferenceComparer())
            .OrderBy(reference => reference.Kind, StringComparer.Ordinal)
            .ThenBy(reference => reference.EvidenceId, StringComparer.Ordinal)
            .ThenBy(reference => reference.DigestScope, StringComparer.Ordinal)
            .ThenBy(reference => reference.Digest.ToString(), StringComparer.Ordinal)
            .ToArray();
        var merged = new CorpusSnapshotGroup(
            BuildGroupId(memberIds),
            representative.CandidateId,
            memberIds,
            evidence);

        var groups = predecessorSnapshot.Groups.Except(groupsToRemove)
            .Append(merged)
            .OrderBy(group => group.GroupId, StringComparer.Ordinal)
            .ToArray();
        var unresolved = predecessorSnapshot.UnresolvedCandidates
            .Where(candidate => !unresolvedToRemove.Contains(candidate.CandidateId))
            .OrderBy(candidate => candidate.CandidateId, StringComparer.Ordinal)
            .ToArray();
        return new DeduplicationSnapshotReduction(groups, unresolved);
    }

    private static void ValidateSeparationConstraints(
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> activeDecisions,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> knownDecisions,
        VerifiedDeduplicationAuthorityDecision decision,
        IReadOnlyList<CandidateLocation> locations)
    {
        var mergedMembers = locations.Where(location => location.Group is not null)
            .SelectMany(location => location.Group!.MemberCandidateIds)
            .Concat(locations.Select(location => location.CandidateId))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var constraint in activeDecisions.Where(item =>
                     string.Equals(item.ActionType, DeduplicationAuthorityPolicyConstants.KeepSeparateAction, StringComparison.Ordinal)))
        {
            if (string.Equals(constraint.DecisionId, decision.SupersedesDecisionId, StringComparison.Ordinal))
            {
                continue;
            }

            var target = ResolveTarget(sourceResult, constraint);
            if (target.CandidateIds.All(mergedMembers.Contains))
            {
                throw Invalid("Merge would violate an active keep-separate constraint.");
            }
        }
    }

    private static void ValidateActiveTransition(
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> predecessorActive,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> successorActive,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> knownDecisions,
        VerifiedDeduplicationAuthorityDecision decision)
    {
        if (!successorActive.Any(item => SameRecord(item, decision)))
        {
            throw Invalid("The reduced decision must be present in the successor active decision set.");
        }

        var sameTarget = predecessorActive.SingleOrDefault(item => SameTarget(item, decision));
        if (sameTarget is null)
        {
            if (decision.SupersedesDecisionId is not null)
            {
                throw Invalid("A supersession must reference the active decision for the same target.");
            }
        }
        else
        {
            if (!string.Equals(decision.SupersedesDecisionId, sameTarget.DecisionId, StringComparison.Ordinal) ||
                !decision.InvalidationEffects.Any(effect =>
                    string.Equals(effect.RecordKind, DeduplicationDecisionConstants.InvalidationDecisionKind, StringComparison.Ordinal) &&
                    string.Equals(effect.RecordId, sameTarget.DecisionId, StringComparison.Ordinal) &&
                    effect.RecordDigest == sameTarget.DecisionDigest))
            {
                throw Invalid("A same-target correction must explicitly supersede the active decision id and digest.");
            }
        }

        var expected = predecessorActive.Where(item => sameTarget is null || !SameRecord(item, sameTarget))
            .Append(decision)
            .OrderBy(item => item.DecisionId, StringComparer.Ordinal)
            .ToArray();
        var actual = successorActive.OrderBy(item => item.DecisionId, StringComparer.Ordinal).ToArray();
        if (expected.Length != actual.Length || !expected.Zip(actual).All(pair => SameRecord(pair.First, pair.Second)))
        {
            throw Invalid("Successor active decisions must equal the predecessor active set with exactly one decision applied.");
        }

        var knownById = knownDecisions.ToDictionary(item => item.DecisionId, StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal) { decision.DecisionId };
        var current = decision;
        while (current.SupersedesDecisionId is not null)
        {
            if (!knownById.TryGetValue(current.SupersedesDecisionId, out current) || !visited.Add(current.DecisionId))
            {
                throw Invalid("Decision supersession must resolve to an acyclic known-decision chain.");
            }
        }
    }

    private static IReadOnlyList<VerifiedDeduplicationAuthorityDecision> ResolvePredecessorActiveDecisions(
        VerifiedCorpusSnapshot predecessorSnapshot,
        IReadOnlyList<VerifiedDeduplicationAuthorityDecision> knownDecisions)
    {
        var known = knownDecisions.ToDictionary(item => $"{item.DecisionId}\u001f{item.DecisionDigest}", StringComparer.Ordinal);
        return predecessorSnapshot.DecisionReferences.Select(reference =>
        {
            if (!known.TryGetValue($"{reference.DecisionId}\u001f{reference.DecisionDigest}", out var resolved))
            {
                throw Invalid("Every predecessor active decision reference must resolve to an exact verified decision.");
            }

            return resolved;
        }).ToArray();
    }

    private static VerifiedDeduplicationAuthorityReviewTargetDigest ResolveTarget(
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedDeduplicationAuthorityDecision decision)
    {
        if (!string.Equals(decision.TargetKind, ReviewTargetKind, StringComparison.Ordinal))
        {
            throw Invalid("Only verified review-candidate-pair decisions can reduce a corpus snapshot.");
        }

        foreach (var pair in sourceResult.Result.ReviewRequiredCandidates)
        {
            var candidateIds = new[] { pair.CandidateAId, pair.CandidateBId }.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var evidence = sourceResult.Result.Evidence.Where(item => IsPairEvidence(item, candidateIds)).ToArray();
            var target = DeduplicationAuthorityDigests.CreateReviewTargetDigestMaterial(sourceResult, pair, candidateIds, evidence);
            if (string.Equals(target.TargetId, decision.TargetId, StringComparison.Ordinal) && target.TargetDigest == decision.TargetContentDigest)
            {
                return target;
            }
        }

        throw Invalid("Decision target and evidence do not resolve to an exact verified source review pair.");
    }

    private static bool IsPairEvidence(DedupEvidence evidence, IReadOnlyList<string> candidateIds) =>
        evidence.ObjectCandidateId is not null &&
        candidateIds.Contains(evidence.SubjectCandidateId, StringComparer.Ordinal) &&
        candidateIds.Contains(evidence.ObjectCandidateId, StringComparer.Ordinal) &&
        !string.Equals(evidence.SubjectCandidateId, evidence.ObjectCandidateId, StringComparison.Ordinal);

    private static CandidateLocation FindLocation(VerifiedCorpusSnapshot snapshot, string candidateId)
    {
        var group = snapshot.Groups.SingleOrDefault(item => item.MemberCandidateIds.Contains(candidateId, StringComparer.Ordinal));
        var unresolved = snapshot.UnresolvedCandidates.SingleOrDefault(item => string.Equals(item.CandidateId, candidateId, StringComparison.Ordinal));
        if ((group is null) == (unresolved is null))
        {
            throw Invalid("Each review target candidate must occur exactly once in predecessor membership.");
        }

        return new CandidateLocation(candidateId, group, unresolved);
    }

    private static void ValidateLineage(
        VerifiedDeduplicationAuthorityResultDigest sourceResult,
        VerifiedCorpusSnapshot predecessorSnapshot,
        VerifiedDeduplicationAuthorityDecision decision)
    {
        if (!string.Equals(predecessorSnapshot.SourceResultId, sourceResult.Result.ResultId, StringComparison.Ordinal) ||
            predecessorSnapshot.SourceResultDigest != sourceResult.ResultDigest ||
            !string.Equals(decision.SourceResultId, sourceResult.Result.ResultId, StringComparison.Ordinal) ||
            decision.SourceResultDigest != sourceResult.ResultDigest ||
            !string.Equals(decision.SourceSnapshotId, predecessorSnapshot.SnapshotId, StringComparison.Ordinal) ||
            decision.SourceSnapshotRecordDigest != predecessorSnapshot.RecordDigest ||
            !string.Equals(decision.AuthoritySourceId, predecessorSnapshot.AuthoritySourceId, StringComparison.Ordinal) ||
            decision.AuthoritySourceDigest != predecessorSnapshot.AuthoritySourceDigest)
        {
            throw Invalid("Decision, predecessor snapshot, source result, and policy authority bindings must match exactly.");
        }
    }

    private static DeduplicationSnapshotReduction CopyUnchanged(VerifiedCorpusSnapshot snapshot) =>
        new(snapshot.Groups, snapshot.UnresolvedCandidates);

    private static string BuildGroupId(IReadOnlyList<string> orderedMembers)
    {
        var material = new CanonicalJsonObject().Add(
            "member_candidate_ids",
            CanonicalJsonValue.Array(orderedMembers.Select(CanonicalJsonValue.From).ToArray()));
        return $"group-{ContentDigest.Sha256CanonicalJson(material).Value}";
    }

    private static bool SameTarget(VerifiedDeduplicationAuthorityDecision left, VerifiedDeduplicationAuthorityDecision right) =>
        string.Equals(left.PolicyId, right.PolicyId, StringComparison.Ordinal) &&
        string.Equals(left.TargetKind, right.TargetKind, StringComparison.Ordinal) &&
        string.Equals(left.TargetId, right.TargetId, StringComparison.Ordinal);

    private static bool SameRecord(VerifiedDeduplicationAuthorityDecision left, VerifiedDeduplicationAuthorityDecision right) =>
        string.Equals(left.DecisionId, right.DecisionId, StringComparison.Ordinal) && left.DecisionDigest == right.DecisionDigest;

    private static CorpusSnapshotAuthorityException Invalid(string message) =>
        new(CorpusSnapshotErrorCodes.InvalidSnapshot, message);

    private sealed record CandidateLocation(
        string CandidateId,
        CorpusSnapshotGroup? Group,
        CorpusSnapshotUnresolvedCandidate? Unresolved);

    private sealed class EvidenceReferenceComparer : IEqualityComparer<CorpusSnapshotEvidenceReference>
    {
        public bool Equals(CorpusSnapshotEvidenceReference? x, CorpusSnapshotEvidenceReference? y) =>
            x is not null && y is not null &&
            string.Equals(x.Kind, y.Kind, StringComparison.Ordinal) &&
            string.Equals(x.EvidenceId, y.EvidenceId, StringComparison.Ordinal) &&
            string.Equals(x.DigestScope, y.DigestScope, StringComparison.Ordinal) &&
            x.Digest == y.Digest;

        public int GetHashCode(CorpusSnapshotEvidenceReference obj) =>
            HashCode.Combine(obj.Kind, obj.EvidenceId, obj.DigestScope, obj.Digest);
    }
}
