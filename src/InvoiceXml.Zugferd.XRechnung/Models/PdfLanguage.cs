namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// Language of the texts the API writes on your behalf: the labels of a rendered PDF, and
/// default texts such as payment terms or a VAT exemption reason when you leave them empty.
/// Your own texts are always used as they are. Sent as <c>"en"</c>, <c>"de"</c> or <c>"fr"</c>.
/// </summary>
public enum PdfLanguage
{
    /// <summary>English (the default).</summary>
    En,

    /// <summary>German.</summary>
    De,

    /// <summary>French.</summary>
    Fr,
}
