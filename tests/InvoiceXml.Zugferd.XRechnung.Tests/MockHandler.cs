using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace InvoiceXml.Zugferd.XRechnung.Tests;

/// <summary>A captured request, read while the request was still alive.</summary>
internal sealed record CapturedRequest(
    HttpMethod Method,
    Uri Uri,
    AuthenticationHeaderValue? Authorization,
    string? UserAgent,
    string? ContentType,
    byte[] Body,
    IReadOnlyList<CapturedPart> Parts)
{
    public string BodyText => Encoding.UTF8.GetString(Body);

    public CapturedPart Part(string name) => Parts.Single(p => p.Name == name);
}

internal sealed record CapturedPart(string? Name, string? FileName, string? ContentType, byte[] Body)
{
    public string Text => Encoding.UTF8.GetString(Body);
}

/// <summary>Replies with queued responses and records every request.</summary>
internal sealed class MockHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public List<CapturedRequest> Requests { get; } = new();

    public MockHandler Reply(Func<HttpResponseMessage> response)
    {
        _responses.Enqueue(response);
        return this;
    }

    public MockHandler ReplyJson(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        Reply(() => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, status == HttpStatusCode.OK ? "application/json" : "application/problem+json"),
        });

    public MockHandler ReplyFile(byte[] content, string contentType, string fileName) =>
        Reply(() =>
        {
            var body = new ByteArrayContent(content);
            body.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            body.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment") { FileName = $"\"{fileName}\"", FileNameStar = fileName };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = body };
        });

    public MockHandler ReplyTooManyRequests(TimeSpan? retryAfter)
    {
        return Reply(() =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)429)
            {
                Content = new StringContent(
                    """{"title":"Too many requests","status":429,"detail":"Retry after the number of seconds given in the Retry-After header.","errorCode":4019}""",
                    Encoding.UTF8,
                    "application/problem+json"),
            };
            if (retryAfter is { } delta)
                response.Headers.RetryAfter = new RetryConditionHeaderValue(delta);
            return response;
        });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parts = new List<CapturedPart>();
        byte[] body = [];
        if (request.Content is MultipartFormDataContent multipart)
        {
            foreach (var part in multipart)
            {
                parts.Add(new CapturedPart(
                    part.Headers.ContentDisposition?.Name?.Trim('"'),
                    part.Headers.ContentDisposition?.FileName?.Trim('"'),
                    part.Headers.ContentType?.MediaType,
                    await ReadAsync(part, cancellationToken)));
            }
        }
        else if (request.Content is not null)
        {
            body = await ReadAsync(request.Content, cancellationToken);
        }

        Requests.Add(new CapturedRequest(
            request.Method,
            request.RequestUri!,
            request.Headers.Authorization,
            request.Headers.UserAgent.ToString(),
            request.Content?.Headers.ContentType?.MediaType,
            body,
            parts));

        if (_responses.Count == 0)
            throw new InvalidOperationException("No response queued.");
        return _responses.Dequeue()();
    }

    private static Task<byte[]> ReadAsync(HttpContent content, CancellationToken cancellationToken) =>
#if NET5_0_OR_GREATER
        content.ReadAsByteArrayAsync(cancellationToken);
#else
        content.ReadAsByteArrayAsync();
#endif
}
