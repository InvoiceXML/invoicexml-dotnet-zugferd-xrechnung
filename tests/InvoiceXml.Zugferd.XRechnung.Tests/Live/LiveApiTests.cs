using System.Text;
using Xunit.Abstractions;

namespace InvoiceXml.Zugferd.XRechnung.Tests.Live;

/// <summary>
/// End-to-end checks against the production API. Skipped without an API key.
/// Run only these with: dotnet test --framework net8.0 --filter "FullyQualifiedName~Live"
/// </summary>
public class LiveApiTests(ITestOutputHelper output)
{
    [LiveFact]
    public async Task Readme_quick_start_creates_a_valid_zugferd_that_reads_back()
    {
        using var client = new ZugferdXRechnungClient(LiveApi.ApiKey!);

        var pdf = await Run(() => client.CreateZugferdAsync(LiveInvoices.QuickStart()));
        await LiveApi.SaveAsync(pdf, "quick-start-zugferd.pdf");
        Assert.Equal("application/pdf", pdf.ContentType);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(pdf.ToArray(), 0, 4));
        output.WriteLine($"Created {pdf.FileName} ({pdf.Length} bytes)");

        var validation = await client.ValidateZugferdAsync(pdf.ToArray(), pdf.FileName);
        Report(validation);
        Assert.True(validation.Valid, validation.Detail);
        Assert.True(validation.Data?.HasEmbeddedXml);
        Assert.Equal("en16931", validation.Data?.Profile);
        Assert.NotNull(validation.Report);

        var extracted = await client.ExtractJsonAsync(pdf.ToArray(), pdf.FileName);
        Assert.Equal("RE-2026-001", extracted.InvoiceNumber);
        Assert.Equal(CurrencyCode.EUR, extracted.Currency);
        Assert.Equal("Muster GmbH", extracted.Seller?.Name);
        Assert.Equal(CountryCode.DE, extracted.Buyer?.PostalAddress?.Country);
        Assert.Equal(VatCategoryCode.S, extracted.Lines[0].VatInformation?.CategoryCode);
        Assert.Equal(1136.45m, extracted.Totals?.GrandTotalAmount);
        output.WriteLine($"Extracted {extracted.InvoiceNumber}, due {extracted.Totals?.DuePayableAmount} {extracted.Currency}");
    }

    [LiveFact]
    public async Task Readme_xrechnung_creates_valid_ubl_and_cii_that_render()
    {
        using var client = new ZugferdXRechnungClient(LiveApi.ApiKey!);

        var ubl = await Run(() => client.CreateXRechnungAsync(LiveInvoices.XRechnung()));
        await LiveApi.SaveAsync(ubl, "xrechnung-ubl.xml");
        Assert.Contains("urn:oasis:names:specification:ubl", Encoding.UTF8.GetString(ubl.ToArray()));

        var cii = await Run(() => client.CreateXRechnungAsync(LiveInvoices.XRechnung(), new XRechnungOptions { Syntax = XRechnungSyntax.Cii, Language = PdfLanguage.De }));
        await LiveApi.SaveAsync(cii, "xrechnung-cii.xml");
        Assert.Contains("CrossIndustryInvoice", Encoding.UTF8.GetString(cii.ToArray()));

        var validation = await client.ValidateXRechnungAsync(ubl.ToArray(), ubl.FileName);
        Report(validation);
        Assert.True(validation.Valid, validation.Detail);
        Assert.Equal("xrechnung", validation.Data?.Profile);
        Assert.Empty(validation.Warnings);

        var preview = await Run(() => client.RenderXRechnungToPdfAsync(cii.ToArray(), new RenderOptions { Language = PdfLanguage.De }, cii.FileName));
        await LiveApi.SaveAsync(preview, "xrechnung-preview.pdf");
        Assert.Equal("application/pdf", preview.ContentType);
        Assert.StartsWith("%PDF", Encoding.ASCII.GetString(preview.ToArray(), 0, 4));

        // The rendered preview plus the CII XML become a hybrid PDF.
        var hybrid = await Run(() => client.EmbedZugferdAsync(preview.ToArray(), cii.ToArray(), fileName: "rechnung.pdf"));
        await LiveApi.SaveAsync(hybrid, "embedded-zugferd.pdf");
        Assert.Equal("application/pdf", hybrid.ContentType);
        output.WriteLine($"Embedded into {hybrid.FileName} ({hybrid.Length} bytes)");
    }

    [LiveFact]
    public async Task Embedded_supporting_document_comes_back_as_zip()
    {
        using var client = new ZugferdXRechnungClient(LiveApi.ApiKey!);
        var invoice = LiveInvoices.XRechnung();
        invoice.SupportingDocuments.Add(new SupportingDocument
        {
            Reference = "TIMESHEET-2026-07",
            Description = "Timesheet July 2026",
            Attachment = Encoding.UTF8.GetBytes("Date;Hours\n2026-07-01;8\n"),
            AttachmentMimeCode = "text/csv",
            AttachmentFilename = "timesheet-2026-07.csv",
        });

        var xml = await Run(() => client.CreateXRechnungAsync(invoice));
        var zip = await Run(() => client.ExtractAttachmentsAsync(xml.ToArray(), xml.FileName));
        await LiveApi.SaveAsync(zip, "attachments.zip");

        Assert.Equal("application/zip", zip.ContentType);
        Assert.Equal((byte)'P', zip.ToArray()[0]);
        Assert.Equal((byte)'K', zip.ToArray()[1]);
    }

    [LiveFact]
    public async Task Xrechnung_without_leitweg_id_is_refused_with_findings()
    {
        using var client = new ZugferdXRechnungClient(LiveApi.ApiKey!);
        var invoice = LiveInvoices.XRechnung();
        invoice.BuyerReference = null;

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.CreateXRechnungAsync(invoice));

        output.WriteLine(ex.Message);
        Assert.True(ex.IsValidationError);
        Assert.NotEmpty(ex.Findings);
        Assert.Contains(ex.Findings, f => f.Fields.Contains("buyerReference"));
        Assert.NotNull(ex.TraceId);
    }

    // Prints the API's findings before failing, so a refused request is easy to diagnose.
    private async Task<T> Run<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (InvoiceXmlApiException ex)
        {
            output.WriteLine(ex.Message);
            output.WriteLine(ex.ResponseBody ?? string.Empty);
            throw;
        }
    }

    private void Report(InvoiceValidationResult result)
    {
        output.WriteLine($"{(result.Valid ? "VALID" : "INVALID")}: {result.Detail} (profile {result.Data?.Profile})");
        foreach (var error in result.Errors)
            output.WriteLine($"  error {error}");
        foreach (var warning in result.Warnings)
            output.WriteLine($"  warning {warning}");
    }
}
