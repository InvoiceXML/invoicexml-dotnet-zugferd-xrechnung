namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// An invoice read from a PDF by AI, returned by
/// <see cref="ZugferdXRechnungClient.ParseJsonAsync(byte[], string?, System.Threading.CancellationToken)"/>.
/// AI read the data, so a person must check it before it is used.
/// </summary>
public sealed class ParsedInvoice
{
    /// <summary>Creates a parse result. Useful when faking <see cref="IZugferdXRechnungClient"/> in tests.</summary>
    /// <param name="invoice">The invoice read from the PDF.</param>
    /// <param name="confidence">How sure the read was, or null when no scores came back.</param>
    /// <param name="language">The language the invoice is written in, or null when unknown.</param>
    public ParsedInvoice(InvoiceDocument invoice, ParseConfidence? confidence, string? language)
    {
        Invoice = invoice ?? throw new System.ArgumentNullException(nameof(invoice));
        Confidence = confidence;
        Language = language;
    }

    /// <summary>The invoice read from the PDF, ready to check and pass to a create method.</summary>
    public InvoiceDocument Invoice { get; }

    /// <summary>How sure the read was, overall and per area. Null when no scores came back.</summary>
    public ParseConfidence? Confidence { get; }

    /// <summary>
    /// The lowercase ISO 639-1 code of the language the invoice is written in, for example
    /// <c>de</c>. Any language, so not always a <see cref="PdfLanguage"/>. Null when it could not be told.
    /// </summary>
    public string? Language { get; }
}

/// <summary>
/// How sure the AI read of a PDF was. Scores run from 0.0 (had to guess) to 1.0 (unambiguous).
/// </summary>
public sealed class ParseConfidence
{
    /// <summary>The mean of the reported area scores, rounded to two decimals. Null when none were reported.</summary>
    public decimal? Overall { get; set; }

    /// <summary>The score of each area that was read.</summary>
    public ParseConfidenceAreas Areas { get; set; } = new();
}

/// <summary>The scores of the four areas of an AI read. Each one is null when no score was reported for it.</summary>
public sealed class ParseConfidenceAreas
{
    /// <summary>How sure the read of the seller's name, address and identifiers was.</summary>
    public decimal? SellerIdentification { get; set; }

    /// <summary>How sure the read of the buyer's name, address and identifiers was.</summary>
    public decimal? BuyerIdentification { get; set; }

    /// <summary>How sure the read of the VAT rates and amounts was.</summary>
    public decimal? TaxCalculation { get; set; }

    /// <summary>How sure the read of the invoice lines was. Lowered when the lines do not add up to the printed totals.</summary>
    public decimal? LineItems { get; set; }
}
