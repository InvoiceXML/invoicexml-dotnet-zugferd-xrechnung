using System;
using System.Collections.Generic;
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
/// header, up to <see cref="InvoiceXmlClientOptions.MaxRetries"/> times, except transform calls.
/// Uploads are buffered in memory so a retry can resend them.
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
    public async Task<InvoiceFile> CreateCiiAsync(InvoiceDocument invoice, CiiOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (invoice is null) throw new ArgumentNullException(nameof(invoice));
        var body = RequestContent.JsonBody(new CreateInvoiceRequestBody { Invoice = invoice, Options = InvoiceOptionsBody.From(options) });
        var response = await Transport.PostAsync("v1/create/cii", body, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InvoiceFile> CreateUblAsync(InvoiceDocument invoice, UblOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (invoice is null) throw new ArgumentNullException(nameof(invoice));
        var body = RequestContent.JsonBody(new CreateInvoiceRequestBody { Invoice = invoice, Options = InvoiceOptionsBody.From(options) });
        var response = await Transport.PostAsync("v1/create/ubl", body, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EmailSendResult> EmailZugferdAsync(InvoiceDocument invoice, EmailMessage email, ZugferdOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (invoice is null) throw new ArgumentNullException(nameof(invoice));
        if (email is null) throw new ArgumentNullException(nameof(email));
        var body = RequestContent.JsonBody(new CreateInvoiceEmailRequestBody { Invoice = invoice, Options = InvoiceOptionsBody.From(options), Email = EmailBody.FromMessage(email) });
        var response = await Transport.PostAsync("v1/create/zugferd/email", body, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadJsonAsync<EmailSendResult>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EmailSendResult> EmailXRechnungAsync(InvoiceDocument invoice, EmailMessage email, XRechnungOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (invoice is null) throw new ArgumentNullException(nameof(invoice));
        if (email is null) throw new ArgumentNullException(nameof(email));
        var body = RequestContent.JsonBody(new CreateInvoiceEmailRequestBody { Invoice = invoice, Options = InvoiceOptionsBody.From(options), Email = EmailBody.FromMessage(email) });
        var response = await Transport.PostAsync("v1/create/xrechnung/email", body, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadJsonAsync<EmailSendResult>(response, cancellationToken).ConfigureAwait(false);
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
    public async Task<EmailSendResult> EmailEmbeddedZugferdAsync(byte[] pdf, byte[] xml, EmailMessage email, string? fileName = null, CancellationToken cancellationToken = default)
    {
        if (pdf is null) throw new ArgumentNullException(nameof(pdf));
        if (xml is null) throw new ArgumentNullException(nameof(xml));
        if (email is null) throw new ArgumentNullException(nameof(email));
        var parts = new List<FormPart>
        {
            FormPart.FromFile("pdf", pdf, fileName ?? "invoice.pdf"),
            FormPart.FromFile("xml", xml, "invoice.xml"),
        };
        parts.AddRange(EmailBody.FromMessage(email).FormParts());
        var response = await Transport.PostAsync("v1/embed/zugferd/email", RequestContent.Form(parts.ToArray()), cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadJsonAsync<EmailSendResult>(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<EmailSendResult> EmailEmbeddedZugferdAsync(Stream pdf, Stream xml, EmailMessage email, string? fileName = null, CancellationToken cancellationToken = default)
    {
        if (email is null) throw new ArgumentNullException(nameof(email));
        var pdfBytes = await RequestContent.BufferAsync(pdf, cancellationToken).ConfigureAwait(false);
        var xmlBytes = await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false);
        return await EmailEmbeddedZugferdAsync(pdfBytes, xmlBytes, email, fileName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<InvoiceFile> RenderXRechnungToPdfAsync(byte[] xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        RenderToPdfAsync("v1/render/xrechnung/to/pdf", xml, options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> RenderXRechnungToPdfAsync(Stream xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await RenderXRechnungToPdfAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> RenderCiiToPdfAsync(byte[] xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        RenderToPdfAsync("v1/render/cii/to/pdf", xml, options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> RenderCiiToPdfAsync(Stream xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await RenderCiiToPdfAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> ConvertZugferdToUblAsync(byte[] pdf, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        ConvertAsync("v1/convert/zugferd/to/ubl", pdf, nameof(pdf), options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> ConvertZugferdToUblAsync(Stream pdf, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ConvertZugferdToUblAsync(await RequestContent.BufferAsync(pdf, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> ConvertZugferdToCiiAsync(byte[] pdf, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        ConvertAsync("v1/convert/zugferd/to/cii", pdf, nameof(pdf), options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> ConvertZugferdToCiiAsync(Stream pdf, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ConvertZugferdToCiiAsync(await RequestContent.BufferAsync(pdf, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> ConvertXRechnungToUblAsync(byte[] xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        ConvertAsync("v1/convert/xrechnung/to/ubl", xml, nameof(xml), options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> ConvertXRechnungToUblAsync(Stream xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ConvertXRechnungToUblAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> ConvertXRechnungToCiiAsync(byte[] xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        ConvertAsync("v1/convert/xrechnung/to/cii", xml, nameof(xml), options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> ConvertXRechnungToCiiAsync(Stream xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ConvertXRechnungToCiiAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> ConvertUblToXRechnungAsync(byte[] xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        ConvertAsync("v1/convert/ubl/to/xrechnung", xml, nameof(xml), options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> ConvertUblToXRechnungAsync(Stream xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ConvertUblToXRechnungAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> ConvertCiiToXRechnungAsync(byte[] xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        ConvertAsync("v1/convert/cii/to/xrechnung", xml, nameof(xml), options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> ConvertCiiToXRechnungAsync(Stream xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ConvertCiiToXRechnungAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> ConvertCiiToZugferdAsync(byte[] xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        ConvertToPdfAsync("v1/convert/cii/to/zugferd", xml, options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> ConvertCiiToZugferdAsync(Stream xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ConvertCiiToZugferdAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> ConvertUblToZugferdAsync(byte[] xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        ConvertToPdfAsync("v1/convert/ubl/to/zugferd", xml, options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> ConvertUblToZugferdAsync(Stream xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ConvertUblToZugferdAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<InvoiceFile> ConvertXRechnungToZugferdAsync(byte[] xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        ConvertToPdfAsync("v1/convert/xrechnung/to/zugferd", xml, options, fileName, cancellationToken);

    /// <inheritdoc />
    public async Task<InvoiceFile> ConvertXRechnungToZugferdAsync(Stream xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await ConvertXRechnungToZugferdAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ValidationReportResult> ValidateZugferdReportAsync(byte[] pdf, string? fileName = null, PdfLanguage? validationLanguage = null, CancellationToken cancellationToken = default) =>
        ValidationReportAsync("v1/validate/zugferd/report", pdf, nameof(pdf), fileName, validationLanguage, cancellationToken);

    /// <inheritdoc />
    public async Task<ValidationReportResult> ValidateZugferdReportAsync(Stream pdf, string? fileName = null, PdfLanguage? validationLanguage = null, CancellationToken cancellationToken = default) =>
        await ValidateZugferdReportAsync(await RequestContent.BufferAsync(pdf, cancellationToken).ConfigureAwait(false), fileName, validationLanguage, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ValidationReportResult> ValidateXRechnungReportAsync(byte[] xml, string? fileName = null, PdfLanguage? validationLanguage = null, CancellationToken cancellationToken = default) =>
        ValidationReportAsync("v1/validate/xrechnung/report", xml, nameof(xml), fileName, validationLanguage, cancellationToken);

    /// <inheritdoc />
    public async Task<ValidationReportResult> ValidateXRechnungReportAsync(Stream xml, string? fileName = null, PdfLanguage? validationLanguage = null, CancellationToken cancellationToken = default) =>
        await ValidateXRechnungReportAsync(await RequestContent.BufferAsync(xml, cancellationToken).ConfigureAwait(false), fileName, validationLanguage, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<InvoiceFile> TransformToZugferdAsync(byte[] pdf, TransformOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default)
    {
        if (pdf is null) throw new ArgumentNullException(nameof(pdf));
        var form = RequestContent.Form(
            FormPart.FromFile("file", pdf, fileName),
            LanguagePart("language", options?.Language),
            FlagPart("preserveOriginalPdf", options?.PreserveOriginalPdf),
            FlagPart("includeAdvancedProperties", options?.IncludeAdvancedProperties),
            LanguagePart("validationLanguage", options?.ValidationLanguage));
        var response = await Transport.PostAsync("v1/transform/to/zugferd", form, cancellationToken, retryRateLimit: false).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InvoiceFile> TransformToZugferdAsync(Stream pdf, TransformOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await TransformToZugferdAsync(await RequestContent.BufferAsync(pdf, cancellationToken).ConfigureAwait(false), options, fileName, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<InvoiceFile> TransformToXRechnungAsync(byte[] pdf, string buyerReference, TransformToXRechnungOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default)
    {
        if (pdf is null) throw new ArgumentNullException(nameof(pdf));
        if (string.IsNullOrWhiteSpace(buyerReference)) throw new ArgumentException("A buyer reference is required.", nameof(buyerReference));
        var form = RequestContent.Form(
            FormPart.FromFile("file", pdf, fileName),
            FormPart.FromValue("buyerReference", buyerReference),
            LanguagePart("validationLanguage", options?.ValidationLanguage));
        var response = await Transport.PostAsync("v1/transform/to/xrechnung", form, cancellationToken, retryRateLimit: false).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<InvoiceFile> TransformToXRechnungAsync(Stream pdf, string buyerReference, TransformToXRechnungOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default) =>
        await TransformToXRechnungAsync(await RequestContent.BufferAsync(pdf, cancellationToken).ConfigureAwait(false), buyerReference, options, fileName, cancellationToken).ConfigureAwait(false);

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

    private async Task<InvoiceFile> RenderToPdfAsync(string path, byte[] xml, RenderOptions? options, string? fileName, CancellationToken cancellationToken)
    {
        if (xml is null) throw new ArgumentNullException(nameof(xml));
        var form = RequestContent.Form(
            FormPart.FromFile("file", xml, fileName),
            LanguagePart("language", options?.Language),
            FormPart.FromValue("logoUrl", options?.LogoUrl),
            FlagPart("includeQrCode", options?.IncludeQrCode));
        var response = await Transport.PostAsync(path, form, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InvoiceFile> ConvertAsync(string path, byte[] file, string parameterName, ConvertOptions? options, string? fileName, CancellationToken cancellationToken)
    {
        if (file is null) throw new ArgumentNullException(parameterName);
        var form = RequestContent.Form(
            FormPart.FromFile("file", file, fileName),
            LanguagePart("validationLanguage", options?.ValidationLanguage));
        var response = await Transport.PostAsync(path, form, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<InvoiceFile> ConvertToPdfAsync(string path, byte[] xml, ConvertToPdfOptions? options, string? fileName, CancellationToken cancellationToken)
    {
        if (xml is null) throw new ArgumentNullException(nameof(xml));
        var form = RequestContent.Form(
            FormPart.FromFile("file", xml, fileName),
            LanguagePart("language", options?.Language),
            LanguagePart("validationLanguage", options?.ValidationLanguage));
        var response = await Transport.PostAsync(path, form, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadFileAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ValidationReportResult> ValidationReportAsync(string path, byte[] file, string parameterName, string? fileName, PdfLanguage? validationLanguage, CancellationToken cancellationToken)
    {
        if (file is null) throw new ArgumentNullException(parameterName);
        var form = RequestContent.Form(
            FormPart.FromFile("file", file, fileName),
            LanguagePart("validationLanguage", validationLanguage));
        var response = await Transport.PostAsync(path, form, cancellationToken).ConfigureAwait(false);
        return await ApiTransport.ReadValidationReportAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static FormPart LanguagePart(string name, PdfLanguage? language) =>
        FormPart.FromValue(name, language is { } value ? EnumWire<PdfLanguage>.ToWire(value) : null);

    // An unset option stays off the form, so the API's default applies; false is sent as false.
    private static FormPart FlagPart(string name, bool? value) =>
        FormPart.FromValue(name, value is { } flag ? (flag ? "true" : "false") : null);
}
