namespace NexusScholar.ResearchWorkspace;

public sealed class ResearchWorkspaceMissingInputException : Exception
{
    public ResearchWorkspaceMissingInputException(string message)
        : base(message)
    {
    }
}

public sealed class ResearchWorkspaceDigestMismatchException : Exception
{
    public ResearchWorkspaceDigestMismatchException(string message)
        : base(message)
    {
    }
}

public sealed class ResearchWorkspaceAuthorityGenerationActiveException : InvalidOperationException
{
    public const string StableCategory = "authority-generation-active";

    public ResearchWorkspaceAuthorityGenerationActiveException()
        : base("authority-generation-active: import and analysis are locked while an authority generation is active.")
    {
    }

    public string Category => StableCategory;
}
