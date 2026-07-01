using Microsoft.VisualStudio.TestTools.UnitTesting;
using NexusScholar.Cli;

namespace NexusScholar.Cli.Tests;

[TestClass]
public sealed class ResearchWorkspaceReviewClustersCommandTests
{
    private const string QueryText = "systematic review screening software";
    private static readonly DateTimeOffset FixedNow = new(2026, 7, 1, 0, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Review_requires_generated_workspace_plan()
    {
        using var workspace = TemporaryWorkspace.CreateInitialized();

        var exitCode = RunCli(workspace.Root, new[] { "review" }, out var output, out var error);

        Assert.AreEqual(2, exitCode);
        Assert.AreEqual(string.Empty, output);
        StringAssert.Contains(error, "Generated workspace plan not found: nexus-output/workspace/current.workspace-plan.json");
        StringAssert.Contains(error, "Run: nexus analyze");
    }

    [TestMethod]
    public void Review_prints_read_only_queue()
    {
        using var workspace = TemporaryWorkspace.FromFixture();

        var exitCode = RunCli(workspace.Root, new[] { "review" }, out var output, out var error);

        Assert.AreEqual(0, exitCode, error);
        AssertTextEqual(ExpectedPath("review.txt"), output);
        AssertNoExecutableMergeCommands(output);
        Assert.AreEqual(string.Empty, error);
    }

    [TestMethod]
    public void Clusters_prints_exact_and_review_counts()
    {
        using var workspace = TemporaryWorkspace.FromFixture();

        var exitCode = RunCli(workspace.Root, new[] { "clusters" }, out var output, out var error);

        Assert.AreEqual(0, exitCode, error);
        AssertTextEqual(ExpectedPath("clusters.txt"), output);
        Assert.AreEqual(string.Empty, error);
    }

    [TestMethod]
    public void Clusters_exact_prints_cluster_details()
    {
        using var workspace = TemporaryWorkspace.FromFixture();

        var exitCode = RunCli(workspace.Root, new[] { "clusters", "exact" }, out var output, out var error);

        Assert.AreEqual(0, exitCode, error);
        StringAssert.Contains(output, "Exact duplicate clusters");
        StringAssert.Contains(output, "[dedup-cluster-0001]");
        StringAssert.Contains(output, "Members: 2");
        StringAssert.Contains(output, "Match basis: exact-doi");
        StringAssert.Contains(output, "This command displays generated analysis output only");
        Assert.AreEqual(string.Empty, error);
    }

    [TestMethod]
    public void Clusters_review_prints_review_required_candidates()
    {
        using var workspace = TemporaryWorkspace.FromFixture();

        var exitCode = RunCli(workspace.Root, new[] { "clusters", "review" }, out var output, out var error);

        Assert.AreEqual(0, exitCode, error);
        AssertTextEqual(ExpectedPath("clusters-review.txt"), output);
        AssertNoExecutableMergeCommands(output);
        Assert.AreEqual(string.Empty, error);
    }

    [TestMethod]
    public void Clusters_show_prints_candidate_details()
    {
        using var workspace = TemporaryWorkspace.FromFixture();

        var exitCode = RunCli(workspace.Root, new[] { "clusters", "show", "dedup-candidate-0001" }, out var output, out var error);

        Assert.AreEqual(0, exitCode, error);
        AssertTextEqual(ExpectedPath("clusters-show-dedup-candidate-0001.txt"), output);
        AssertNoExecutableMergeCommands(output);
        Assert.AreEqual(string.Empty, error);
    }

    [TestMethod]
    public void Clusters_show_returns_nonzero_for_missing_id()
    {
        using var workspace = TemporaryWorkspace.FromFixture();

        var exitCode = RunCli(workspace.Root, new[] { "clusters", "show", "missing-id" }, out var output, out var error);

        Assert.AreEqual(1, exitCode);
        Assert.AreEqual(string.Empty, output);
        AssertTextEqual(ExpectedPath("clusters-show-missing.txt"), error);
    }

    [TestMethod]
    public void Review_and_clusters_do_not_mutate_workspace_files()
    {
        using var workspace = TemporaryWorkspace.FromFixture();
        var projectPath = Path.Combine(workspace.Root, "nexus.project.json");
        var planPath = Path.Combine(workspace.Root, "nexus-output", "workspace", "current.workspace-plan.json");
        var projectBefore = File.ReadAllText(projectPath);
        var planBefore = File.ReadAllText(planPath);

        Assert.AreEqual(0, RunCli(workspace.Root, new[] { "review" }, out _, out var reviewError), reviewError);
        Assert.AreEqual(0, RunCli(workspace.Root, new[] { "clusters" }, out _, out var clustersError), clustersError);
        Assert.AreEqual(0, RunCli(workspace.Root, new[] { "clusters", "show", "dedup-candidate-0001" }, out _, out var showError), showError);

        Assert.AreEqual(projectBefore, File.ReadAllText(projectPath));
        Assert.AreEqual(planBefore, File.ReadAllText(planPath));
    }

    [TestMethod]
    public void Review_and_clusters_handle_generated_appservices_plan()
    {
        using var workspace = TemporaryWorkspace.CreateInitialized();
        ImportCombinedBundle(workspace.Root);
        Assert.AreEqual(0, RunCli(workspace.Root, new[] { "analyze" }, out _, out var analyzeError), analyzeError);

        Assert.AreEqual(0, RunCli(workspace.Root, new[] { "review" }, out var reviewOutput, out var reviewError), reviewError);
        Assert.AreEqual(0, RunCli(workspace.Root, new[] { "clusters", "review" }, out var clustersReviewOutput, out var clustersReviewError), clustersReviewError);
        Assert.AreEqual(0, RunCli(workspace.Root, new[] { "clusters", "show", "dedup-candidate-0001" }, out var showOutput, out var showError), showError);

        StringAssert.Contains(reviewOutput, "4 human merge decisions require review");
        StringAssert.Contains(reviewOutput, "4 duplicate record comparisons");
        StringAssert.Contains(clustersReviewOutput, "[dedup-candidate-0001]");
        StringAssert.Contains(clustersReviewOutput, "title-similarity-threshold");
        StringAssert.Contains(showOutput, "Candidate: dedup-candidate-0001");
        StringAssert.Contains(showOutput, "APP-01 action");
        AssertNoExecutableMergeCommands(reviewOutput + clustersReviewOutput + showOutput);
        Assert.IsFalse((reviewOutput + clustersReviewOutput + showOutput).Contains(workspace.Root, StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void Usage_includes_review_and_clusters()
    {
        StringAssert.Contains(CliApplication.Usage, "review");
        StringAssert.Contains(CliApplication.Usage, "clusters");
    }

    private static void ImportCombinedBundle(string workspaceRoot)
    {
        ImportSearch(workspaceRoot, "search-001", "scopus", "csv", CombinedBundlePath("combined_scopus_like.csv"));
        ImportSearch(workspaceRoot, "search-002", "web-of-science", "ris", CombinedBundlePath("combined_wos_like.ris"));
        ImportSearch(workspaceRoot, "search-003", "google-scholar", "bibtex", CombinedBundlePath("combined_scholar_style.bib"));
        ImportSearch(workspaceRoot, "search-004", "web-of-science", "csv", CombinedBundlePath("combined_wos_like_source_specific.csv"));
    }

    private static void ImportSearch(string workspaceRoot, string queryId, string source, string format, string path)
    {
        var exitCode = RunCli(
            workspaceRoot,
            new[] { "import", "search", path, "--source", source, "--format", format, "--query-id", queryId, "--query", QueryText },
            out _,
            out var error);
        Assert.AreEqual(0, exitCode, error);
    }

    private static int RunCli(string workingDirectory, string[] args, out string output, out string error)
    {
        using var outputWriter = new StringWriter();
        using var errorWriter = new StringWriter();

        var exitCode = CliApplication.Run(
            args,
            outputWriter,
            errorWriter,
            workingDirectory,
            () => FixedNow);

        output = outputWriter.ToString();
        error = errorWriter.ToString();
        return exitCode;
    }

    private static string CombinedBundlePath(string fileName)
    {
        return Path.Combine(
            RepositoryRoot(),
            "tests",
            "NexusScholar.AppServices.Tests",
            "Fixtures",
            "App01GeneratedLocalBundles",
            "bundles",
            "FB07-combined-app01-demo",
            fileName);
    }

    private static string ExpectedPath(string fileName)
    {
        return Path.Combine(RepositoryRoot(), "tests", "NexusScholar.Cli.Tests", "Fixtures", "ResearchWorkspaceReviewClusters", "expected", fileName);
    }

    private static string FixtureWorkspacePath()
    {
        return Path.Combine(RepositoryRoot(), "tests", "NexusScholar.Cli.Tests", "Fixtures", "ResearchWorkspaceReviewClusters", "workspace");
    }

    private static void AssertTextEqual(string expectedPath, string actual)
    {
        Assert.AreEqual(
            NormalizeLineEndings(File.ReadAllText(expectedPath)).TrimEnd('\n'),
            NormalizeLineEndings(actual).TrimEnd('\n'));
    }

    private static void AssertNoExecutableMergeCommands(string output)
    {
        Assert.IsFalse(output.Contains("nexus.command.dedup.accept-merge", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("nexus.command.dedup.reject-merge", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("nexus.command.dedup.mark-unresolved", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("placeholder-accept-merge", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("placeholder-reject-merge", StringComparison.Ordinal));
        Assert.IsFalse(output.Contains("placeholder-mark-unresolved", StringComparison.Ordinal));
    }

    private static string NormalizeLineEndings(string value)
    {
        return value.ReplaceLineEndings("\n");
    }

    private static string RepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "NexusScholar.Core.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed class TemporaryWorkspace : IDisposable
    {
        private TemporaryWorkspace(string root) => Root = root;

        public string Root { get; }

        public static TemporaryWorkspace Create()
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "nexus-cli-review-clusters-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return new TemporaryWorkspace(root);
        }

        public static TemporaryWorkspace CreateInitialized()
        {
            var workspace = Create();
            var exitCode = RunCli(
                workspace.Root,
                new[] { "init", "--title", "AI screening tools review" },
                out _,
                out var error);
            Assert.AreEqual(0, exitCode, error);
            return workspace;
        }

        public static TemporaryWorkspace FromFixture()
        {
            var workspace = Create();
            CopyDirectory(FixtureWorkspacePath(), workspace.Root);
            return workspace;
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }

        private static void CopyDirectory(string sourceDirectory, string targetDirectory)
        {
            Directory.CreateDirectory(targetDirectory);

            foreach (var file in Directory.GetFiles(sourceDirectory))
            {
                File.Copy(file, Path.Combine(targetDirectory, Path.GetFileName(file)), overwrite: false);
            }

            foreach (var directory in Directory.GetDirectories(sourceDirectory))
            {
                CopyDirectory(directory, Path.Combine(targetDirectory, Path.GetFileName(directory)));
            }
        }
    }
}
