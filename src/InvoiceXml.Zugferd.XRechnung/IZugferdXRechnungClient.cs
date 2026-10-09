using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// Creates, validates, embeds, renders and reads ZUGFeRD and XRechnung e-invoices through the
/// InvoiceXML API. Implemented by <see cref="ZugferdXRechnungClient"/>; use the interface to
/// inject and fake the client in your own tests.
/// </summary>
public interface IZugferdXRechnungClient
{
    /// <summary>
    /// Creates a ZUGFeRD invoice: a PDF/A-3 with the EN 16931 XML embedded, validated before delivery
    /// (<c>POST /v1/create/zugferd</c>).
    /// </summary>
    /// <param name="invoice">The invoice.</param>
    /// <param name="options">Rendering options, or null for the defaults.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The ZUGFeRD PDF.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The invoice would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> CreateZugferdAsync(InvoiceDocument invoice, ZugferdOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an XRechnung XML (UBL by default, or CII), validated against EN 16931 and the
    /// German XRechnung rules before delivery (<c>POST /v1/create/xrechnung</c>).
    /// </summary>
    /// <param name="invoice">The invoice. <see cref="InvoiceDocument.BuyerReference"/> carries the Leitweg-ID.</param>
    /// <param name="options">Syntax and language, or null for the defaults.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The XRechnung XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The invoice would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> CreateXRechnungAsync(InvoiceDocument invoice, XRechnungOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a ZUGFeRD PDF: extracts the embedded XML and checks it against the rules of the
    /// profile it declares (<c>POST /v1/validate/zugferd</c>).
    /// </summary>
    /// <param name="pdf">The ZUGFeRD PDF.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.pdf</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The verdict. An invalid invoice is a normal result, not an exception.</returns>
    Task<InvoiceValidationResult> ValidateZugferdAsync(byte[] pdf, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ValidateZugferdAsync(byte[], string?, CancellationToken)"/>
    Task<InvoiceValidationResult> ValidateZugferdAsync(Stream pdf, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an XRechnung XML in UBL or CII syntax against EN 16931 and the German XRechnung
    /// rules (<c>POST /v1/validate/xrechnung</c>).
    /// </summary>
    /// <param name="xml">The XRechnung XML.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The verdict. An invalid invoice is a normal result, not an exception.</returns>
    Task<InvoiceValidationResult> ValidateXRechnungAsync(byte[] xml, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ValidateXRechnungAsync(byte[], string?, CancellationToken)"/>
    Task<InvoiceValidationResult> ValidateXRechnungAsync(Stream xml, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns your own PDF plus a CII XML into a ZUGFeRD PDF/A-3. The XML is validated before it is
    /// embedded (<c>POST /v1/embed/zugferd</c>).
    /// </summary>
    /// <param name="pdf">Your invoice PDF, used as the visual layer.</param>
    /// <param name="xml">The CII invoice XML to embed.</param>
    /// <param name="skipValidation">
    /// True to embed without the business rule checks. Structural checks still apply. Defaults to false.
    /// </param>
    /// <param name="fileName">File name of the PDF upload. Defaults to <c>invoice.pdf</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The ZUGFeRD PDF.</returns>
    /// <exception cref="InvoiceXmlApiException">The XML is not compliant, or the request was refused.</exception>
    Task<InvoiceFile> EmbedZugferdAsync(byte[] pdf, byte[] xml, bool skipValidation = false, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="EmbedZugferdAsync(byte[], byte[], bool, string?, CancellationToken)"/>
    Task<InvoiceFile> EmbedZugferdAsync(Stream pdf, Stream xml, bool skipValidation = false, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders an XRechnung XML as a human-readable PDF (<c>POST /v1/render/xrechnung/to/pdf</c>).
    /// </summary>
    /// <param name="xml">The XRechnung XML, UBL or CII.</param>
    /// <param name="options">Language and logo, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The rendered PDF.</returns>
    Task<InvoiceFile> RenderXRechnungToPdfAsync(byte[] xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="RenderXRechnungToPdfAsync(byte[], RenderOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> RenderXRechnungToPdfAsync(Stream xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads an e-invoice (a ZUGFeRD PDF or an XRechnung XML) into an <see cref="InvoiceDocument"/>
    /// by deterministic extraction of its XML (<c>POST /v1/extract/json</c>).
    /// </summary>
    /// <param name="file">A ZUGFeRD PDF or an invoice XML.</param>
    /// <param name="fileName">File name sent with the upload. Defaults by content type.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The invoice, ready to pass back to a create method.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The PDF has no embedded XML (<see cref="InvoiceXmlErrorCode.NoEmbeddedXml"/>), or the request was refused.
    /// </exception>
    Task<InvoiceDocument> ExtractJsonAsync(byte[] file, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ExtractJsonAsync(byte[], string?, CancellationToken)"/>
    Task<InvoiceDocument> ExtractJsonAsync(Stream file, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the supporting documents embedded in an e-invoice, such as timesheets or delivery
    /// notes, as one ZIP archive (<c>POST /v1/extract/attachments</c>).
    /// </summary>
    /// <param name="file">A ZUGFeRD PDF or an invoice XML.</param>
    /// <param name="fileName">File name sent with the upload. Defaults by content type.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>A ZIP archive with the original files.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The invoice has no embedded attachments (<see cref="InvoiceXmlErrorCode.NoAttachments"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ExtractAttachmentsAsync(byte[] file, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ExtractAttachmentsAsync(byte[], string?, CancellationToken)"/>
    Task<InvoiceFile> ExtractAttachmentsAsync(Stream file, string? fileName = null, CancellationToken cancellationToken = default);
}
