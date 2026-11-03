// Reads an ordinary PDF invoice, typed, scanned or photographed, into the InvoiceDocument model.
// AI reads the PDF, so a person must check the data before it is used. The result says how sure
// the read was per area, which tells the person where to look first.
// To read a ZUGFeRD PDF or an XRechnung XML, use the ExtractJsonAndAttachments example instead:
// it reads the embedded XML exactly, without AI.

using System.Globalization;
using InvoiceXml.Zugferd.XRechnung;

// Replace these values with your own.
const string ApiKey = "YOUR_API_KEY";
const string PdfPath = @"C:\invoices\supplier-invoice.pdf"; // an ordinary PDF invoice
const decimal CheckBelow = 0.8m; // areas scored below this are flagged for a closer check

using var client = new ZugferdXRechnungClient(ApiKey);

try
{
    // Read the PDF first, so a wrong path stops the example before any API call.
    var pdf = await File.ReadAllBytesAsync(PdfPath);

    var parsed = await client.ParseJsonAsync(pdf, Path.GetFileName(PdfPath));
    var invoice = parsed.Invoice;

    Console.WriteLine($"Invoice {invoice.InvoiceNumber} from {invoice.Seller?.Name} to {invoice.Buyer?.Name}");
    Console.WriteLine($"Total {invoice.Totals?.GrandTotalAmount} {invoice.Currency}, due {invoice.Totals?.DuePayableAmount}");
    foreach (var line in invoice.Lines)
        Console.WriteLine($"  {line.Quantity} x {line.Item?.Name} at {line.PriceDetails?.NetPrice}");

    // The language the invoice is written in, as an ISO 639-1 code such as "de". Null when unknown.
    Console.WriteLine($"Language: {parsed.Language ?? "unknown"}");

    // Scores run from 0.0 (had to guess) to 1.0 (unambiguous). They can be missing.
    if (parsed.Confidence is { } confidence)
    {
        Console.WriteLine($"Confidence overall: {Score(confidence.Overall)}");
        Flag("seller", confidence.Areas.SellerIdentification);
        Flag("buyer", confidence.Areas.BuyerIdentification);
        Flag("VAT", confidence.Areas.TaxCalculation);
        Flag("lines", confidence.Areas.LineItems);
    }

    // The checked invoice can be passed straight to a create method, for example
    // client.CreateZugferdAsync(invoice) to turn it into a ZUGFeRD invoice.
}
catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
{
    Console.WriteLine($"Could not find {PdfPath}.");
    Console.WriteLine("Set PdfPath at the top of Program.cs to a PDF invoice on your machine.");
}
catch (InvoiceXmlRateLimitException ex)
{
    // Parse calls are not retried automatically, because a retry repeats the AI read.
    Console.WriteLine(ex.RetryAfter is { } wait
        ? $"Too many requests. Wait {wait.TotalSeconds:0} seconds before trying again."
        : "Too many requests. Wait a moment before trying again.");
}
catch (InvoiceXmlApiException ex)
{
    // For example an invalid API key, or a file that is not a PDF invoice.
    Console.WriteLine($"The API refused the request: {ex.Message}");
}

static void Flag(string area, decimal? score)
{
    var note = score is null ? "no score" : score < CheckBelow ? "check this closely" : "ok";
    Console.WriteLine($"  {area}: {Score(score)} ({note})");
}

// Scores print the same on every machine, for example 0.85.
static string Score(decimal? score) => score?.ToString(CultureInfo.InvariantCulture) ?? "-";
