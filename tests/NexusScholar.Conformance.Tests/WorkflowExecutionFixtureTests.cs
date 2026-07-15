using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NexusScholar.Conformance.Tests;

[TestClass]
public sealed class WorkflowExecutionFixtureTests
{
    [TestMethod]
    public void Workflow_execution_conformance_catalog_is_canonical_and_complete()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "workflow-execution", "cases.json");
        var bytes = File.ReadAllBytes(path);
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        Assert.AreEqual("nexus.workflow-execution.conformance-catalog.v1", root.GetProperty("schema").GetString());
        var ids = root.GetProperty("cases").EnumerateArray()
            .Select(item => item.GetProperty("id").GetString()!)
            .ToArray();
        CollectionAssert.AreEqual(new[]
        {
            "canonical-record-roundtrip",
            "automation-human-authority",
            "conflicting-request-id",
            "noncanonical-event-bytes",
            "partial-invalidation-propagation",
            "stale-journal-head",
            "unresolved-output-digest",
            "wrong-output-artifact-kind"
        }, ids);
        Assert.IsTrue(bytes.AsSpan().SequenceEqual(System.Text.Encoding.UTF8.GetBytes(root.GetRawText() + "\n")) ||
            bytes.AsSpan().SequenceEqual(System.Text.Encoding.UTF8.GetBytes(root.GetRawText())),
            "Catalog must contain one compact JSON value without noncanonical whitespace.");
    }
}
