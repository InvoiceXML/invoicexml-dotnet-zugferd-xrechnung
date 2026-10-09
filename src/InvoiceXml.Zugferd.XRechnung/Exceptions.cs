using System;
using System.Collections.Generic;
using System.Net;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// Thrown when the API refuses a request. When a create or embed request is refused because the
/// invoice would not be compliant, <see cref="ErrorCode"/> is
/// <see cref="InvoiceXmlErrorCode.XmlValidation"/> or <see cref="InvoiceXmlErrorCode.ModelValidation"/>
/// and <see cref="Findings"/> lists every violated rule with the fields to correct.
/// </summary>
#pragma warning disable CA1032 // The API error needs its status and body; the parameterless constructors would be meaningless.
public class InvoiceXmlApiException : Exception
#pragma warning restore CA1032
{
    /// <summary>Creates the exception. Useful when faking <see cref="IZugferdXRechnungClient"/> in tests.</summary>
    /// <param name="statusCode">HTTP status code of the response.</param>
    /// <param name="message">Exception message.</param>
    /// <param name="errorCode">The API's numeric error code, if any.</param>
    /// <param name="title">Short error title from the response.</param>
    /// <param name="detail">Error detail from the response.</param>
    /// <param name="findings">Violated rules, if any.</param>
    /// <param name="responseBody">Raw response body.</param>
    /// <param name="traceId">Trace identifier to quote when contacting support.</param>
    public InvoiceXmlApiException(
        HttpStatusCode statusCode,
        string message,
        int? errorCode = null,
        string? title = null,
        string? detail = null,
        IReadOnlyList<ValidationFinding>? findings = null,
        string? responseBody = null,
        string? traceId = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Title = title;
        Detail = detail;
        Findings = findings ?? Array.Empty<ValidationFinding>();
        ResponseBody = responseBody;
        TraceId = traceId;
    }

    /// <summary>HTTP status code of the response.</summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The API's numeric error code, for example 4001. Compare it with
    /// <see cref="InvoiceXmlErrorCode"/>. Null when the response carried none.
    /// </summary>
    public int? ErrorCode { get; }

    /// <summary>Short error title from the response.</summary>
    public string? Title { get; }

    /// <summary>Error detail from the response.</summary>
    public string? Detail { get; }

    /// <summary>Violated rules with the fields to correct. Empty for errors that are not rule violations.</summary>
    public IReadOnlyList<ValidationFinding> Findings { get; }

    /// <summary>Raw response body, for logging.</summary>
    public string? ResponseBody { get; }

    /// <summary>Trace identifier of the request. Quote it when contacting support.</summary>
    public string? TraceId { get; }

    /// <summary>True when the request was refused because the invoice would not be compliant.</summary>
    public bool IsValidationError =>
        ErrorCode is (int)InvoiceXmlErrorCode.XmlValidation or (int)InvoiceXmlErrorCode.ModelValidation;
}

/// <summary>
/// Thrown when the API still answers HTTP 429 (Too Many Requests) after the configured retries,
/// or asks for a longer wait than <see cref="InvoiceXmlClientOptions.MaxRetryDelay"/>.
/// </summary>
#pragma warning disable CA1032 // See InvoiceXmlApiException.
public sealed class InvoiceXmlRateLimitException : InvoiceXmlApiException
#pragma warning restore CA1032
{
    /// <summary>Creates the exception.</summary>
    /// <param name="message">Exception message.</param>
    /// <param name="retryAfter">The wait the API asked for, if any.</param>
    /// <param name="detail">Error detail from the response.</param>
    /// <param name="responseBody">Raw response body.</param>
    /// <param name="traceId">Trace identifier of the request.</param>
    public InvoiceXmlRateLimitException(
        string message,
        TimeSpan? retryAfter,
        string? detail = null,
        string? responseBody = null,
        string? traceId = null)
        : base((HttpStatusCode)429, message, (int)InvoiceXmlErrorCode.TooManyRequests, "Too many requests", detail, null, responseBody, traceId)
    {
        RetryAfter = retryAfter;
    }

    /// <summary>How long the API asked to wait before the next attempt, from the <c>Retry-After</c> header.</summary>
    public TimeSpan? RetryAfter { get; }
}

/// <summary>
/// The API's documented error codes, carried in <see cref="InvoiceXmlApiException.ErrorCode"/>.
/// </summary>
public enum InvoiceXmlErrorCode
{
    /// <summary>An unexpected error.</summary>
    Unknown = 4000,

    /// <summary>The invoice breaks validation rules. <see cref="InvoiceXmlApiException.Findings"/> lists them.</summary>
    XmlValidation = 4001,

    /// <summary>A request field is missing or malformed. <see cref="InvoiceXmlApiException.Findings"/> lists the fields.</summary>
    ModelValidation = 4002,

    /// <summary>No file was uploaded.</summary>
    MissingFile = 4003,

    /// <summary>The uploaded file, or a file fetched from a URL, has the wrong type.</summary>
    UnsupportedContentType = 4004,

    /// <summary>The file exceeds the size limit.</summary>
    FileTooLarge = 4005,

    /// <summary>The PDF contains no embedded invoice XML.</summary>
    NoEmbeddedXml = 4006,

    /// <summary>The PDF could not be processed, for example because it is corrupted or password-protected.</summary>
    PdfError = 4007,

    /// <summary>The document is not an invoice.</summary>
    NotAnInvoice = 4008,

    /// <summary>The document contains more than one invoice.</summary>
    MultipleInvoices = 4009,

    /// <summary>The API key is missing, malformed, unknown or revoked.</summary>
    Unauthorized = 4010,

    /// <summary>A PDF or logo URL could not be fetched.</summary>
    RemoteFetchError = 4011,

    /// <summary>The invoice carries no embedded attachments (HTTP 404).</summary>
    NoAttachments = 4013,

    /// <summary>An embedded attachment could not be decoded.</summary>
    InvalidAttachment = 4014,

    /// <summary>The supplied PDF uses a font that cannot be embedded, so it cannot become PDF/A-3.</summary>
    UnembeddableFont = 4015,

    /// <summary>The host of a PDF or logo URL refused access (HTTP 403).</summary>
    RemoteFetchForbidden = 4016,

    /// <summary>The declared specification identifier is not an official ZUGFeRD profile.</summary>
    UnsupportedZugferdProfile = 4017,

    /// <summary>Too many concurrent requests. Retry after the <c>Retry-After</c> interval.</summary>
    TooManyRequests = 4019,
}
