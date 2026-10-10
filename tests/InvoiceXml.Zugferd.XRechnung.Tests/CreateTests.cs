using System.Net;
using System.Text.Json;

namespace InvoiceXml.Zugferd.XRechnung.Tests;

public class CreateTests
{
    [Fact]
    public async Task CreateZugferd_posts_invoice_envelope_with_bearer_key_and_returns_pdf()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice-zugferd-RE-2026-001.pdf");

        var file = await client.CreateZugferdAsync(
            TestData.Invoice(),
            new ZugferdOptions { Language = PdfLanguage.De, BrandColor = "#0F766E", IncludeAdvancedProperties = true });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.invoicexml.com/v1/create/zugferd", request.Uri.ToString());
        Assert.Equal("Bearer", request.Authorization?.Scheme);
        Assert.Equal("test-key", request.Authorization?.Parameter);
        Assert.StartsWith("InvoiceXml.Zugferd.XRechnung/", request.UserAgent);
        Assert.Equal("application/json", request.ContentType);

        Assert.Equal(TestData.Pdf, file.ToArray());
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("invoice-zugferd-RE-2026-001.pdf", file.FileName);
    }

    [Fact]
    public async Task CreateZugferd_serializes_the_api_wire_format()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        await client.CreateZugferdAsync(TestData.Invoice(), new ZugferdOptions { Language = PdfLanguage.De });

        using var json = JsonDocument.Parse(handler.Requests[0].Body);
        var invoice = json.RootElement.GetProperty("invoice");
        Assert.Equal("RE-2026-001", invoice.GetProperty("invoiceNumber").GetString());
        Assert.Equal("2026-10-09", invoice.GetProperty("issueDate").GetString());
        Assert.Equal("380", invoice.GetProperty("typeCode").GetString());
        Assert.Equal("EUR", invoice.GetProperty("currency").GetString());
        Assert.Equal("DE", invoice.GetProperty("seller").GetProperty("postalAddress").GetProperty("country").GetString());
        Assert.Equal("EM", invoice.GetProperty("seller").GetProperty("electronicAddress").GetProperty("schemeId").GetString());
        Assert.Equal("58", invoice.GetProperty("paymentDetails").GetProperty("paymentMeansCode").GetString());

        var notes = invoice.GetProperty("notes");
        Assert.Equal("Thank you for your order.", notes[0].GetString());
        Assert.Equal("REG", notes[1].GetProperty("subjectCode").GetString());

        var line = invoice.GetProperty("lines")[0];
        Assert.Equal(10m, line.GetProperty("quantity").GetDecimal());
        Assert.Equal("HUR", line.GetProperty("unitCode").GetString());
        Assert.Equal(95.50m, line.GetProperty("priceDetails").GetProperty("netPrice").GetDecimal());
        Assert.Equal("S", line.GetProperty("vatInformation").GetProperty("categoryCode").GetString());

        // Unset values are omitted rather than sent as null.
        Assert.False(invoice.TryGetProperty("dueDate", out _));
        Assert.False(invoice.TryGetProperty("totals", out _));

        var options = json.RootElement.GetProperty("options");
        Assert.Equal("de", options.GetProperty("language").GetString());
        Assert.False(options.TryGetProperty("syntax", out _));
    }

    [Fact]
    public async Task CreateZugferd_without_options_omits_the_options_object()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        await client.CreateZugferdAsync(TestData.Invoice());

        using var json = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.False(json.RootElement.TryGetProperty("options", out _));
    }

    [Fact]
    public async Task CreateXRechnung_posts_to_xrechnung_with_syntax_and_returns_xml()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Xml, "application/xml", "invoice-xrechnung.xml");

        var file = await client.CreateXRechnungAsync(TestData.Invoice(), new XRechnungOptions { Syntax = XRechnungSyntax.Cii });

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/create/xrechnung", request.Uri.AbsolutePath);
        using var json = JsonDocument.Parse(request.Body);
        Assert.Equal("cii", json.RootElement.GetProperty("options").GetProperty("syntax").GetString());
        Assert.Equal("04011000-12345-03", json.RootElement.GetProperty("invoice").GetProperty("buyerReference").GetString());
        Assert.Equal("application/xml", file.ContentType);
        Assert.Equal(TestData.Xml, file.ToArray());
    }

    [Fact]
    public async Task Create_rejected_by_validation_throws_with_findings()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(
            """
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "One or more validation errors occurred.",
              "status": 400,
              "detail": "The invoice failed validation with 1 error(s).",
              "valid": false,
              "errorCode": 4001,
              "traceId": "00-abc-01",
              "errors": [
                {
                  "rule": "BR-DE-15",
                  "layer": "cius",
                  "line": null,
                  "message": "The buyer reference (Leitweg-ID) is missing.",
                  "fields": ["buyerReference"],
                  "raw": "Original validator output."
                }
              ],
              "warnings": []
            }
            """,
            HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.CreateXRechnungAsync(TestData.Invoice()));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal((int)InvoiceXmlErrorCode.XmlValidation, ex.ErrorCode);
        Assert.True(ex.IsValidationError);
        Assert.Equal("00-abc-01", ex.TraceId);
        var finding = Assert.Single(ex.Findings);
        Assert.Equal("BR-DE-15", finding.Rule);
        Assert.Equal(ValidationLayers.Cius, finding.Layer);
        Assert.Null(finding.Line);
        Assert.Equal(["buyerReference"], finding.Fields);
        Assert.Contains("BR-DE-15", ex.Message);
        Assert.Contains("4001", ex.Message);
    }

    [Fact]
    public async Task Model_validation_errors_carry_field_paths_without_rule()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(
            """{"title":"One or more validation errors occurred.","status":400,"errorCode":4002,"errors":[{"rule":null,"layer":null,"line":1,"message":"The value is not a valid currency code.","fields":["currency"],"raw":null}]}""",
            HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.CreateZugferdAsync(TestData.Invoice()));

        Assert.True(ex.IsValidationError);
        var finding = Assert.Single(ex.Findings);
        Assert.Null(finding.Rule);
        Assert.Equal(1, finding.Line);
        Assert.Equal("The value is not a valid currency code.", finding.ToString());
    }

    [Fact]
    public async Task Non_json_error_body_still_produces_a_useful_exception()
    {
        var (client, handler) = TestData.CreateClient();
        handler.Reply(() => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>Bad gateway</html>") });

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.CreateZugferdAsync(TestData.Invoice()));

        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
        Assert.Empty(ex.Findings);
        Assert.Contains("502", ex.Message);
        Assert.Equal("<html>Bad gateway</html>", ex.ResponseBody);
    }

    [Fact]
    public async Task Create_rejects_null_invoice()
    {
        var (client, _) = TestData.CreateClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.CreateZugferdAsync(null!));
    }
}
