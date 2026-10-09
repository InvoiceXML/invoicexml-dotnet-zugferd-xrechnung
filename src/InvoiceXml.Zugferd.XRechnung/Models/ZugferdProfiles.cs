namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// Official ZUGFeRD 2.x profile identifiers for <see cref="InvoiceDocument.SpecificationId"/>.
/// Leave the specification identifier empty to get the recommended EN 16931 profile.
/// </summary>
public static class ZugferdProfiles
{
    /// <summary>MINIMUM: header-only booking aid. The PDF is the legal invoice.</summary>
    public const string Minimum = "urn:factur-x.eu:1p0:minimum";

    /// <summary>BASIC WL: header-only booking aid without lines. The PDF is the legal invoice.</summary>
    public const string BasicWl = "urn:factur-x.eu:1p0:basicwl";

    /// <summary>BASIC: a subset of EN 16931 with line items.</summary>
    public const string Basic = "urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic";

    /// <summary>EN 16931 (COMFORT): the full European standard. The default and recommended profile.</summary>
    public const string En16931 = "urn:cen.eu:en16931:2017";

    /// <summary>EXTENDED: EN 16931 plus additional structured content.</summary>
    public const string Extended = "urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended";
}
