using System.Text;

namespace InvoiceXml.Zugferd.XRechnung.Tests;

internal static class TestData
{
    public static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7\n%test\n");

    public static readonly byte[] Xml = Encoding.UTF8.GetBytes("<?xml version=\"1.0\"?><Invoice />");

    public static (ZugferdXRechnungClient Client, MockHandler Handler) CreateClient(InvoiceXmlClientOptions? options = null)
    {
        var handler = new MockHandler();
        var client = new ZugferdXRechnungClient(new HttpClient(handler), options ?? new InvoiceXmlClientOptions { ApiKey = "test-key" });
        client.Transport.Delay = (_, _) => Task.CompletedTask;
        return (client, handler);
    }

    public static InvoiceDocument Invoice() => new()
    {
        InvoiceNumber = "RE-2026-001",
        IssueDate = new DateTime(2026, 10, 9),
        TypeCode = InvoiceTypeCode.Invoice,
        Currency = CurrencyCode.EUR,
        BuyerReference = "04011000-12345-03",
        Notes = { "Thank you for your order.", new InvoiceNote("Registered in Berlin", "REG") },
        Seller = new SellerParty
        {
            Name = "Muster GmbH",
            VatIdentifier = "DE123456789",
            PostalAddress = new PostalAddress { Line1 = "Hauptstr. 1", City = "Berlin", PostCode = "10115", Country = CountryCode.DE },
            ElectronicAddress = new SchemeIdentifier("billing@muster.de", "EM"),
        },
        Buyer = new BuyerParty
        {
            Name = "Kunde AG",
            PostalAddress = new PostalAddress { City = "Hamburg", PostCode = "20095", Country = CountryCode.DE },
        },
        PaymentDetails = new PaymentDetails
        {
            PaymentMeansCode = PaymentMeansCodes.SepaCreditTransfer,
            PaymentAccountIdentifier = "DE89370400440532013000",
        },
        Lines =
        {
            new InvoiceLine
            {
                Quantity = 10,
                UnitCode = UnitCodes.Hour,
                PriceDetails = new PriceDetails { NetPrice = 95.50m },
                VatInformation = new LineVatInformation { CategoryCode = VatCategoryCode.S, Rate = 19 },
                Item = new ItemInformation { Name = "Consulting" },
            },
        },
    };
}
