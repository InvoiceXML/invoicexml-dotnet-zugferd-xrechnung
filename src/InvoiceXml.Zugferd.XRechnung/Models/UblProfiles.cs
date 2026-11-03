namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// UBL profiles for <see cref="UblOptions.Profile"/>. The profile picks the identifier the UBL
/// invoice declares and the rule set it is validated against. Leave it empty for
/// <see cref="PeppolBis3"/>.
/// </summary>
public static class UblProfiles
{
    /// <summary>EN 16931: the European standard without a national or network extension.</summary>
    public const string En16931 = "en16931";

    /// <summary>Peppol BIS Billing 3.0: the profile for invoices sent over the Peppol network. The default.</summary>
    public const string PeppolBis3 = "peppol-bis-3";

    /// <summary>NLCIUS: the Dutch extension for invoices to Dutch public bodies.</summary>
    public const string NlCius = "nlcius";

    /// <summary>EHF: the Norwegian Peppol profile.</summary>
    public const string Ehf = "ehf";

    /// <summary>XRechnung: the German extension, in UBL syntax.</summary>
    public const string XRechnung = "xrechnung";

    /// <summary>Peppol PINT: the Peppol International invoice model.</summary>
    public const string Pint = "pint";
}
