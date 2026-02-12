namespace Flow.JiraSearch.Settings;

public sealed class SettingsViewModel(PluginSettings settings)
{
    public PluginSettings Settings { get; } = settings;

    public string DefaultProjects
    {
        get => string.Join(",", Settings.DefaultProjects);
        set =>
            Settings.DefaultProjects = value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
    }

    public string NamedJqlFilters
    {
        get =>
            string.Join(
                Environment.NewLine,
                Settings.NamedJqlFilters
                    .Where(filter =>
                        !string.IsNullOrWhiteSpace(filter.Name)
                        && !string.IsNullOrWhiteSpace(filter.Jql)
                    )
                    .Select(filter => $"{filter.Name}={filter.Jql}")
            );
        set => Settings.NamedJqlFilters = ParseNamedJqlFilters(value);
    }

    internal static List<NamedJqlFilter> ParseNamedJqlFilters(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return [];

        var result = new Dictionary<string, NamedJqlFilter>(StringComparer.OrdinalIgnoreCase);
        var lines = rawValue.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            var separatorIndex = trimmedLine.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == trimmedLine.Length - 1)
                continue;

            var name = trimmedLine[..separatorIndex].Trim();
            var jql = trimmedLine[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(jql))
                continue;

            result[name] = new NamedJqlFilter { Name = name, Jql = jql };
        }

        return result.Values.ToList();
    }
}
