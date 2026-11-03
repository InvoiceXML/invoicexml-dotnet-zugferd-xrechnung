// Reads a ZUGFeRD invoice you received: the invoice data as a typed InvoiceDocument, and the
// supporting documents embedded in it (timesheets, delivery notes) as one ZIP archive.

using InvoiceXml.Zugferd.XRechnung;

// Replace these values with your own.
const string ApiKey = "YOUR_API_KEY";
const string ZugferdPdfPath = @"C:\invoices\incoming-zugferd.pdf"; // an XRechnung XML works too
const string AttachmentsZipPath = "incoming-zugferd-attachments.zip";

using var client = new ZugferdXRechnungClient(ApiKey);

try
{
    var file = await File.ReadAllBytesAsync(ZugferdPdfPath);
    var fileName = Path.GetFileName(ZugferdPdfPath);

    // 1. The invoice data, read from the embedded XML. It can be passed straight back to the create methods.
    var invoice = await client.ExtractJsonAsync(file, fileName);

    Console.WriteLine($"Invoice {invoice.InvoiceNumber} from {invoice.Seller?.Name} to {invoice.Buyer?.Name}");
    Console.WriteLine($"Total {invoice.Totals?.GrandTotalAmount} {invoice.Currency}, due {invoice.Totals?.DuePayableAmount}, {invoice.Lines.Count} line(s)");
    foreach (var line in invoice.Lines)
        Console.WriteLine($"  {line.Quantity} x {line.Item?.Name} at {line.PriceDetails?.NetPrice}");

    // 2. The embedded supporting documents, as one ZIP archive.
    try
    {
        var attachments = await client.ExtractAttachmentsAsync(file, fileName);
        await attachments.SaveAsync(AttachmentsZipPath);
        Console.WriteLine($"Saved the attachments to {AttachmentsZipPath}.");
    }
    catch (InvoiceXmlApiException ex) when (ex.ErrorCode == (int)InvoiceXmlErrorCode.NoAttachments)
    {
        Console.WriteLine("The invoice has no embedded attachments.");
    }
}
catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
{
    Console.WriteLine($"Could not find {ZugferdPdfPath}.");
    Console.WriteLine("Set ZugferdPdfPath at the top of Program.cs to a ZUGFeRD PDF on your machine.");
}
catch (InvoiceXmlApiException ex)
{
    // For example an invalid API key, or a PDF without an embedded invoice.
    Console.WriteLine($"The API refused the request: {ex.Message}");
}
