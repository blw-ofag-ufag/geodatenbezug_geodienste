using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;

namespace Geodatenbezug;

/// <summary>
/// Service for sending emails.
/// </summary>
public class MailService(ILogger<MailService> logger)
{
    /// <summary>
    /// Sends an email with the processing results.
    /// </summary>
    public void SendProcessingResults(List<ProcessingResult> results)
    {
        try
        {
            logger.LogInformation("Versende E-Mail mit Prozessierungsresultaten...");

            var smptHost = Environment.GetEnvironmentVariable("SmtpHost");
            if (string.IsNullOrEmpty(smptHost))
            {
                throw new InvalidOperationException("SMTP Host is not set");
            }

            var smtpPortValue = Environment.GetEnvironmentVariable("SmtpPort");
            if (!int.TryParse(smtpPortValue, out var smtpPort))
            {
                smtpPort = 25;
            }

            using var smtpClient = new SmtpClient(smptHost, smtpPort);

            var smtpUser = Environment.GetEnvironmentVariable("SmtpUser");
            var smtpPassword = Environment.GetEnvironmentVariable("SmtpPw");
            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                smtpClient.UseDefaultCredentials = true;
            }
            else
            {
                smtpClient.EnableSsl = true;
                smtpClient.Credentials = new NetworkCredential(smtpUser, smtpPassword);
            }

            using var message = BuildMailMessage(results);

            smtpClient.Send(message);
            logger.LogInformation("E-Mail erfolgreich versendet");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Fehler beim Versenden der E-Mail: {ex.Message}");
        }
    }

    /// <summary>
    /// Builds mail message.
    /// </summary>
    protected internal MailMessage BuildMailMessage(List<ProcessingResult> results)
    {
        var senderEmail = Environment.GetEnvironmentVariable("SmtpFrom");
        var recipientEmail = Environment.GetEnvironmentVariable("SmtpTo");
        if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(recipientEmail))
        {
            throw new InvalidOperationException("Sender or recipient email is not set");
        }

        var message = new MailMessage(senderEmail, recipientEmail)
        {
            Subject = "Geodatenbezug Prozessierungsresultate",
            IsBodyHtml = true,
            Body = BuildMailBody(results),
        };

        if (results.Any(result => result.Code != HttpStatusCode.OK))
        {
            var smtpCc = Environment.GetEnvironmentVariable("SmtpCc");

            if (!string.IsNullOrEmpty(smtpCc))
            {
                message.CC.Add(smtpCc);
            }
        }

        return message;
    }

    /// <summary>
    /// Builds the body of the email with the processing results.
    /// </summary>
    protected internal string BuildMailBody(List<ProcessingResult> results)
    {
        var successResults = results.FindAll(result => result.Code == HttpStatusCode.OK);
        var failureResults = results.FindAll(result => result.Code != HttpStatusCode.OK);

        var tableHeaderThema = CreateTableHeaderCell("Thema");
        var tableHeaderKanton = CreateTableHeaderCell("Kanton");
        var tableHeaderAktualisiertAm = CreateTableHeaderCell("Aktualisiert am");

        var body = new StringBuilder();
        body.AppendLine("<p>Guten Tag</p>");

        if (successResults.Count > 0)
        {
            body.AppendLine("<p>Die folgenden Themen wurden erfolgreich prozessiert:</p>");
            body.AppendLine("<table>");
            body.AppendLine("<tr>");
            body.AppendLine(tableHeaderThema);
            body.AppendLine(tableHeaderKanton);
            body.AppendLine(tableHeaderAktualisiertAm);
            body.AppendLine(CreateTableHeaderCell(string.Empty));
            body.AppendLine("</tr>");

            foreach (var result in successResults)
            {
                body.AppendLine("<tr>");
                body.AppendLine(CreateTableDataCell(result.TopicTitle));
                body.AppendLine(CreateTableDataCell(result.Canton.ToString()));
                body.AppendLine(CreateTableDataCell(FormatDateTime(result.UpdatedAt)));
                body.AppendLine(CreateTableDataCell($"<a href=\"{result.DownloadUrl}\">Herunterladen</a>"));
                body.AppendLine("</tr>");
            }

            body.AppendLine("</table>");
            body.AppendLine("<br />");
        }

        if (failureResults.Count > 0)
        {
            body.AppendLine("<p>Bei folgenden Themen traten während der Prozessierung Fehler auf:</p>");
            body.AppendLine("<table>");
            body.AppendLine("<tr>");
            body.AppendLine(tableHeaderThema);
            body.AppendLine(tableHeaderKanton);
            body.AppendLine(tableHeaderAktualisiertAm);
            body.AppendLine(CreateTableHeaderCell("Fehler"));
            body.AppendLine("</tr>");

            foreach (var result in failureResults)
            {
                body.AppendLine("<tr>");
                body.AppendLine(CreateTableDataCell(result.TopicTitle));
                body.AppendLine(CreateTableDataCell(result.Canton.ToString()));
                body.AppendLine(CreateTableDataCell(FormatDateTime(result.UpdatedAt)));
                body.AppendLine(CreateTableDataCell(string.IsNullOrEmpty(result.Info) ? $"{result.Reason}" : $"{result.Reason} - {result.Info}"));
                body.AppendLine("</tr>");
            }

            body.AppendLine("</table>");
        }

        return body.ToString();
    }

    private string CreateTableHeaderCell(string content)
    {
        return $"<th style=\"text-align: left; padding: 10px;\">{content}</th>";
    }

    private string CreateTableDataCell(string content)
    {
        return $"<td style=\"text-align: left; padding: 10px;\">{content}</td>";
    }

    private string FormatDateTime(DateTime? dateTime)
    {
        if (dateTime.HasValue)
        {
            return dateTime.Value.ToString("G", CultureInfo.GetCultureInfo("de-CH"));
        }
        else
        {
            return string.Empty;
        }
    }
}
