using Geodatenbezug.Models;

namespace Geodatenbezug;

/// <summary>
/// Service for sending emails.
/// </summary>
public interface IMailService
{
    /// <summary>
    /// Sends an email with the processing results.
    /// </summary>
    void SendProcessingResults(List<ProcessingResult> results);
}
