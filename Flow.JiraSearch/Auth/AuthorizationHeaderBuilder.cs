using System.Net.Http.Headers;
using System.Text;

namespace Flow.JiraSearch.Auth;

internal static class AuthorizationHeaderBuilder
{
    public static AuthenticationHeaderValue? Build(string? userEmail, string? apiToken)
    {
        if (string.IsNullOrWhiteSpace(apiToken))
            return null;

        var token = apiToken.Trim();
        var credentials = token.Contains(':', StringComparison.Ordinal)
            ? token
            : !string.IsNullOrWhiteSpace(userEmail)
                ? $"{userEmail.Trim()}:{token}"
                : token;

        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        return new AuthenticationHeaderValue("Basic", basic);
    }
}
