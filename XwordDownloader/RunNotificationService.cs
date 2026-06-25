using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Logging;

namespace XwordDownloader;

public class RunNotificationService
{
    private const string ResendApiUrl = "https://api.resend.com/emails";
    private readonly ILogger _logger;

    public RunNotificationService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task SendRunSummary(
        IReadOnlyCollection<DownloadAttemptResult> attempts,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        DateTime? nextRun)
    {
        var apiKey = Environment.GetEnvironmentVariable("ResendApiKey");
        var fromEmail = Environment.GetEnvironmentVariable("ResendFromEmail");
        var toEmails = GetToEmails();

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(fromEmail) ||
            toEmails.Length == 0)
        {
            _logger.LogWarning(
                "Resend notification skipped because ResendApiKey, ResendFromEmail, or ResendToEmail is not configured.");
            return;
        }

        var failedCount = attempts.Count(attempt => attempt.Failed);
        var skippedCount = attempts.Count(attempt => attempt.Status == DownloadAttemptStatus.Skipped);
        var succeededCount = attempts.Count(attempt => attempt.Succeeded);
        var subject = failedCount == 0
            ? $"xword-downloader run succeeded: {succeededCount} succeeded, {skippedCount} skipped"
            : $"xword-downloader run failed: {failedCount} failed, {succeededCount} succeeded";

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("xword-downloader/1.0");

        var payload = new
        {
            from = fromEmail,
            to = toEmails,
            subject,
            text = BuildTextBody(attempts, startedAt, finishedAt, nextRun),
            html = BuildHtmlBody(attempts, startedAt, finishedAt, nextRun)
        };

        using var response = await httpClient.PostAsJsonAsync(ResendApiUrl, payload);
        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            throw new Exception($"Resend email failed with HTTP {(int)response.StatusCode}: {responseBody}");
        }
    }

    private static string[] GetToEmails()
    {
        var configured = Environment.GetEnvironmentVariable("ResendToEmail");
        if (string.IsNullOrWhiteSpace(configured))
        {
            return [];
        }

        return configured
            .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(email => email.Length > 0)
            .ToArray();
    }

    private static string BuildTextBody(
        IEnumerable<DownloadAttemptResult> attempts,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        DateTime? nextRun)
    {
        var attemptsList = attempts.ToList();
        var builder = new StringBuilder();
        builder.AppendLine("xword-downloader run summary");
        builder.AppendLine($"Started: {startedAt:O}");
        builder.AppendLine($"Finished: {finishedAt:O}");
        builder.AppendLine($"Duration: {FormatDuration(finishedAt - startedAt)}");
        if (nextRun is not null)
        {
            builder.AppendLine($"Next scheduled run: {nextRun:O}");
        }

        builder.AppendLine();
        builder.AppendLine($"Attempted: {attemptsList.Count}");
        builder.AppendLine($"Succeeded: {attemptsList.Count(attempt => attempt.Succeeded)}");
        builder.AppendLine($"Skipped: {attemptsList.Count(attempt => attempt.Status == DownloadAttemptStatus.Skipped)}");
        builder.AppendLine($"Failed: {attemptsList.Count(attempt => attempt.Failed)}");
        builder.AppendLine();

        foreach (var attempt in attemptsList)
        {
            builder.AppendLine($"{attempt.Source}: {attempt.Status} ({FormatDuration(attempt.Duration)})");
            if (!string.IsNullOrWhiteSpace(attempt.Message))
            {
                builder.AppendLine($"  {attempt.Message}");
            }

            if (!string.IsNullOrWhiteSpace(attempt.Error))
            {
                builder.AppendLine("  Error:");
                builder.AppendLine(Indent(attempt.Error, "    "));
            }
        }

        return builder.ToString();
    }

    private static string BuildHtmlBody(
        IReadOnlyCollection<DownloadAttemptResult> attempts,
        DateTimeOffset startedAt,
        DateTimeOffset finishedAt,
        DateTime? nextRun)
    {
        var builder = new StringBuilder();
        builder.Append("<h2>xword-downloader run summary</h2>");
        builder.Append("<p>");
        builder.Append($"Started: {Html(startedAt.ToString("O"))}<br>");
        builder.Append($"Finished: {Html(finishedAt.ToString("O"))}<br>");
        builder.Append($"Duration: {Html(FormatDuration(finishedAt - startedAt))}");
        if (nextRun is not null)
        {
            builder.Append($"<br>Next scheduled run: {Html(nextRun.Value.ToString("O"))}");
        }
        builder.Append("</p>");

        builder.Append("<ul>");
        builder.Append($"<li>Attempted: {attempts.Count}</li>");
        builder.Append($"<li>Succeeded: {attempts.Count(attempt => attempt.Succeeded)}</li>");
        builder.Append($"<li>Skipped: {attempts.Count(attempt => attempt.Status == DownloadAttemptStatus.Skipped)}</li>");
        builder.Append($"<li>Failed: {attempts.Count(attempt => attempt.Failed)}</li>");
        builder.Append("</ul>");

        builder.Append("<table cellpadding=\"8\" cellspacing=\"0\" border=\"1\" style=\"border-collapse:collapse;font-family:Arial,sans-serif;font-size:14px;\">");
        builder.Append("<thead><tr><th align=\"left\">Puzzle</th><th align=\"left\">Status</th><th align=\"left\">Duration</th><th align=\"left\">Details</th></tr></thead><tbody>");
        foreach (var attempt in attempts)
        {
            builder.Append("<tr>");
            builder.Append($"<td>{Html(attempt.Source)}</td>");
            builder.Append($"<td>{Html(attempt.Status.ToString())}</td>");
            builder.Append($"<td>{Html(FormatDuration(attempt.Duration))}</td>");
            builder.Append("<td>");
            if (!string.IsNullOrWhiteSpace(attempt.Message))
            {
                builder.Append(Html(attempt.Message));
            }

            if (!string.IsNullOrWhiteSpace(attempt.Error))
            {
                builder.Append("<pre style=\"white-space:pre-wrap;max-width:900px;\">");
                builder.Append(Html(attempt.Error));
                builder.Append("</pre>");
            }

            builder.Append("</td>");
            builder.Append("</tr>");
        }

        builder.Append("</tbody></table>");
        return builder.ToString();
    }

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string Indent(string value, string prefix)
    {
        return string.Join(
            Environment.NewLine,
            value.Split(Environment.NewLine).Select(line => $"{prefix}{line}"));
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalSeconds < 1
            ? $"{duration.TotalMilliseconds:N0}ms"
            : $"{duration.TotalSeconds:N1}s";
    }
}
