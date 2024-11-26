using System.Net;
using System.Text;

namespace Geodatenbezug;

/// <summary>
/// A simplified version of the HttpResponseMessage class that can be used to mock HTTP responses.
/// </summary>
public record TestMessageResponse()
{
    /// <summary>
    /// The HTTP status code of the response.
    /// </summary>
    public required HttpStatusCode Code { get; set; }

    /// <summary>
    /// The content as a JSON string.
    /// </summary>
    public string? Content { get; set; }
}

/// <summary>
/// A test message handler that can be used to mock multiple HTTP responses.
/// </summary>
public class HttpTestMessageHandler : HttpMessageHandler
{
    private int attempts;

    private List<TestMessageResponse> testMessageResponses;

    /// <summary>
    /// Simulates the sending of a HTTP request and returns a predefined response.
    /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        var testMessageResponse = testMessageResponses[attempts];
        attempts++;
        var response = new HttpResponseMessage(testMessageResponse.Code);
        if (testMessageResponse.Content != null)
        {
            response.Content = new StringContent(testMessageResponse.Content, Encoding.UTF8, "application/json");
        }

        return response;
    }

    /// <summary>
    /// Sets the responses that the test message handler should return.
    /// </summary>
    public void SetTestMessageResponses(List<TestMessageResponse> responses)
    {
        testMessageResponses = responses;
    }

    /// <summary>
    /// Verifies that all responses have been used.
    /// </summary>
    public void VerifyNoOutstandingExpectation()
    {
        if (testMessageResponses != null && attempts != testMessageResponses.Count)
        {
            throw new InvalidOperationException("There are " + (testMessageResponses.Count - attempts) + " unfulfilled expectations");
        }
    }

    /// <summary>
    /// Converts the test message handler to an HttpClient.
    /// </summary>
    public HttpClient ToHttpClient() => new(this);
}
