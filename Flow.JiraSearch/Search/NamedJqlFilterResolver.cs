using Flow.JiraSearch.Settings;

namespace Flow.JiraSearch.Search;

internal static class NamedJqlFilterResolver
{
    public static string? ExtractRequestedFilterName(string query)
    {
        var token = query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (token is null || !token.StartsWith("&", StringComparison.Ordinal))
            return null;

        if (string.IsNullOrWhiteSpace(token) || token.Length == 1)
            return null;

        var name = token[1..].Trim();
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    public static bool TryResolveJql(
        string query,
        IReadOnlyList<NamedJqlFilter> filters,
        out string resolvedJql
    )
    {
        var requestedName = ExtractRequestedFilterName(query);
        if (string.IsNullOrWhiteSpace(requestedName))
        {
            resolvedJql = string.Empty;
            return false;
        }

        var filter = filters.FirstOrDefault(filter =>
            string.Equals(filter.Name, requestedName, StringComparison.OrdinalIgnoreCase)
        );
        if (filter is null || string.IsNullOrWhiteSpace(filter.Jql))
        {
            resolvedJql = string.Empty;
            return false;
        }

        resolvedJql = filter.Jql.Trim();
        return true;
    }
}
