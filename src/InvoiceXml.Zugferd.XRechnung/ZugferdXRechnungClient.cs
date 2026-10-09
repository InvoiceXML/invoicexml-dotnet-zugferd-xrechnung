using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using InvoiceXml.Zugferd.XRechnung.Internal;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// Client for the ZUGFeRD and XRechnung endpoints of the InvoiceXML API.
/// </summary>
/// <remarks>
/// <para>
/// The client is thread-safe; create one and reuse it. Pass your own <see cref="HttpClient"/>
/// (for example from <c>IHttpClientFactory</c>) to control its lifetime and handlers; otherwise
/// the client creates one and disposes it with itself.
/// </para>
/// <para>
/// Requests answered with HTTP 429 are retried after the interval in the <c>Retry-After</c>
/// header, up to <see cref="InvoiceXmlClientOptions.MaxRetries"/> times. Uploads are buffered in
/// memory so a retry can resend them.
/// </para>
/// </remarks>
public sealed class ZugferdXRechnungClient : IZugferdXRechnungClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttpClient;

    /// <summary>Creates a client with its own <see cref="HttpClient"/>.</summary>
    /// <param name="apiKey">Your InvoiceXML API key.</param>
    public ZugferdXRechnungClient(string apiKey)
        : this(new InvoiceXmlClientOptions { ApiKey = apiKey })
    {
    }

    /// <summary>Creates a client with its own <see cref="HttpClient"/>.</summary>
    /// <param name="options">Client settings, including the API key.</param>
    public ZugferdXRechnungClient(InvoiceXmlClientOptions options)
        : this(new HttpClient(), options, ownsHttpClient: true)
    {
    }

    /// <summary>Creates a client on an existing <see cref="HttpClient"/>, which is not disposed by the client.</summary>
    /// <param name="httpClient">The HTTP client to send requests with.</param>
    /// <param name="apiKey">Your InvoiceXML API key.</param>
    public ZugferdXRechnungClient(HttpClient httpClient, string apiKey)
        : this(httpClient, new InvoiceXmlClientOptions { ApiKey = apiKey })
    {
    }

    /// <summary>Creates a client on an existing <see cref="HttpClient"/>, which is not disposed by the client.</summary>
    /// <param name="httpClient">The HTTP client to send requests with.</param>
    /// <param name="options">Client settings, including the API key.</param>
    public ZugferdXRechnungClient(HttpClient httpClient, InvoiceXmlClientOptions options)
        : this(httpClient, options, ownsHttpClient: false)
    {
    }

    private ZugferdXRechnungClient(HttpClient httpClient, InvoiceXmlClientOptions options, bool ownsHttpClient)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (options is null) throw new ArgumentNullException(nameof(options));
        _ownsHttpClient = ownsHttpClient;
        Transport = new ApiTransport(_http, options);
    }

    internal ApiTransport Transport { get; }

    /// <inheritdoc />
    public async Task<InvoiceFile> CreateZugferdAsync(InvoiceDocument invoice, ZugferdOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (invoice is null) throw new ArgumentNullException(nameof(invoice));
        var body = RequestContent.JsonBody(new CreateInvoiceRequestBody { Invoice = invoice, Options = InvoiceOptionsBody.From(options) });
        var response = await Transport.PostAsync("v1/create/zugferd", body, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InvoiceFile> CreateXRechnungAsync(InvoiceDocument invoice, XRechnungOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (invoice is null) throw new ArgumentNullException(nameof(invoice));
        var body = RequestContent.JsonBody(new CreateInvoiceRequestBody { Invoice = invoice, Options = InvoiceOptionsBody.From(options) });
        var response = await Transport.PostAsync("v1/create/xrechnung", body, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<InvoiceValidationResult> ValidateZugferdAsync(byte[] pdf, string? fileName = null, CancellationToken cancellationToken = default) =>
        PostFileForJsonAsync<InvoiceValidationResult>("v1/validate/zugferd", pdf, nameof(pdf), fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceValidationResult> ValidateZugferdAsync(Stream pdf, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ValidateZugferdAsync(await RequestContent.BufferAsync(pdf, cancellationToken).ConfigureAwait(false), fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceValidationResult> ValidateXRechnungAsync(byte[] xml, string? fileName = null, CancellationToken cancellationToken = default) =>
        PostFileForJsonAsync<InvoiceValidationResult>("v1/validate/xrechnung", xml, nameof(xml), fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceValidationResult> ValidateXRechnungAsync(Stream xml, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ValidateXRechnungAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<InvoiceFile> EmbedZugferdAsync(byte[] pdf, byte[] xml, bool skipValidation = false, string? fileName = null, CancellationToken cancellationToken = default)
    {
        if (pdf is null) throw new ArgumentNullException(nameof(pdf));
        if (xml is null) throw new ArgumentNullException(nameof(xml));
        var form = RequestContent.Form(
            FormPart.FromFile("pdf", pdf, fileName ?? "invoice.pdf"),
            FormPart.FromFile("xml", xml, "invoice.xml"),
            FormPart.FromValue("skipValidation", skipValidation ? "true" : null));
        var response = await Transport.PostAsync("v1/embed/zugferd", form, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InvoiceFile> EmbedZugferdAsync(Stream pdf, Stream xml, bool skipValidation = false, string? fileName = null, CancellationToken cancellationToken = default)
    {
        var pdfBytes = await RequestContent.BufferAsync(pdf, cancellationToken).ConfigureAwait(false);
        var xmlBytes = await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false);
        return await EmbedZugferdAsync(pdfBytes, xmlBytes, skipValidation, fileName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InvoiceFile> RenderXRechnungToPdfAsync(byte[] xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default)
    {
        if (xml is null) throw new ArgumentNullException(nameof(xml));
        var form = RequestContent.Form(
            FormPart.FromFile("file", xml, fileName),
            FormPart.FromValue("language", options?.Language is { } language ? EnumWire<PdfLanguage>.ToWire(language) : null),
            FormPart.FromValue("logoUrl", options?.LogoUrl));
        var response = await Transport.PostAsync("v1/render/xrechnung/to/pdf", form, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InvoiceFile> RenderXRechnungToPdfAsync(Stream xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await RenderXRechnungToPdfAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<InvoiceDocument> ExtractJsonAsync(byte[] file, string? fileName = null, CancellationToken cancellationToken = default)
    {
        var result = await PostFileForJsonAsync<InvoiceJsonResponseBody>("v1/extract/json", file, nameof(file), fileName, cancellationToken).ConfigureAwait(false);
        return result.Invoice ?? throw new InvoiceXmlApiException(System.Net.HttpStatusCode.OK, "InvoiceXML API returned no invoice.");
    }

    /// <inheritdoc />
    public async Task<InvoiceDocument> ExtractJsonAsync(Stream file, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ExtractJsonAsync(await RequestContent.BufferAsync(file, cancellationToken).ConfigureAwait(false), fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<InvoiceFile> ExtractAttachmentsAsync(byte[] file, string? fileName = null, CancellationToken cancellationToken = default)
    {
        if (file is null) throw new ArgumentNullException(nameof(file));
        var form = RequestContent.Form(FormPart.FromFile("file", file, fileName));
        var response = await Transport.PostAsync("v1/extract/attachments", form, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InvoiceFile> ExtractAttachmentsAsync(Stream file, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ExtractAttachmentsAsync(await RequestContent.BufferAsync(file, cancellationToken).ConfigureAwait(false), fileName, cancellationToken).ConfigureAwait(false);

    /// <summary>Disposes the <see cref="HttpClient"/> if the client created it.</summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
            _http.Dispose();
    }

    private async Task<T> PostFileForJsonAsync<T>(string path, byte[] file, string parameterName, string? fileName, CancellationToken cancellationToken)
        where T : class
    {
        if (file is null) throw new ArgumentNullException(parameterName);
        var form = RequestContent.Form(FormPart.FromFile("file", file, fileName));
        var response = await Transport.PostAsync(path, form, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadJsonAsync<T>(response, cancellationToken).ConfigureAwait(false);
    }
}
