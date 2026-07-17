using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Desktop.AppServices;

namespace NexusScholar.Desktop.AppServices.Tests;

[TestClass]
public sealed class DesktopFullTextWorkflowFacadeTests
{
    private static readonly DateTimeOffset FixedTime =
        new(2026, 7, 17, 10, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Intake_confirmation_token_binds_projection_and_nonclaims()
    {
        var preview = Intake();
        var token = DesktopWorkspaceCommandFacade.CreateFullTextIntakeConfirmationToken(preview);

        Assert.AreNotEqual(token,
            DesktopWorkspaceCommandFacade.CreateFullTextIntakeConfirmationToken(
                preview with { RawArtifactDigest = Digest('f') }));
        Assert.AreNotEqual(token,
            DesktopWorkspaceCommandFacade.CreateFullTextIntakeConfirmationToken(
                preview with { ActorId = "reviewer-2" }));
        Assert.AreNotEqual(token,
            DesktopWorkspaceCommandFacade.CreateFullTextIntakeConfirmationToken(
                preview with { NonClaims = ["changed"] }));
        Assert.AreNotEqual(token,
            DesktopWorkspaceCommandFacade.CreateFullTextIntakeConfirmationToken(
                preview with { OperationConfirmationToken = "changed" }));
    }

    [TestMethod]
    public void Review_confirmation_token_binds_authority_actor_and_decision()
    {
        var preview = Review();
        var token = DesktopWorkspaceCommandFacade.CreateFullTextReviewConfirmationToken(preview);

        Assert.AreNotEqual(token,
            DesktopWorkspaceCommandFacade.CreateFullTextReviewConfirmationToken(
                preview with { FullTextManifestDigest = Digest('e') }));
        Assert.AreNotEqual(token,
            DesktopWorkspaceCommandFacade.CreateFullTextReviewConfirmationToken(
                preview with { ActorRole = "other-role" }));
        Assert.AreNotEqual(token,
            DesktopWorkspaceCommandFacade.CreateFullTextReviewConfirmationToken(
                preview with { Verdict = "exclude", SelectedExclusionReasonCode = "wrong-population" }));
        Assert.AreNotEqual(token,
            DesktopWorkspaceCommandFacade.CreateFullTextReviewConfirmationToken(
                preview with { OperationConfirmationToken = "changed" }));
    }

    private static DesktopFullTextIntakePreview Intake() => new(
        "C:\\workspace", "workspace-1", 8, Digest('a'), Digest('b'), Digest('c'),
        "candidate-1", "C:\\workspace\\paper.txt", "text", "text/plain",
        "reviewer-1", "human", FixedTime, 4096, null, Digest('d'), Digest('e'),
        Digest('f'), Digest('1'), Digest('2'), Digest('3'), "success",
        "fulltext-1234567890abcdef",
        ["persist immutable local Full Text generation"],
        ["no-network", "no-ai"], "operation-token", string.Empty);

    private static DesktopFullTextReviewPreview Review() => new(
        "C:\\workspace", "workspace-1", 9, Digest('a'), Digest('b'), Digest('c'),
        "fulltext-1234567890abcdef", Digest('d'), "candidate-1", Digest('e'),
        Digest('f'), Digest('1'), "success", Digest('2'), Digest('3'), Digest('4'),
        Digest('5'), Digest('6'), "include", "reviewer-1", "human", "reviewer",
        "Meets the locked criteria.", "Eligible population.", "Wrong population.",
        "wrong-population", null, FixedTime,
        ["append one human decision"], ["no-network", "no-ai"],
        "operation-token", string.Empty);

    private static string Digest(char value) => $"sha256:{new string(value, 64)}";
}
