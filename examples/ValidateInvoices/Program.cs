// Validates incoming invoices: a ZUGFeRD PDF and an XRechnung XML.
// A failed validation is a normal result, not an exception: you get the verdict and every
// finding with the rule, the message and the field it concerns. The example also saves the
// printable validation report PDF for the ZUGFeRD invoice.

using InvoiceXml.Zugferd.XRechnung;

// Replace these values with your own.
const string ApiKey = "YOUR_API_KEY";
const string ZugferdPdfPath = @"C:\invoices\incoming-zugferd.pdf";
const string XRechnungXmlPath = @"C:\invoices\incoming-xrechnung.xml";
const string ReportPath = "incoming-zugferd-report.pdf";

using var client = new ZugferdXRechnungClient(ApiKey);

try
{
    // Read both files first, so a wrong path stops the example before any API call.
    var zugferdPdf = await File.ReadAllBytesAsync(ZugferdPdfPath);
    var xrechnungXml = await File.ReadAllBytesAsync(XRechnungXmlPath);

    // 1. Validate a ZUGFeRD PDF against the rules of the profile it declares.
    var zugferd = await client.ValidateZugferdAsync(zugferdPdf, Path.GetFileName(ZugferdPdfPath));
    Print("ZUGFeRD PDF", zugferd);

    // 2. Validate an XRechnung XML, UBL or CII, against EN 16931 and the German XRechnung rules.
    var xrechnung = await client.ValidateXRechnungAsync(xrechnungXml, Path.GetFileName(XRechnungXmlPath));
    Print("XRechnung XML", xrechnung);

    // 3. The same check as a printable PDF report, for example to send back to a supplier.
    var report = await client.ValidateZugferdReportAsync(zugferdPdf, Path.GetFileName(ZugferdPdfPath), PdfLanguage.De);
    await report.File.SaveAsync(ReportPath);
    Console.WriteLine($"Saved the validation report to {ReportPath} (valid: {report.Valid}).");
}
catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
{
    Console.WriteLine(ex.Message);
    Console.WriteLine("Set ZugferdPdfPath and XRechnungXmlPath at the top of Program.cs to files on your machine.");
}
catch (InvoiceXmlApiException ex)
{
    // For example an invalid API key, or a file that is not an invoice at all.
    Console.WriteLine($"The API refused the request: {ex.Message}");
}

static void Print(string label, InvoiceValidationResult result)
{
    Console.WriteLine($"{label}: {(result.Valid ? "valid" : "not valid")}. {result.Detail}");

    foreach (var error in result.Errors)
        Console.WriteLine($"  error   [{error.Rule}] {error.Message} ({string.Join(", ", error.Fields)})");

    // Warnings never make an invoice invalid, but are worth reviewing.
    foreach (var warning in result.Warnings)
        Console.WriteLine($"  warning [{warning.Rule}] {warning.Message} ({string.Join(", ", warning.Fields)})");
}
