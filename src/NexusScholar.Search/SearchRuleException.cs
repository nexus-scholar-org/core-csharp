using NexusScholar.Kernel;

namespace NexusScholar.Search;

public sealed class SearchRuleException : DomainRuleException
{
    public SearchRuleException(string category, string message)
        : base(message)
    {
        Category = category;
    }

    public string Category { get; }
}
