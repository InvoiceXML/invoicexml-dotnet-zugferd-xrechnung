// Converts a UBL invoice XML, for example one received over Peppol, into a ZUGFeRD invoice:
// a readable PDF with the invoice data embedded as CII XML.

using InvoiceXml.Zugferd.XRechnung;

// Replace these values with your own.
const string ApiKey = "YOUR_API_KEY";
const string UblXmlPath = @"C:\invoices\RE-2026-001-ubl.xml";
const string OutputPath = "RE-2026-001-zugferd.pdf";

using var client = new ZugferdXRechnungClient(ApiKey);

try
{
    var ubl = await File.ReadAllBytesAsync(UblXmlPath);

    // Language sets the PDF layout and the default texts added when the UBL has none.
    var zugferd = await client.ConvertUblToZugferdAsync(ubl, new ConvertToPdfOptions { Language = PdfLanguage.De }, Path.GetFileName(UblXmlPath));

    await zugferd.SaveAsync(OutputPath);
    Console.WriteLine($"Saved the ZUGFeRD invoice to {OutputPath}.");
}
catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
{
    Console.WriteLine($"Could not find {UblXmlPath}.");
    Console.WriteLine("Set UblXmlPath at the top of Program.cs to a UBL invoice XML on your machine.");
}
catch (InvoiceXmlApiException ex) when (ex.IsValidationError)
{
    // The result would not be a compliant ZUGFeRD invoice. Each finding names the field to fix.
    Console.WriteLine("The converted invoice would not be compliant:");
    foreach (var finding in ex.Findings)
        Console.WriteLine($"  [{finding.Rule}] {finding.Message} ({string.Join(", ", finding.Fields)})");
}
catch (InvoiceXmlApiException ex)
{
    Console.WriteLine($"The API refused the request: {ex.Message}");
}
