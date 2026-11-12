using System.Net.Http.Json;
using System.Text.Json;
using Amazon.Lambda.Core;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    private static readonly HttpClient SharedClient = new(new HttpClientHandler
    {
        AllowAutoRedirect = false
    })
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private readonly HttpClient client;
    private readonly Func<string, string?> getEnvironmentVariable;

    public Function() : this(SharedClient, Environment.GetEnvironmentVariable)
    {
    }

    // Tests supply a fake HTTP transport and configuration without changing process state.
    public Function(HttpClient client, Func<string, string?> getEnvironmentVariable)
    {
        this.client = client;
        this.getEnvironmentVariable = getEnvironmentVariable;
    }

    public async Task<string> FunctionHandler(JsonElement input, ILambdaContext context)
    {
        // GitHub's initial ping has no issue. Ignore it without contacting Slack.
        if (input.ValueKind != JsonValueKind.Object || !input.TryGetProperty("issue", out var issue))
        {
            return "ignored";
        }

        if (issue.ValueKind != JsonValueKind.Object ||
            !issue.TryGetProperty("html_url", out var issueUrl) ||
            issueUrl.ValueKind != JsonValueKind.String ||
            !Uri.TryCreate(issueUrl.GetString(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ArgumentException("The issue must have an HTTPS html_url.");
        }

        var payload = new { text = $"Issue Created: {issueUrl.GetString()}" };

        if (string.Equals(getEnvironmentVariable("DRY_RUN"), "true", StringComparison.OrdinalIgnoreCase))
        {
            context.Logger.LogLine("Dry run: Slack request skipped.");
            return JsonSerializer.Serialize(new { dryRun = true, payload });
        }

        if (!Uri.TryCreate(getEnvironmentVariable("SLACK_URL"), UriKind.Absolute, out var slackUrl) ||
            slackUrl.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException("SLACK_URL must be set to an HTTPS webhook URL.");
        }

        // Send once. Retrying could duplicate a message that Slack already accepted.
        using var response = await client.PostAsJsonAsync(slackUrl, payload);
        var result = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode || result.Trim() != "ok")
        {
            throw new InvalidOperationException($"Slack did not confirm delivery. HTTP status: {(int)response.StatusCode}.");
        }

        return "ok";
    }
}
