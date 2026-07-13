using System.Reflection;

string[] expectedAssemblies =
[
    "NexusScholar.Artifacts",
    "NexusScholar.Bundles",
    "NexusScholar.Deduplication",
    "NexusScholar.Extensibility",
    "NexusScholar.FullText",
    "NexusScholar.Kernel",
    "NexusScholar.Protocol",
    "NexusScholar.Provenance",
    "NexusScholar.Screening",
    "NexusScholar.Search",
    "NexusScholar.Shared",
    "NexusScholar.Workflow"
];

foreach (var assemblyName in expectedAssemblies)
{
    var assembly = Assembly.Load(assemblyName);
    if (!string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Loaded assembly identity did not match '{assemblyName}'.");
    }
}

Console.WriteLine($"Loaded {expectedAssemblies.Length} Nexus Scholar package assemblies from the local package source.");
