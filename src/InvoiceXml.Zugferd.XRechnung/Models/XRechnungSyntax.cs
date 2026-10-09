namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// XML syntax of a created XRechnung. Both syntaxes are legally equivalent and pass the same
/// German rule set; pick the one your receiver or archive expects.
/// </summary>
public enum XRechnungSyntax
{
    /// <summary>UBL 2.1 (the default, and the syntax most German receivers see in practice).</summary>
    Ubl,

    /// <summary>UN/CEFACT Cross Industry Invoice (CII).</summary>
    Cii,
}
