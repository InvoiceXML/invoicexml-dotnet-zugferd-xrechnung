namespace InvoiceXml.Zugferd.XRechnung.Tests;

/// <summary>
/// The piece constant follows the API default (H87 since 1.2.0); C62 stays
/// available under its own name for callers who need the older code.
/// </summary>
public class UnitCodesTests
{
    [Fact]
    public void Piece_is_H87_and_One_is_C62()
    {
        Assert.Equal("H87", UnitCodes.Piece);
        Assert.Equal("C62", UnitCodes.One);
        Assert.Equal("HUR", UnitCodes.Hour);
    }
}
