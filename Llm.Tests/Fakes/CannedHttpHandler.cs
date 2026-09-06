using System.Net;
using System.Text;

namespace FreeAIr.Llm.Tests.Fakes;

/// <summary>
/// Stands in for the server. It records the request the transport actually built - the whole point
/// of most of these tests - and answers with bytes the test wrote by hand, so a protocol detail can
/// be asserted without a model, a network or a timeout.
/// </summary>
public sealed class CannedHttpHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    /// <summary>The last request that reached the handler, kept so a test can assert on its body and headers.</summary>
    public HttpRequestMessage? LastRequest
    {
        get;
        private set;
    }

    /// <summary>The body of the last request, already read out - the message's own content stream is consumed by then.</summary>
    public string LastRequestBody
    {
        get;
        private set;
    } = string.Empty;

    public CannedHttpHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responder
        )
    {
        _responder = responder;
    }

    /// <summary>Answers with a `text/event-stream` body, the way both protocols stream an answer.</summary>
    public static CannedHttpHandler ServerSentEvents(
        string body
        )
    {
        return new CannedHttpHandler(
            _ =>
            {
                var content = new StringContent(body, Encoding.UTF8);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/event-stream");

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = content,
                };
            });
    }

    /// <summary>Answers with a failure and the body the provider would have put its complaint in.</summary>
    public static CannedHttpHandler Failure(
        HttpStatusCode statusCode,
        string body
        )
    {
        return new CannedHttpHandler(
            _ => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            });
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
        )
    {
        LastRequest = request;
        LastRequestBody = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);

        return _responder(request);
    }
}
