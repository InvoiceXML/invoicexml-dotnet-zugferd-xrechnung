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
    /// A request turned away with 429 is never billed, so retrying it is safe.
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

/// <summary>Options for <see cref="ZugferdXRechnungClient.RenderXRechnungToPdfAsync(byte[], RenderOptions?, string?, System.Threading.CancellationToken)"/>.</summary>
public sealed class RenderOptions
{
    /// <summary>Language of the rendered PDF. Defaults to English.</summary>
    public PdfLanguage? Language { get; set; }

    /// <summary>Public http(s) URL of the seller's logo (PNG or JPEG, at most 2 MB).</summary>
#pragma warning disable CA1056 // Kept as string to match the API field and accept any URL text.
    public string? LogoUrl { get; set; }
#pragma warning restore CA1056
}
