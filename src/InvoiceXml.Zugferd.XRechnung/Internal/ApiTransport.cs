using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceXml.Zugferd.XRechnung.Internal;

/// <summary>
/// Sends requests to the API: authentication, user agent, 429 retries with Retry-After, and the
/// mapping of error responses to <see cref="InvoiceXmlApiException"/>.
/// </summary>
internal sealed class ApiTransport
{
    private static readonly ProductInfoHeaderValue UserAgent = new("InvoiceXml.Zugferd.XRechnung", PackageVersion());

    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly Uri _baseAddress;
    private readonly int _maxRetries;
    private readonly TimeSpan _maxRetryDelay;

    public ApiTransport(HttpClient http, InvoiceXmlClientOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            throw new ArgumentException("An InvoiceXML API key is required.", nameof(options));
        if (options.BaseAddress is null || !options.BaseAddress.IsAbsoluteUri)
            throw new ArgumentException("BaseAddress must be an absolute URI.", nameof(options));
        if (options.MaxRetries < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxRetries cannot be negative.");

        _http = http;
        _apiKey = options.ApiKey!.Trim();
        var baseText = options.BaseAddress.AbsoluteUri;
        _baseAddress = new Uri(baseText.EndsWith("/", StringComparison.Ordinal) ? baseText : baseText + "/");
        _maxRetries = options.MaxRetries;
        _maxRetryDelay = options.MaxRetryDelay;
    }

    /// <summary>Waits between retries. Replaceable in tests.</summary>
    internal Func<TimeSpan, CancellationToken, Task> Delay { get; set; } = Task.Delay;

    public async Task<HttpResponseMessage> PostAsync(string path, Func<HttpContent> createContent, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage response;
            using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_baseAddress, path)))
            {
                request.Content = createContent();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Headers.UserAgent.Add(UserAgent);
                response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
            }

            if ((int)response.StatusCode != 429)
                return response;

            var retryAfter = RetryAfter(response);
            var wait = retryAfter ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));
            if (attempt >= _maxRetries || wait > _maxRetryDelay)
            {
                using (response)
                {
                    var body = await ReadStringAsync(response, cancellationToken).ConfigureAwait(false);
                    var problem = TryParseProblem(body);
                    throw new InvoiceXmlRateLimitException(
                        $"InvoiceXML API returned 429 Too Many Requests after {attempt + 1} attempt(s).",
                        retryAfter,
                        problem?.Detail,
                        body,
                        problem?.TraceId);
                }
            }

            response.Dispose();
            await Delay(wait, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task<InvoiceFile> ReadFileAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        using (response)
        {
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
            var bytes = await ReadBytesAsync(response, cancellationToken).ConfigureAwait(false);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var disposition = response.Content.Headers.ContentDisposition;
            var fileName = (disposition?.FileNameStar ?? disposition?.FileName)?.Trim('"');
            return new InvoiceFile(bytes, contentType, string.IsNullOrEmpty(fileName) ? null : fileName);
        }
    }

    public static async Task<T> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        where T : class
    {
        using (response)
        {
            await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
            var bytes = await ReadBytesAsync(response, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<T>(bytes, InvoiceXmlJson.Options)
                ?? throw new InvoiceXmlApiException(response.StatusCode, "InvoiceXML API returned an empty response.");
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await ReadStringAsync(response, cancellationToken).ConfigureAwait(false);
        var problem = TryParseProblem(body);
        var findings = problem?.Errors?.ToArray() ?? Array.Empty<ValidationFinding>();
        throw new InvoiceXmlApiException(
            response.StatusCode,
            BuildMessage(response.StatusCode, problem, findings, body),
            problem?.ErrorCode,
            problem?.Title,
            problem?.Detail,
            findings,
            body,
            problem?.TraceId);
    }

    private static string BuildMessage(HttpStatusCode status, ProblemBody? problem, ValidationFinding[] findings, string body)
    {
        var message = new StringBuilder()
            .Append("InvoiceXML API returned ")
            .Append(((int)status).ToString(CultureInfo.InvariantCulture));

        if (problem?.ErrorCode is { } code)
            message.Append(" (error ").Append(code.ToString(CultureInfo.InvariantCulture)).Append(')');

        var text = problem?.Detail ?? problem?.Title;
        if (!string.IsNullOrWhiteSpace(text))
            message.Append(": ").Append(text);
        else if (problem is null && !string.IsNullOrWhiteSpace(body))
            message.Append(": ").Append(body.Length > 300 ? body.Substring(0, 300) : body);

        foreach (var finding in findings.Take(3))
            message.Append(Environment.NewLine).Append("  ").Append(finding);
        if (findings.Length > 3)
            message.Append(Environment.NewLine).Append("  and ").Append(findings.Length - 3).Append(" more");

        return message.ToString();
    }

    private static ProblemBody? TryParseProblem(string body)
    {
        if (string.IsNullOrWhiteSpace(body) || body.TrimStart()[0] != '{')
            return null;
        try
        {
            return JsonSerializer.Deserialize<ProblemBody>(body, InvoiceXmlJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TimeSpan? RetryAfter(HttpResponseMessage response)
    {
        var header = response.Headers.RetryAfter;
        if (header?.Delta is { } delta)
            return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
        if (header?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait < TimeSpan.Zero ? TimeSpan.Zero : wait;
        }

        return null;
    }

    private static Task<byte[]> ReadBytesAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
#if NET5_0_OR_GREATER
        response.Content.ReadAsByteArrayAsync(cancellationToken);
#else
        response.Content.ReadAsByteArrayAsync();
#endif

    private static Task<string> ReadStringAsync(HttpResponseMessage response, CancellationToken cancellationToken) =>
#if NET5_0_OR_GREATER
        response.Content.ReadAsStringAsync(cancellationToken);
#else
        response.Content.ReadAsStringAsync();
#endif

    private static string PackageVersion()
    {
        var version = typeof(ApiTransport).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
        var plus = version.IndexOf('+');
        return plus > 0 ? version.Substring(0, plus) : version;
    }
}

/// <summary>Builds request bodies.</summary>
internal static class RequestContent
{
    public static Func<HttpContent> JsonBody(object body)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(body, body.GetType(), InvoiceXmlJson.Options);
        return () =>
        {
            var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
            return content;
        };
    }

    /// <summary>A multipart form, rebuilt per attempt from buffered bytes.</summary>
    public static Func<HttpContent> Form(params FormPart[] parts) => () =>
    {
        var form = new MultipartFormDataContent();
        foreach (var part in parts)
        {
            if (part.File is { } file)
            {
                var content = new ByteArrayContent(file);
                content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeOf(file));
                form.Add(content, part.Name, part.FileName ?? DefaultFileName(file));
            }
            else if (part.Value is not null)
            {
                form.Add(new StringContent(part.Value), part.Name);
            }
        }

        return form;
    };

    public static string MediaTypeOf(byte[] file) => IsPdf(file) ? "application/pdf" : "application/xml";

    public static string DefaultFileName(byte[] file) => IsPdf(file) ? "invoice.pdf" : "invoice.xml";

    public static async Task<byte[]> BufferAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, 81920, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    // A PDF starts with "%PDF", possibly after a few bytes of junk the spec tolerates.
    private static bool IsPdf(byte[] file)
    {
        var limit = Math.Min(file.Length - 4, 1024);
        for (var i = 0; i <= limit; i++)
        {
            if (file[i] == '%' && file[i + 1] == 'P' && file[i + 2] == 'D' && file[i + 3] == 'F')
                return true;
        }

        return false;
    }
}

/// <summary>One part of a multipart form: a file or a text value.</summary>
internal readonly struct FormPart
{
    private FormPart(string name, byte[]? file, string? fileName, string? value)
    {
        Name = name;
        File = file;
        FileName = fileName;
        Value = value;
    }

    public string Name { get; }

    public byte[]? File { get; }

    public string? FileName { get; }

    public string? Value { get; }

    public static FormPart FromFile(string name, byte[] file, string? fileName) => new(name, file, fileName, null);

    public static FormPart FromValue(string name, string? value) => new(name, null, null, value);
}
