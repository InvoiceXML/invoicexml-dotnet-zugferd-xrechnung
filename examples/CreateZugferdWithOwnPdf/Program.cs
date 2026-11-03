// Creates a ZUGFeRD invoice that keeps your own PDF layout.
// The API downloads your PDF from a public URL, embeds the XML it generates from the invoice
// data into it, and returns a ZUGFeRD PDF/A-3. Nothing about your PDF's look changes.

using InvoiceXml.Zugferd.XRechnung;

// Replace these values with your own.
const string ApiKey = "YOUR_API_KEY";
const string PdfUrl = "https://example.com/invoices/RE-2026-001.pdf"; // public http(s) URL of your PDF
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
    // PdfUrl makes your PDF the visual layer. Without it, the API renders its own invoice layout.
    var zugferd = await client.CreateZugferdAsync(invoice, new ZugferdOptions { PdfUrl = PdfUrl });

    await zugferd.SaveAsync(OutputPath);
    Console.WriteLine($"Saved the ZUGFeRD invoice to {OutputPath}.");
}
catch (InvoiceXmlApiException ex) when (ex.IsValidationError)
{
    // The invoice data would not produce a compliant ZUGFeRD invoice. Each finding names the field to fix.
    Console.WriteLine("The invoice is not compliant:");
    foreach (var finding in ex.Findings)
        Console.WriteLine($"  [{finding.Rule}] {finding.Message} ({string.Join(", ", finding.Fields)})");
}
catch (InvoiceXmlApiException ex)
{
    // For example an invalid API key, or a PDF URL the API could not download.
    Console.WriteLine($"The API refused the request: {ex.Message}");
}
