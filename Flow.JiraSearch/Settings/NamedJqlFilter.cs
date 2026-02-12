namespace Flow.JiraSearch.Settings;

public sealed class NamedJqlFilter
{
    public string Name { get; set; } = string.Empty;
    public string Jql { get; set; } = string.Empty;
}
