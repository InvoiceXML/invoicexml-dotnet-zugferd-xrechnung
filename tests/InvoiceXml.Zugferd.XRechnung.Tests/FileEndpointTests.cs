using System.Net;

namespace InvoiceXml.Zugferd.XRechnung.Tests;

public class FileEndpointTests
{
    private const string InvalidResult =
        """
        {
          "valid": false,
          "detail": "Your invoice failed validation.",
          "data": {
            "hasEmbeddedXml": true,
            "schemaValid": true,
            "schematronValid": false,
            "conformanceLevel": "EN16931",
            "profile": "en16931",
            "customizationId": "urn:cen.eu:en16931:2017",
            "futureFlag": 42
          },
          "errors": [
            {
              "rule": "BR-CO-15",
              "layer": "en16931",
              "line": null,
              "message": "The total amount with VAT does not match.",
              "fields": ["totals.grandTotalAmount"],
              "raw": "Original validator output."
            }
          ],
          "warnings": [],
          "report": [
            {
              "name": "Invoice total amount with VAT",
              "section": "totals",
              "path": "totals.grandTotalAmount",
              "value": "1190.00",
              "isValid": false,
              "errors": [ { "rule": "BR-CO-15", "message": "The total amount with VAT does not match." } ],
              "warnings": []
            }
          ]
        }
        """;

    [Fact]
    public async Task ValidateZugferd_uploads_pdf_as_file_part_and_parses_the_verdict()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(InvalidResult);

        var result = await client.ValidateZugferdAsync(TestData.Pdf, "rechnung.pdf");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/validate/zugferd", request.Uri.AbsolutePath);
        var part = request.Part("file");
        Assert.Equal("rechnung.pdf", part.FileName);
        Assert.Equal("application/pdf", part.ContentType);
        Assert.Equal(TestData.Pdf, part.Body);

        Assert.False(result.Valid);
        Assert.Equal("en16931", result.Data?.Profile);
        Assert.True(result.Data?.HasEmbeddedXml);
        Assert.False(result.Data?.SchematronValid);
        Assert.True(result.Data?.AdditionalData?.ContainsKey("futureFlag"));

        var error = Assert.Single(result.Errors);
        Assert.Equal("BR-CO-15", error.Rule);
        Assert.Equal(ValidationLayers.En16931, error.Layer);
        Assert.Equal(["totals.grandTotalAmount"], error.Fields);
        Assert.Empty(result.Warnings);

        var row = Assert.Single(result.Report!);
        Assert.Equal("totals.grandTotalAmount", row.Path);
        Assert.False(row.IsValid);
        Assert.Equal("BR-CO-15", Assert.Single(row.Errors).Rule);
    }

    [Fact]
    public async Task ValidateXRechnung_uploads_xml_with_default_file_name()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson("""{"valid":true,"detail":"Your invoice is XRechnung compliant (EN16931 + CIUS-XR)","data":{"profile":"xrechnung"},"errors":[],"warnings":[],"report":null}""");

        var result = await client.ValidateXRechnungAsync(TestData.Xml);

        var part = handler.Requests[0].Part("file");
        Assert.Equal("/v1/validate/xrechnung", handler.Requests[0].Uri.AbsolutePath);
        Assert.Equal("invoice.xml", part.FileName);
        Assert.Equal("application/xml", part.ContentType);
        Assert.True(result.Valid);
        Assert.Empty(result.Errors);
        Assert.Null(result.Report);
    }

    [Fact]
    public async Task Stream_overloads_buffer_non_seekable_streams()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson("""{"valid":true,"errors":[],"warnings":[]}""");

        using var stream = new NonSeekableStream(TestData.Pdf);
        await client.ValidateZugferdAsync(stream);

        Assert.Equal(TestData.Pdf, handler.Requests[0].Part("file").Body);
        Assert.Equal("invoice.pdf", handler.Requests[0].Part("file").FileName);
    }

    [Fact]
    public async Task EmbedZugferd_sends_pdf_xml_and_skip_validation_parts()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "rechnung.pdf");

        var file = await client.EmbedZugferdAsync(TestData.Pdf, TestData.Xml, skipValidation: true, fileName: "rechnung.pdf");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/embed/zugferd", request.Uri.AbsolutePath);
        Assert.Equal(TestData.Pdf, request.Part("pdf").Body);
        Assert.Equal("rechnung.pdf", request.Part("pdf").FileName);
        Assert.Equal(TestData.Xml, request.Part("xml").Body);
        Assert.Equal("application/xml", request.Part("xml").ContentType);
        Assert.Equal("true", request.Part("skipValidation").Text);
        Assert.Equal("rechnung.pdf", file.FileName);
    }

    [Fact]
    public async Task EmbedZugferd_omits_skip_validation_by_default()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        await client.EmbedZugferdAsync(TestData.Pdf, TestData.Xml);

        Assert.DoesNotContain(handler.Requests[0].Parts, p => p.Name == "skipValidation");
    }

    [Fact]
    public async Task ExtractJson_returns_the_invoice_document()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(
            """
            {
              "invoice": {
                "invoiceNumber": "INV-2026-0042",
                "issueDate": "2026-06-14",
                "typeCode": "381",
                "currency": "EUR",
                "notes": ["Plain note", { "subjectCode": "AAB", "note": "Coded note" }],
                "seller": { "name": "Muster Lieferant GmbH", "postalAddress": { "country": "DE" }, "identifiers": [ { "identifier": "4000001000005", "schemeId": "0088" } ] },
                "buyer": { "name": "Client SARL", "postalAddress": { "country": "FR" } },
                "totals": { "grandTotalAmount": 1190.00, "duePayableAmount": "1190.00" },
                "lines": [ { "quantity": 1, "unitCode": "C62", "priceDetails": { "netPrice": 1000 }, "vatInformation": { "categoryCode": "S", "rate": 19 }, "item": { "name": "Widget", "countryOfOrigin": "XX-UNKNOWN" } } ]
              }
            }
            """);

        var invoice = await client.ExtractJsonAsync(TestData.Pdf);

        Assert.Equal("/v1/extract/json", handler.Requests[0].Uri.AbsolutePath);
        Assert.Equal("application/pdf", handler.Requests[0].Part("file").ContentType);
        Assert.Equal("INV-2026-0042", invoice.InvoiceNumber);
        Assert.Equal(new DateTime(2026, 6, 14), invoice.IssueDate);
        Assert.Equal(InvoiceTypeCode.CreditNote, invoice.TypeCode);
        Assert.Equal(CurrencyCode.EUR, invoice.Currency);
        Assert.Equal("Plain note", invoice.Notes[0].Note);
        Assert.Equal("AAB", invoice.Notes[1].SubjectCode);
        Assert.Equal(CountryCode.FR, invoice.Buyer?.PostalAddress?.Country);
        Assert.Equal("0088", invoice.Seller?.Identifiers[0].SchemeId);
        Assert.Equal(1190.00m, invoice.Totals?.DuePayableAmount);
        Assert.Equal(VatCategoryCode.S, invoice.Lines[0].VatInformation?.CategoryCode);

        // A code this package version does not know yet reads as null instead of failing.
        Assert.Null(invoice.Lines[0].Item?.CountryOfOrigin);
    }

    [Fact]
    public async Task ParseJson_returns_the_invoice_confidence_and_language()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(
            """
            {
              "invoice": { "invoiceNumber": "RE-2026-0815", "currency": "EUR", "lines": [ { "quantity": 2, "priceDetails": { "netPrice": 50 } } ] },
              "confidence": { "overall": 0.91, "areas": { "sellerIdentification": 0.98, "buyerIdentification": 0.95, "taxCalculation": 0.9, "lineItems": null } },
              "language": "de"
            }
            """);

        var parsed = await client.ParseJsonAsync(TestData.Pdf, "scan.pdf");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/parse/json", request.Uri.AbsolutePath);
        Assert.Equal(TestData.Pdf, request.Part("file").Body);
        Assert.Equal("scan.pdf", request.Part("file").FileName);
        Assert.Equal("RE-2026-0815", parsed.Invoice.InvoiceNumber);
        Assert.Equal(2m, parsed.Invoice.Lines[0].Quantity);
        Assert.Equal(0.91m, parsed.Confidence?.Overall);
        Assert.Equal(0.98m, parsed.Confidence?.Areas.SellerIdentification);
        Assert.Null(parsed.Confidence?.Areas.LineItems);
        Assert.Equal("de", parsed.Language);
    }

    [Fact]
    public async Task ParseJson_without_confidence_or_language_returns_nulls()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson("""{ "invoice": { "invoiceNumber": "RE-2026-0816" } }""");

        using var stream = new MemoryStream(TestData.Pdf);
        var parsed = await client.ParseJsonAsync(stream);

        Assert.Equal(TestData.Pdf, Assert.Single(handler.Requests).Part("file").Body);
        Assert.Equal("RE-2026-0816", parsed.Invoice.InvoiceNumber);
        Assert.Null(parsed.Confidence);
        Assert.Null(parsed.Language);
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.ParseJsonAsync((byte[])null!));
    }

    [Fact]
    public async Task ParseJson_does_not_retry_too_many_requests()
    {
        var (client, handler) = TestData.CreateClient();
        var delays = new List<TimeSpan>();
        client.Transport.Delay = (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        };
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(7));

        var ex = await Assert.ThrowsAsync<InvoiceXmlRateLimitException>(() => client.ParseJsonAsync(TestData.Pdf));

        Assert.Equal("/v1/parse/json", Assert.Single(handler.Requests).Uri.AbsolutePath);
        Assert.Empty(delays);
        Assert.Equal(TimeSpan.FromSeconds(7), ex.RetryAfter);
    }

    [Fact]
    public async Task ExtractJson_without_embedded_xml_throws_with_error_code()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(
            """{"title":"No embedded XML","status":400,"detail":"The PDF does not contain an embedded invoice XML.","errorCode":4006}""",
            HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.ExtractJsonAsync(TestData.Pdf));

        Assert.Equal((int)InvoiceXmlErrorCode.NoEmbeddedXml, ex.ErrorCode);
        Assert.False(ex.IsValidationError);
        Assert.Equal("The PDF does not contain an embedded invoice XML.", ex.Detail);
    }

    [Fact]
    public async Task ExtractAttachments_returns_the_zip_archive()
    {
        var (client, handler) = TestData.CreateClient();
        byte[] zip = [0x50, 0x4B, 0x03, 0x04];
        handler.ReplyFile(zip, "application/zip", "invoice-attachments.zip");

        var file = await client.ExtractAttachmentsAsync(TestData.Xml, "rechnung.xml");

        Assert.Equal("/v1/extract/attachments", handler.Requests[0].Uri.AbsolutePath);
        Assert.Equal("rechnung.xml", handler.Requests[0].Part("file").FileName);
        Assert.Equal("application/zip", file.ContentType);
        Assert.Equal("invoice-attachments.zip", file.FileName);
        Assert.Equal(zip, file.ToArray());
    }

    private sealed class NonSeekableStream(byte[] content) : MemoryStream(content)
    {
        public override bool CanSeek => false;

        public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
    }
}
