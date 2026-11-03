// Builds a CII XML from your invoice data, then embeds it into a PDF you already have.
// Use this when your own system prints the invoice PDF and you want it to become a
// ZUGFeRD invoice. The CII XML declares the EN 16931 profile, so it embeds as it is.

using InvoiceXml.Zugferd.XRechnung;

// Replace these values with your own.
const string ApiKey = "YOUR_API_KEY";
const string PdfPath = @"C:\invoices\RE-2026-001.pdf"; // the PDF your system printed
const string OutputPath = "RE-2026-001-zugferd.pdf";

using var client = new ZugferdXRechnungClient(ApiKey);

var invoice = new InvoiceDocument
{
    InvoiceNumber = "RE-2026-001",
    Currency = CurrencyCode.EUR,
    Seller = new SellerParty
    {
        Name = "Muster GmbH",
        VatIdentifier = "DE123456789",
        PostalAddress = new PostalAddress { City = "Berlin", PostCode = "10115", Country = CountryCode.DE },
    },
    Buyer = new BuyerParty
    {
        Name = "Kunde AG",
        PostalAddress = new PostalAddress { City = "Hamburg", PostCode = "20095", Country = CountryCode.DE },
    },
    Lines =
    {
        new InvoiceLine
        {
            Quantity = 10,
            UnitCode = UnitCodes.Hour,
            Item = new ItemInformation { Name = "Consulting" },
            PriceDetails = new PriceDetails { NetPrice = 95.50m },
            VatInformation = new LineVatInformation { CategoryCode = VatCategoryCode.S, Rate = 19 },
        },
    },
};

try
{
    // Read the PDF first, so a wrong path stops the example before any API call.
    var pdf = await File.ReadAllBytesAsync(PdfPath);

    // 1. Build the CII XML from the invoice data. Language words the default texts, such as the payment terms.
    var cii = await client.CreateCiiAsync(invoice, new CiiOptions { Language = PdfLanguage.De });

    // 2. Embed the XML into your PDF. The result is a ZUGFeRD PDF/A-3.
    var zugferd = await client.EmbedZugferdAsync(pdf, cii.ToArray(), fileName: Path.GetFileName(PdfPath));

    await zugferd.SaveAsync(OutputPath);
    Console.WriteLine($"Saved the ZUGFeRD invoice to {OutputPath}.");
}
catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
{
    Console.WriteLine($"Could not find {PdfPath}.");
    Console.WriteLine("Set PdfPath at the top of Program.cs to a PDF on your machine.");
}
catch (InvoiceXmlApiException ex) when (ex.IsValidationError)
{
    // The invoice data would not produce a compliant invoice. Each finding names the field to fix.
    Console.WriteLine("The invoice is not compliant:");
    foreach (var finding in ex.Findings)
        Console.WriteLine($"  [{finding.Rule}] {finding.Message} ({string.Join(", ", finding.Fields)})");
}
catch (InvoiceXmlApiException ex)
{
    Console.WriteLine($"The API refused the request: {ex.Message}");
}
