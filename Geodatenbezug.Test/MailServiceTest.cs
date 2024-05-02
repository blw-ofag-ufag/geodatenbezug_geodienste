using System.Net;
using Geodatenbezug.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Geodatenbezug;

[TestClass]
public class MailServiceTest
{
    private Mock<ILogger<MailService>> loggerMock;
    private MailService mailService;

    [TestInitialize]
    public void Initialize()
    {
        loggerMock = new Mock<ILogger<MailService>>(MockBehavior.Strict);
        mailService = new MailService(loggerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        loggerMock.VerifyAll();
    }

    [TestMethod]
    public void BuildMailMessage()
    {
        var results = new List<ProcessingResult>
        {
            new ()
            {
                Code = HttpStatusCode.OK,
                Reason = "Success",
                Info = "Data processed successfully",
                TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                Canton = Canton.SH,
                UpdatedAt = new DateTime(2023, 11, 05, 15, 33, 22),
                DownloadUrl = "https://a-test.ch/link.zip",
            },
        };

        using var mailMessage = mailService.BuildMailMessage(results);
        Assert.AreEqual("notifications@geowerkstatt.ch", mailMessage.From.Address);
        Assert.AreEqual("geodaten@blw.admin.ch", mailMessage.To.First().Address);
        Assert.AreEqual(0, mailMessage.CC.Count);
        Assert.AreEqual("Geodatenbezug Prozessierungsresultate", mailMessage.Subject);
    }

    [TestMethod]
    public void BuildMailMessageWithNotOkProcessingResult()
    {
        var results = new List<ProcessingResult>
        {
            new ()
            {
                Code = HttpStatusCode.OK,
                Reason = "Success",
                Info = "Data processed successfully",
                TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                Canton = Canton.SH,
                UpdatedAt = new DateTime(2023, 11, 05, 15, 33, 22),
                DownloadUrl = "https://a-test.ch/link.zip",
            },
            new ()
            {
                Code = HttpStatusCode.NotFound,
                Reason = "Kein Wert für Key SH gefunden",
                TopicTitle = "Perimeter Terrassenreben",
                Canton = Canton.SH,
                UpdatedAt = new DateTime(2023, 11, 05, 15, 33, 22),
            },
        };

        using var mailMessage = mailService.BuildMailMessage(results);
        Assert.AreEqual("notifications@geowerkstatt.ch", mailMessage.From.Address);
        Assert.AreEqual("geodaten@blw.admin.ch", mailMessage.To.First().Address);
        Assert.AreEqual(1, mailMessage.CC.Count);
        Assert.AreEqual("support@geowerkstatt.ch", mailMessage.CC.First().Address);
        Assert.AreEqual("Geodatenbezug Prozessierungsresultate", mailMessage.Subject);
    }

    [TestMethod]
    public void BuildMailBody()
    {
        var expectedMailBody = @"<p>Guten Tag</p>
<p>Die folgenden Themen wurden erfolgreich prozessiert:</p>
<table>
<tr>
<th style=""text-align: left; padding: 10px;"">Thema</th>
<th style=""text-align: left; padding: 10px;"">Kanton</th>
<th style=""text-align: left; padding: 10px;"">Aktualisiert am</th>
<th style=""text-align: left; padding: 10px;""></th>
</tr>
<tr>
<td style=""text-align: left; padding: 10px;"">Perimeter LN- und Sömmerungsflächen</td>
<td style=""text-align: left; padding: 10px;"">SH</td>
<td style=""text-align: left; padding: 10px;"">05.11.2023 15:33:22</td>
<td style=""text-align: left; padding: 10px;""><a href=""https://a-test.ch/link.zip"">Herunterladen</a></td>
</tr>
<tr>
<td style=""text-align: left; padding: 10px;"">Nutzungsflaechen</td>
<td style=""text-align: left; padding: 10px;"">SH</td>
<td style=""text-align: left; padding: 10px;""></td>
<td style=""text-align: left; padding: 10px;""><a href=""https://a-test.ch/link.zip"">Herunterladen</a></td>
</tr>
</table>
<br />
<p>Bei folgenden Themen traten während der Prozessierung Fehler auf:</p>
<table>
<tr>
<th style=""text-align: left; padding: 10px;"">Thema</th>
<th style=""text-align: left; padding: 10px;"">Kanton</th>
<th style=""text-align: left; padding: 10px;"">Aktualisiert am</th>
<th style=""text-align: left; padding: 10px;"">Fehler</th>
</tr>
<tr>
<td style=""text-align: left; padding: 10px;"">Rebbaukaster</td>
<td style=""text-align: left; padding: 10px;"">SH</td>
<td style=""text-align: left; padding: 10px;"">05.11.2023 15:33:22</td>
<td style=""text-align: left; padding: 10px;"">Not Found - Data export information not found. Invalid token?</td>
</tr>
<tr>
<td style=""text-align: left; padding: 10px;"">Perimeter Terrassenreben</td>
<td style=""text-align: left; padding: 10px;"">SH</td>
<td style=""text-align: left; padding: 10px;"">05.11.2023 15:33:22</td>
<td style=""text-align: left; padding: 10px;"">Kein Wert für Key SH gefunden</td>
</tr>
</table>
";

        var results = new List<ProcessingResult>
        {
            new ()
            {
                Code = HttpStatusCode.OK,
                Reason = "Success",
                Info = "Data processed successfully",
                TopicTitle = "Perimeter LN- und Sömmerungsflächen",
                Canton = Canton.SH,
                UpdatedAt = new DateTime(2023, 11, 05, 15, 33, 22),
                DownloadUrl = "https://a-test.ch/link.zip",
            },
            new ()
            {
                Code = HttpStatusCode.NotFound,
                Reason = "Not Found",
                Info = GeodiensteExportError.InvalidToken,
                TopicTitle = "Rebbaukaster",
                Canton = Canton.SH,
                UpdatedAt = new DateTime(2023, 11, 05, 15, 33, 22),
            },
            new ()
            {
                Code = HttpStatusCode.NotFound,
                Reason = "Kein Wert für Key SH gefunden",
                TopicTitle = "Perimeter Terrassenreben",
                Canton = Canton.SH,
                UpdatedAt = new DateTime(2023, 11, 05, 15, 33, 22),
            },
            new ()
            {
                Code = HttpStatusCode.OK,
                Reason = "Success",
                Info = "Data processed successfully",
                TopicTitle = "Nutzungsflaechen",
                Canton = Canton.SH,
                UpdatedAt = null,
                DownloadUrl = "https://a-test.ch/link.zip",
            },
        };

        var body = mailService.BuildMailBody(results);
        Assert.AreEqual(expectedMailBody, body);
    }
}
