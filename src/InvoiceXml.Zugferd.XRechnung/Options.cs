using System;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>Settings for <see cref="ZugferdXRechnungClient"/>.</summary>
public sealed class InvoiceXmlClientOptions
{
    /// <summary>The default API address, <c>https://api.invoicexml.com/</c>.</summary>
    public static readonly Uri DefaultBaseAddress = new("https://api.invoicexml.com/");

    /// <summary>Your InvoiceXML API key, sent as <c>Authorization: Bearer &lt;key&gt;</c>. Required.</summary>
    public string? ApiKey { get; set; }

    /// <summary>API address. Defaults to <see cref="DefaultBaseAddress"/>.</summary>
    public Uri BaseAddress { get; set; } = DefaultBaseAddress;

    /// <summary>
    /// How often a request answered with HTTP 429 (Too Many Requests) is retried before an
    /// <see cref="InvoiceXmlRateLimitException"/> is thrown. Defaults to 3; 0 disables retries.
    /// Transform calls are never retried. A 429 is not billed.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// The longest wait the client accepts between retries. When the API asks for a longer wait
    /// in its <c>Retry-After</c> header, the client stops retrying and throws instead.
    /// Defaults to 30 seconds.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>Options for <see cref="ZugferdXRechnungClient.CreateZugferdAsync"/>.</summary>
public sealed class ZugferdOptions
{
    /// <summary>Language of the rendered PDF and of default texts. Defaults to English.</summary>
    public PdfLanguage? Language { get; set; }

    /// <summary>
    /// Brand colour for headings and accents of the rendered PDF, as <c>#RRGGBB</c>.
    /// Body text is never recoloured.
    /// </summary>
    public string? BrandColor { get; set; }

    /// <summary>
    /// Public http(s) URL of your own PDF to use as the visual layer. The API downloads it and
    /// embeds the generated XML into it instead of rendering its own template; language, brand
    /// colour and logo are then ignored.
    /// </summary>
#pragma warning disable CA1056 // Kept as string to match the API field and accept any URL text.
    public string? PdfUrl { get; set; }

    /// <summary>
    /// Public http(s) URL of the seller's logo (PNG or JPEG, at most 2 MB), printed top-left on
    /// the rendered PDF.
    /// </summary>
    public string? LogoUrl { get; set; }
#pragma warning restore CA1056

    /// <summary>
    /// When true, the rendered PDF gets an extra section listing the technical e-invoice
    /// properties that a classic invoice layout does not show. Defaults to false.
    /// </summary>
    public bool? IncludeAdvancedProperties { get; set; }
}

/// <summary>Options for <see cref="ZugferdXRechnungClient.CreateXRechnungAsync"/>.</summary>
public sealed class XRechnungOptions
{
    /// <summary>XML syntax of the created XRechnung. Defaults to <see cref="XRechnungSyntax.Ubl"/>.</summary>
    public XRechnungSyntax? Syntax { get; set; }

    /// <summary>
    /// Language of the default texts the API writes when you leave them empty, such as payment
    /// terms. Defaults to English.
    /// </summary>
    public PdfLanguage? Language { get; set; }
}

/// <summary>
/// Options for <see cref="ZugferdXRechnungClient.RenderXRechnungToPdfAsync(byte[], RenderOptions?, string?, System.Threading.CancellationToken)"/>
/// and <see cref="ZugferdXRechnungClient.RenderCiiToPdfAsync(byte[], RenderOptions?, string?, System.Threading.CancellationToken)"/>.
/// </summary>
public sealed class RenderOptions
{
    /// <summary>Language of the rendered PDF. Defaults to English.</summary>
    public PdfLanguage? Language { get; set; }

    /// <summary>Public http(s) URL of the seller's logo (PNG or JPEG, at most 2 MB).</summary>
#pragma warning disable CA1056 // Kept as string to match the API field and accept any URL text.
    public string? LogoUrl { get; set; }
#pragma warning restore CA1056

    /// <summary>When true, the PDF prints the EPC payment QR code (GiroCode). Defaults to false.</summary>
    public bool? IncludeQrCode { get; set; }
}

/// <summary>
/// Options for the convert methods that return XML, such as
/// <see cref="ZugferdXRechnungClient.ConvertZugferdToUblAsync(byte[], ConvertOptions?, string?, System.Threading.CancellationToken)"/>.
/// </summary>
public sealed class ConvertOptions
{
    /// <summary>Language of the findings when the conversion is refused. Defaults to English.</summary>
    public PdfLanguage? ValidationLanguage { get; set; }
}

/// <summary>
/// Options for the convert methods that return a ZUGFeRD PDF, such as
/// <see cref="ZugferdXRechnungClient.ConvertCiiToZugferdAsync(byte[], ConvertToPdfOptions?, string?, System.Threading.CancellationToken)"/>.
/// </summary>
public sealed class ConvertToPdfOptions
{
    /// <summary>
    /// Language of the rendered PDF, and of the default texts added to the XML when the source
    /// has none. Defaults to English.
    /// </summary>
    public PdfLanguage? Language { get; set; }

    /// <summary>Language of the findings when the conversion is refused. Defaults to English.</summary>
    public PdfLanguage? ValidationLanguage { get; set; }
}

/// <summary>Options for <see cref="ZugferdXRechnungClient.TransformToZugferdAsync(byte[], TransformOptions?, string?, System.Threading.CancellationToken)"/>.</summary>
public sealed class TransformOptions
{
    /// <summary>
    /// Language of the rendered PDF and of the default texts in the XML. When null, the API uses
    /// the language the invoice is written in, and English when that is not one of the
    /// <see cref="PdfLanguage"/> values. With <see cref="PreserveOriginalPdf"/> set, it words the
    /// XML texts only.
    /// </summary>
    public PdfLanguage? Language { get; set; }

    /// <summary>
    /// When true, the uploaded PDF stays as the visual layer and the XML is embedded into it; its
    /// earlier attachments and form fields are not kept. Defaults to false, when the PDF is
    /// rendered fresh from the data read.
    /// </summary>
    public bool? PreserveOriginalPdf { get; set; }

    /// <summary>
    /// Whether the rendered PDF ends with a section listing the technical e-invoice properties, so
    /// they can be checked on the pages. Defaults to true; set false for the classic layout alone.
    /// Ignored with <see cref="PreserveOriginalPdf"/>.
    /// </summary>
    public bool? IncludeAdvancedProperties { get; set; }

    /// <summary>Language of the findings when the result would not be compliant. Defaults to English.</summary>
    public PdfLanguage? ValidationLanguage { get; set; }
}

/// <summary>Options for <see cref="ZugferdXRechnungClient.TransformToXRechnungAsync(byte[], string, TransformToXRechnungOptions?, string?, System.Threading.CancellationToken)"/>.</summary>
public sealed class TransformToXRechnungOptions
{
    /// <summary>Language of the findings when the result would not be compliant. Defaults to English.</summary>
    public PdfLanguage? ValidationLanguage { get; set; }
}
