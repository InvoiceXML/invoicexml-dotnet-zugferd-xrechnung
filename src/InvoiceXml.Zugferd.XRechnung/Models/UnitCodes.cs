namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// Common UN/ECE Recommendation 20 unit codes for <see cref="InvoiceLine.UnitCode"/>.
/// Any valid Recommendation 20 code is accepted; these constants cover the usual cases.
/// </summary>
public static class UnitCodes
{
    /// <summary>One, piece (C62).</summary>
    public const string Piece = "C62";

    /// <summary>Hour (HUR).</summary>
    public const string Hour = "HUR";

    /// <summary>Day (DAY).</summary>
    public const string Day = "DAY";

    /// <summary>Week (WEE).</summary>
    public const string Week = "WEE";

    /// <summary>Month (MON).</summary>
    public const string Month = "MON";

    /// <summary>Year (ANN).</summary>
    public const string Year = "ANN";

    /// <summary>Kilogram (KGM).</summary>
    public const string Kilogram = "KGM";

    /// <summary>Tonne, metric ton (TNE).</summary>
    public const string Tonne = "TNE";

    /// <summary>Litre (LTR).</summary>
    public const string Litre = "LTR";

    /// <summary>Metre (MTR).</summary>
    public const string Metre = "MTR";

    /// <summary>Square metre (MTK).</summary>
    public const string SquareMetre = "MTK";

    /// <summary>Cubic metre (MTQ).</summary>
    public const string CubicMetre = "MTQ";

    /// <summary>Set (SET).</summary>
    public const string Set = "SET";
}
