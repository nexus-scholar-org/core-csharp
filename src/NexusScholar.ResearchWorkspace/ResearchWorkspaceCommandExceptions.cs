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

public sealed class ResearchWorkspaceAuthorityTransitionException : InvalidOperationException
{
    public const string StaleAuthorityCategory = "stale-authority-generation";
    public const string ConflictingReplayCategory = "conflicting-authority-request-replay";

    public ResearchWorkspaceAuthorityTransitionException(string category, string message)
        : base($"{category}: {message}")
    {
        Category = category;
    }

    public string Category { get; }
}
