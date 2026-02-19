using System.Text.RegularExpressions;

namespace Flow.JiraSearch.JiraClient;

internal interface IIssueQueryBuilder
{
    Task<string> BuildTextJql(
        string text,
        IReadOnlyList<string> projects,
        CancellationToken cancellationToken
    );
}

internal class IssueQueryBuilder(IUserSearchClient userSearchClient) : IIssueQueryBuilder
{
    private static readonly Regex SlashIssueTokenRegex = new(
        @"^/[A-Z0-9\-]{1,}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
    );

    public async Task<string> BuildTextJql(
        string text,
        IReadOnlyList<string> projects,
        CancellationToken cancellationToken
    )
    {
        if (HasSlashIssueToken(text))
            return await BuildSlashModeJql(text, projects, cancellationToken);

        return await BuildDefaultModeJql(text, projects, cancellationToken);
    }

    private async Task<string> BuildDefaultModeJql(
        string text,
        IReadOnlyList<string> projects,
        CancellationToken cancellationToken
    )
    {
        var tokens = text.Tokenize();

        return await tokens
            .When("#all")
            .ThenDoNothing()
            .When("#([a-zA-Z]{2,})")
            .ThenRemember()
            .Aggregate(mem => $"project IN ({string.Join(", ", mem)})")
            .Else(projects.Count > 0 ? $"project IN ({string.Join(", ", projects)})" : string.Empty)
            .When("\\!")
            .Then("statusCategory = Done")
            .When("\\?")
            .Then("statusCategory = \"In Progress\"")
            .When("\\*")
            .ThenDoNothing() // does nothing, but resets the match state
            .Else("statusCategory != Done")
            .When(@"@\?")
            .Then("assignee IS EMPTY")
            .When("@me")
            .ThenRemember("currentUser()")
            .When(@"@([\p{L}-]{2,})")
            .ThenRemember(async input =>
                await userSearchClient.FindUserIdsByExactNameAsync(input, 5, cancellationToken)
            )
            .Aggregate(mem => $"assignee IN ({string.Join(", ", mem)})")
            .When(@"\+([a-zA-Z0-9]{2,})")
            .ThenRemember()
            .Aggregate(mem => $"labels IN ({string.Join(", ", mem)})")
            .When(@"[A-Z][A-Z0-9]+-\d+")
            .ThenRemember()
            .Aggregate(mem => $"issuekey IN ({string.Join(", ", mem)})")
            .When("@reporter:me")
            .ThenRemember("currentUser()")
            .When("@reporter:([\\p{L}-]{2,})")
            .ThenRemember(async input =>
                await userSearchClient.FindUserIdsByExactNameAsync(input, 5, cancellationToken)
            )
            .Aggregate(mem => $"reporter IN ({string.Join(", ", mem)})")
            .When("@was:me")
            .ThenRemember("currentUser()")
            .When("@was:([\\p{L}-]{2,})")
            .ThenRemember(async input =>
                await userSearchClient.FindUserIdsByExactNameAsync(input, 5, cancellationToken)
            )
            .Aggregate(mem => $"assignee WAS ({string.Join(", ", mem)})")
            .When(".*")
            .ThenRemember()
            .Aggregate(mem =>
                $"(summary ~ \"{string.Join(" ", mem)}\" OR text ~ \"{string.Join(" ", mem)}\")"
            )
            .BuildJql();
    }

    private async Task<string> BuildSlashModeJql(
        string text,
        IReadOnlyList<string> projects,
        CancellationToken cancellationToken
    )
    {
        var tokens = text.Tokenize();

        return await tokens
            .When("#all")
            .ThenDoNothing()
            .When("#([a-zA-Z]{2,})")
            .ThenRemember()
            .Aggregate(mem => $"project IN ({string.Join(", ", mem)})")
            .Else(projects.Count > 0 ? $"project IN ({string.Join(", ", projects)})" : string.Empty)
            .When(@"\/([a-zA-Z0-9\-]{1,})")
            .ThenRemember()
            .Aggregate(BuildSlashIssueLookupClause)
            .When("\\!")
            .ThenDoNothing()
            .When("\\?")
            .ThenDoNothing()
            .When("\\*")
            .ThenDoNothing()
            .When(@"@\?")
            .Then("assignee IS EMPTY")
            .When("@me")
            .ThenRemember("currentUser()")
            .When(@"@([\p{L}-]{2,})")
            .ThenRemember(async input =>
                await userSearchClient.FindUserIdsByExactNameAsync(input, 5, cancellationToken)
            )
            .Aggregate(mem => $"assignee IN ({string.Join(", ", mem)})")
            .When(@"\+([a-zA-Z0-9]{2,})")
            .ThenRemember()
            .Aggregate(mem => $"labels IN ({string.Join(", ", mem)})")
            .When(@"[A-Z][A-Z0-9]+-\d+")
            .ThenRemember()
            .Aggregate(mem => $"issuekey IN ({string.Join(", ", mem)})")
            .When("@reporter:me")
            .ThenRemember("currentUser()")
            .When("@reporter:([\\p{L}-]{2,})")
            .ThenRemember(async input =>
                await userSearchClient.FindUserIdsByExactNameAsync(input, 5, cancellationToken)
            )
            .Aggregate(mem => $"reporter IN ({string.Join(", ", mem)})")
            .When("@was:me")
            .ThenRemember("currentUser()")
            .When("@was:([\\p{L}-]{2,})")
            .ThenRemember(async input =>
                await userSearchClient.FindUserIdsByExactNameAsync(input, 5, cancellationToken)
            )
            .Aggregate(mem => $"assignee WAS ({string.Join(", ", mem)})")
            .When(".*")
            .ThenRemember()
            .Aggregate(mem =>
                $"(summary ~ \"{string.Join(" ", mem)}\" OR text ~ \"{string.Join(" ", mem)}\")"
            )
            .BuildJql();
    }

    private static bool HasSlashIssueToken(string text) =>
        text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(token => SlashIssueTokenRegex.IsMatch(token));

    private static string BuildSlashIssueLookupClause(IEnumerable<string> capturedValues)
    {
        var clauses = capturedValues
            .Select(BuildSingleSlashIssueLookupClause)
            .Where(clause => !string.IsNullOrWhiteSpace(clause))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (clauses.Count == 0)
            return string.Empty;
        if (clauses.Count == 1)
            return clauses[0];

        return $"({string.Join(" OR ", clauses)})";
    }

    private static string BuildSingleSlashIssueLookupClause(string rawValue)
    {
        var upper = rawValue.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(upper))
            return string.Empty;

        // Keep prefix matching on issue key and ignore trailing dash during typing.
        var terms = new[] { upper.TrimEnd('-') }
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (terms.Count == 0)
            return string.Empty;

        var partialClauses = terms.Select(term =>
            $"issuekey ~ \"{EscapeForJqlQuotedText(term)}*\""
        );
        return terms.Count == 1
            ? $"({partialClauses.First()})"
            : $"({string.Join(" OR ", partialClauses)})";
    }

    private static string EscapeForJqlQuotedText(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);
}
