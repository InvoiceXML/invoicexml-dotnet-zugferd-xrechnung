namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// Document type of the invoice (UNTDID 1001). Sent and received as the numeric code,
/// for example <c>"380"</c> for a commercial invoice.
/// </summary>
public enum InvoiceTypeCode
{
    /// <summary>Partial invoice (326).</summary>
    PartialInvoice = 326,

    /// <summary>Commercial invoice (380). The default when no type is given.</summary>
    Invoice = 380,

    /// <summary>Credit note (381).</summary>
    CreditNote = 381,

    /// <summary>Debit note (383).</summary>
    DebitNote = 383,

    /// <summary>Corrected invoice (384).</summary>
    CorrectedInvoice = 384,

    /// <summary>Prepayment invoice (386).</summary>
    PrepaymentInvoice = 386,

    /// <summary>Self-billed invoice (389).</summary>
    SelfBilledInvoice = 389,

    /// <summary>Partial construction invoice (875).</summary>
    PartialConstructionInvoice = 875,

    /// <summary>Partial final construction invoice (876).</summary>
    PartialFinalConstructionInvoice = 876,

    /// <summary>Final construction invoice (877).</summary>
    FinalConstructionInvoice = 877,
}
