// Creates a ZUGFeRD invoice and emails it to your customer in one call, sent from your own address.
//
// Before running it:
// - Add and verify your sending domain on the Verified email domains page of your dashboard.
//   Until it is verified, the API refuses the call and nothing is sent.
// - The client already retries when the API answers 429, because nothing was sent then.
//   A timeout or a lost connection may still mean the email went out, so check before
//   sending the same invoice again.

using InvoiceXml.Zugferd.XRechnung;

// Replace these values with your own.
const string ApiKey = "YOUR_API_KEY";
const string From = "invoices@yourcompany.example"; // must be on your verified domain
const string To = "accounting@customer.example";

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

var email = new EmailMessage
{
    From = From,
    FromName = "Muster GmbH Billing",
    To = To,
    Subject = "Rechnung RE-2026-001",
    Message = "Anbei erhalten Sie Ihre Rechnung.",
};

try
{
    // The invoice is validated exactly like on CreateZugferdAsync before anything is sent.
    var sent = await client.EmailZugferdAsync(invoice, email, new ZugferdOptions { Language = PdfLanguage.De });

    // "accepted" means the email was handed over for delivery.
    Console.WriteLine($"{sent.Status}: message id {sent.MessageId}");
}
catch (InvoiceXmlApiException ex) when (ex.ErrorCode == (int)InvoiceXmlErrorCode.EmailDomainNotVerified)
{
    Console.WriteLine($"The domain of {From} is not verified yet. Verify it in your dashboard, then run this again.");
}
catch (InvoiceXmlApiException ex) when (ex.IsValidationError)
{
    // The invoice or an email field is invalid. Nothing was sent.
    Console.WriteLine("Nothing was sent. Fix these fields first:");
    foreach (var finding in ex.Findings)
        Console.WriteLine($"  [{finding.Rule}] {finding.Message} ({string.Join(", ", finding.Fields)})");
}
catch (InvoiceXmlApiException ex)
{
    Console.WriteLine($"The API refused the request: {ex.Message}");
}
