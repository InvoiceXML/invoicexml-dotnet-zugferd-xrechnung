using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// Creates, validates, embeds, renders, converts, emails and reads ZUGFeRD and XRechnung
/// e-invoices through the InvoiceXML API. Implemented by <see cref="ZugferdXRechnungClient"/>; use
/// the interface to inject and fake the client in your own tests.
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
    /// Creates a plain UN/CEFACT CII XML, validated against EN 16931 before delivery
    /// (<c>POST /v1/create/cii</c>). Pass it to <see cref="EmbedZugferdAsync(byte[], byte[], bool, string?, CancellationToken)"/>
    /// to turn your own PDF into a ZUGFeRD invoice.
    /// </summary>
    /// <param name="invoice">The invoice.</param>
    /// <param name="options">Language, or null for the defaults.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The CII XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The invoice would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> CreateCiiAsync(InvoiceDocument invoice, CiiOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a UBL 2.1 invoice XML in the profile you choose (Peppol BIS Billing 3.0 by default),
    /// validated against that profile's rules before delivery (<c>POST /v1/create/ubl</c>).
    /// </summary>
    /// <param name="invoice">The invoice.</param>
    /// <param name="options">Profile and language, or null for the defaults.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The UBL XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The invoice would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> CreateUblAsync(InvoiceDocument invoice, UblOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a ZUGFeRD invoice exactly like <see cref="CreateZugferdAsync"/> and sends it as a
    /// real email from your own verified domain (<c>POST /v1/create/zugferd/email</c>). Returns the
    /// message id of the email, not the file.
    /// </summary>
    /// <param name="invoice">The invoice.</param>
    /// <param name="email">Sender, recipient and, optionally, the copies, subject and text of the email.</param>
    /// <param name="options">Rendering options, or null for the defaults.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The accepted email with its message id.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The invoice would not be compliant or an email field is invalid (see <see cref="InvoiceXmlApiException.Findings"/>),
    /// the sender's domain is not verified (<see cref="InvoiceXmlErrorCode.EmailDomainNotVerified"/>),
    /// your plan does not include email delivery (<see cref="InvoiceXmlErrorCode.EmailNotInPlan"/>),
    /// sending is disabled for the account (<see cref="InvoiceXmlErrorCode.EmailSendingDisabled"/>), or the request was refused.
    /// </exception>
    Task<EmailSendResult> EmailZugferdAsync(InvoiceDocument invoice, EmailMessage email, ZugferdOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates an XRechnung XML exactly like <see cref="CreateXRechnungAsync"/> and sends it as a
    /// real email from your own verified domain (<c>POST /v1/create/xrechnung/email</c>). Returns
    /// the message id of the email, not the file.
    /// </summary>
    /// <param name="invoice">The invoice. <see cref="InvoiceDocument.BuyerReference"/> carries the Leitweg-ID.</param>
    /// <param name="email">Sender, recipient and, optionally, the copies, subject and text of the email.</param>
    /// <param name="options">Syntax and language, or null for the defaults.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The accepted email with its message id.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The invoice would not be compliant or an email field is invalid (see <see cref="InvoiceXmlApiException.Findings"/>),
    /// the sender's domain is not verified (<see cref="InvoiceXmlErrorCode.EmailDomainNotVerified"/>),
    /// your plan does not include email delivery (<see cref="InvoiceXmlErrorCode.EmailNotInPlan"/>),
    /// sending is disabled for the account (<see cref="InvoiceXmlErrorCode.EmailSendingDisabled"/>), or the request was refused.
    /// </exception>
    Task<EmailSendResult> EmailXRechnungAsync(InvoiceDocument invoice, EmailMessage email, XRechnungOptions? options = null, CancellationToken cancellationToken = default);

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
    /// Turns your own PDF plus a CII XML into a ZUGFeRD PDF/A-3, always with full validation, and
    /// sends it as a real email from your own verified domain (<c>POST /v1/embed/zugferd/email</c>).
    /// Returns the message id of the email, not the file.
    /// </summary>
    /// <param name="pdf">Your invoice PDF, used as the visual layer.</param>
    /// <param name="xml">The CII invoice XML to embed.</param>
    /// <param name="email">Sender, recipient and, optionally, the copies, subject and text of the email.</param>
    /// <param name="fileName">File name of the PDF upload. Defaults to <c>invoice.pdf</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The accepted email with its message id.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The XML is not compliant or an email field is invalid (see <see cref="InvoiceXmlApiException.Findings"/>),
    /// the sender's domain is not verified (<see cref="InvoiceXmlErrorCode.EmailDomainNotVerified"/>),
    /// your plan does not include email delivery (<see cref="InvoiceXmlErrorCode.EmailNotInPlan"/>),
    /// sending is disabled for the account (<see cref="InvoiceXmlErrorCode.EmailSendingDisabled"/>), or the request was refused.
    /// </exception>
    Task<EmailSendResult> EmailEmbeddedZugferdAsync(byte[] pdf, byte[] xml, EmailMessage email, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="EmailEmbeddedZugferdAsync(byte[], byte[], EmailMessage, string?, CancellationToken)"/>
    Task<EmailSendResult> EmailEmbeddedZugferdAsync(Stream pdf, Stream xml, EmailMessage email, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renders any invoice XML as a human-readable PDF: XRechnung, CII (such as the XML of a
    /// ZUGFeRD invoice) or UBL. The format is detected from the XML (<c>POST /v1/render/xml/to/pdf</c>).
    /// </summary>
    /// <param name="xml">The invoice XML.</param>
    /// <param name="options">Language, logo and payment QR code, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The rendered PDF.</returns>
    Task<InvoiceFile> RenderXmlToPdfAsync(byte[] xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="RenderXmlToPdfAsync(byte[], RenderOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> RenderXmlToPdfAsync(Stream xml, RenderOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a ZUGFeRD PDF to a UBL XML from the XML embedded in it (<c>POST /v1/convert/zugferd/to/ubl</c>).
    /// </summary>
    /// <param name="pdf">The ZUGFeRD PDF.</param>
    /// <param name="options">Language of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.pdf</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The UBL XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The PDF has no embedded XML (<see cref="InvoiceXmlErrorCode.NoEmbeddedXml"/>), the result
    /// would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ConvertZugferdToUblAsync(byte[] pdf, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ConvertZugferdToUblAsync(byte[], ConvertOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> ConvertZugferdToUblAsync(Stream pdf, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a ZUGFeRD PDF to a CII XML from the XML embedded in it (<c>POST /v1/convert/zugferd/to/cii</c>).
    /// </summary>
    /// <param name="pdf">The ZUGFeRD PDF.</param>
    /// <param name="options">Language of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.pdf</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The CII XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The PDF has no embedded XML (<see cref="InvoiceXmlErrorCode.NoEmbeddedXml"/>), the result
    /// would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ConvertZugferdToCiiAsync(byte[] pdf, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ConvertZugferdToCiiAsync(byte[], ConvertOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> ConvertZugferdToCiiAsync(Stream pdf, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an XRechnung XML, UBL or CII, to a plain EN 16931 UBL XML
    /// (<c>POST /v1/convert/xrechnung/to/ubl</c>).
    /// </summary>
    /// <param name="xml">The XRechnung XML, UBL or CII.</param>
    /// <param name="options">Language of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The UBL XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The result would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ConvertXRechnungToUblAsync(byte[] xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ConvertXRechnungToUblAsync(byte[], ConvertOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> ConvertXRechnungToUblAsync(Stream xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an XRechnung XML, UBL or CII, to a plain EN 16931 CII XML
    /// (<c>POST /v1/convert/xrechnung/to/cii</c>).
    /// </summary>
    /// <param name="xml">The XRechnung XML, UBL or CII.</param>
    /// <param name="options">Language of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The CII XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The result would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ConvertXRechnungToCiiAsync(byte[] xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ConvertXRechnungToCiiAsync(byte[], ConvertOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> ConvertXRechnungToCiiAsync(Stream xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a UBL XML to an XRechnung in UBL syntax. The invoice must meet the German
    /// XRechnung rules (<c>POST /v1/convert/ubl/to/xrechnung</c>).
    /// </summary>
    /// <param name="xml">The UBL invoice XML.</param>
    /// <param name="options">Language of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The XRechnung XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The result would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ConvertUblToXRechnungAsync(byte[] xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ConvertUblToXRechnungAsync(byte[], ConvertOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> ConvertUblToXRechnungAsync(Stream xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a CII XML to an XRechnung in UBL syntax. The invoice must meet the German
    /// XRechnung rules (<c>POST /v1/convert/cii/to/xrechnung</c>).
    /// </summary>
    /// <param name="xml">The CII invoice XML.</param>
    /// <param name="options">Language of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The XRechnung XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The result would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ConvertCiiToXRechnungAsync(byte[] xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ConvertCiiToXRechnungAsync(byte[], ConvertOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> ConvertCiiToXRechnungAsync(Stream xml, ConvertOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a CII XML to a ZUGFeRD PDF/A-3, with the PDF rendered from the invoice data
    /// (<c>POST /v1/convert/cii/to/zugferd</c>).
    /// </summary>
    /// <param name="xml">The CII invoice XML.</param>
    /// <param name="options">Language of the PDF and of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The ZUGFeRD PDF.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The result would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ConvertCiiToZugferdAsync(byte[] xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ConvertCiiToZugferdAsync(byte[], ConvertToPdfOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> ConvertCiiToZugferdAsync(Stream xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts a UBL XML to a ZUGFeRD PDF/A-3, with the PDF rendered from the invoice data
    /// (<c>POST /v1/convert/ubl/to/zugferd</c>).
    /// </summary>
    /// <param name="xml">The UBL invoice XML.</param>
    /// <param name="options">Language of the PDF and of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The ZUGFeRD PDF.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The result would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ConvertUblToZugferdAsync(byte[] xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ConvertUblToZugferdAsync(byte[], ConvertToPdfOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> ConvertUblToZugferdAsync(Stream xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Converts an XRechnung XML, UBL or CII, to a ZUGFeRD PDF/A-3, with the PDF rendered from the
    /// invoice data (<c>POST /v1/convert/xrechnung/to/zugferd</c>).
    /// </summary>
    /// <param name="xml">The XRechnung XML, UBL or CII.</param>
    /// <param name="options">Language of the PDF and of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The ZUGFeRD PDF.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The result would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> ConvertXRechnungToZugferdAsync(byte[] xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ConvertXRechnungToZugferdAsync(byte[], ConvertToPdfOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> ConvertXRechnungToZugferdAsync(Stream xml, ConvertToPdfOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a ZUGFeRD PDF like <see cref="ValidateZugferdAsync(byte[], string?, CancellationToken)"/>
    /// and returns a printable PDF report with the verdict (<c>POST /v1/validate/zugferd/report</c>).
    /// </summary>
    /// <param name="pdf">The ZUGFeRD PDF.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.pdf</c>.</param>
    /// <param name="validationLanguage">Language of the whole report. Defaults to English.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The report PDF and the verdict. An invalid invoice is a normal result, not an exception.</returns>
    /// <exception cref="InvoiceXmlApiException">The upload could not be read, or the request was refused.</exception>
    Task<ValidationReportResult> ValidateZugferdReportAsync(byte[] pdf, string? fileName = null, PdfLanguage? validationLanguage = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ValidateZugferdReportAsync(byte[], string?, PdfLanguage?, CancellationToken)"/>
    Task<ValidationReportResult> ValidateZugferdReportAsync(Stream pdf, string? fileName = null, PdfLanguage? validationLanguage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an XRechnung XML like <see cref="ValidateXRechnungAsync(byte[], string?, CancellationToken)"/>
    /// and returns a printable PDF report with the verdict (<c>POST /v1/validate/xrechnung/report</c>).
    /// </summary>
    /// <param name="xml">The XRechnung XML.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.xml</c>.</param>
    /// <param name="validationLanguage">Language of the whole report. Defaults to English.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The report PDF and the verdict. An invalid invoice is a normal result, not an exception.</returns>
    /// <exception cref="InvoiceXmlApiException">The upload could not be read, or the request was refused.</exception>
    Task<ValidationReportResult> ValidateXRechnungReportAsync(byte[] xml, string? fileName = null, PdfLanguage? validationLanguage = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ValidateXRechnungReportAsync(byte[], string?, PdfLanguage?, CancellationToken)"/>
    Task<ValidationReportResult> ValidateXRechnungReportAsync(Stream xml, string? fileName = null, PdfLanguage? validationLanguage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns an ordinary PDF invoice into a ZUGFeRD PDF/A-3 (<c>POST /v1/transform/to/zugferd</c>).
    /// AI reads the PDF, so a person must check the result before it is used.
    /// A 429 on this method is not retried automatically, so wait for <c>Retry-After</c> before calling again if you want to retry.
    /// </summary>
    /// <param name="pdf">The PDF invoice.</param>
    /// <param name="options">Language, visual layer and language of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.pdf</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The ZUGFeRD PDF.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The result would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> TransformToZugferdAsync(byte[] pdf, TransformOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="TransformToZugferdAsync(byte[], TransformOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> TransformToZugferdAsync(Stream pdf, TransformOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns an ordinary PDF invoice into an XRechnung XML (<c>POST /v1/transform/to/xrechnung</c>).
    /// AI reads the PDF, so a person must check the result before it is used.
    /// A 429 on this method is not retried automatically, so wait for <c>Retry-After</c> before calling again if you want to retry.
    /// </summary>
    /// <param name="pdf">The PDF invoice.</param>
    /// <param name="buyerReference">The Leitweg-ID. Takes precedence over anything read from the PDF.</param>
    /// <param name="options">Language of the findings, or null for the defaults.</param>
    /// <param name="fileName">File name sent with the upload. Defaults to <c>invoice.pdf</c>.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The XRechnung XML.</returns>
    /// <exception cref="InvoiceXmlApiException">
    /// The result would not be compliant (see <see cref="InvoiceXmlApiException.Findings"/>), or the request was refused.
    /// </exception>
    Task<InvoiceFile> TransformToXRechnungAsync(byte[] pdf, string buyerReference, TransformToXRechnungOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="TransformToXRechnungAsync(byte[], string, TransformToXRechnungOptions?, string?, CancellationToken)"/>
    Task<InvoiceFile> TransformToXRechnungAsync(Stream pdf, string buyerReference, TransformToXRechnungOptions? options = null, string? fileName = null, CancellationToken cancellationToken = default);

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
    /// Reads an ordinary PDF invoice, typed, scanned or photographed, into an
    /// <see cref="InvoiceDocument"/> (<c>POST /v1/parse/json</c>). AI reads the PDF, so a person
    /// must check the data before it is used. Any XML embedded in the PDF is ignored; use
    /// <see cref="ExtractJsonAsync(byte[], string?, CancellationToken)"/> to read that instead.
    /// A 429 on this method is not retried automatically, so wait for <c>Retry-After</c> before calling again if you want to retry.
    /// </summary>
    /// <param name="pdf">The PDF invoice.</param>
    /// <param name="fileName">File name sent with the upload. Defaults by content type.</param>
    /// <param name="cancellationToken">Cancels the request.</param>
    /// <returns>The invoice, how sure the read was, and the language the invoice is written in.</returns>
    /// <exception cref="InvoiceXmlApiException">The file is not a PDF, or the request was refused.</exception>
    Task<ParsedInvoice> ParseJsonAsync(byte[] pdf, string? fileName = null, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ParseJsonAsync(byte[], string?, CancellationToken)"/>
    Task<ParsedInvoice> ParseJsonAsync(Stream pdf, string? fileName = null, CancellationToken cancellationToken = default);

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
