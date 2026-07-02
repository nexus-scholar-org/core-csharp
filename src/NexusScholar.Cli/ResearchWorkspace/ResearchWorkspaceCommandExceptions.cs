namespace NexusScholar.Cli.ResearchWorkspace;

internal sealed class ResearchWorkspaceMissingInputException : Exception
{
    public ResearchWorkspaceMissingInputException(string message)
        : base(message)
    {
    }
}

internal sealed class ResearchWorkspaceDigestMismatchException : Exception
{
    public ResearchWorkspaceDigestMismatchException(string message)
        : base(message)
    {
    }
}
