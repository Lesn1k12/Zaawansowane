using System.Net;
using System.Text;

namespace OrderFlow.Tests.Helpers;

/// <summary>
/// Intercepts outgoing <see cref="HttpClient"/> requests in unit tests.
/// Pass a factory that maps each <see cref="HttpRequestMessage"/> to a
/// canned <see cref="HttpResponseMessage"/>.
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

    public int CallCount { get; private set; }
    public HttpRequestMessage? LastRequest { get; private set; }

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> factory)
        => _factory = factory;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CallCount++;
        LastRequest = request;
        return Task.FromResult(_factory(request));
    }

    // ── Static factory helpers ─────────────────────────────────────────────────

    public static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    public static HttpResponseMessage Status(HttpStatusCode status)
        => new(status);
}
