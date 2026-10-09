namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// VAT category of a line, allowance, charge or VAT breakdown (UNTDID 5305 subset used by EN 16931).
/// </summary>
public enum VatCategoryCode
{
    /// <summary>Standard rate.</summary>
    S,

    /// <summary>Zero rated goods.</summary>
    Z,

    /// <summary>Exempt from tax.</summary>
    E,

    /// <summary>VAT reverse charge.</summary>
    AE,

    /// <summary>VAT exempt for EEA intra-community supply of goods and services.</summary>
    K,

    /// <summary>Free export item, VAT not charged.</summary>
    G,

    /// <summary>Services outside scope of tax.</summary>
    O,

    /// <summary>Canary Islands general indirect tax.</summary>
    L,

    /// <summary>Tax for production, services and importation in Ceuta and Melilla.</summary>
    M,
}
