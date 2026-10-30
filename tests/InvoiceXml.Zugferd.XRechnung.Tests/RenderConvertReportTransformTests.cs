using System.Net;

namespace InvoiceXml.Zugferd.XRechnung.Tests;

public class RenderConvertReportTransformTests
{
    private const string RefusedInvoice =
        """{"title":"Validation failed","status":400,"detail":"The invoice is not compliant.","errorCode":4001,"errors":[{"rule":"BR-CO-15","layer":"en16931","line":null,"message":"Der Gesamtbetrag stimmt nicht.","fields":["totals.grandTotalAmount"],"raw":null}]}""";

    private const string MissingFile =
        """{"title":"Missing file","status":400,"detail":"No file was uploaded.","errorCode":4003}""";

    private static readonly Dictionary<string, Route<RenderOptions>> RenderRoutes = new()
    {
        ["xrechnung/to/pdf"] = new((c, f, o) => c.RenderXRechnungToPdfAsync(f, o), (c, s, o) => c.RenderXRechnungToPdfAsync(s, o)),
        ["cii/to/pdf"] = new((c, f, o) => c.RenderCiiToPdfAsync(f, o), (c, s, o) => c.RenderCiiToPdfAsync(s, o)),
    };

    private static readonly Dictionary<string, Route<ConvertOptions>> XmlRoutes = new()
    {
        ["zugferd/to/ubl"] = new((c, f, o) => c.ConvertZugferdToUblAsync(f, o), (c, s, o) => c.ConvertZugferdToUblAsync(s, o)),
        ["zugferd/to/cii"] = new((c, f, o) => c.ConvertZugferdToCiiAsync(f, o), (c, s, o) => c.ConvertZugferdToCiiAsync(s, o)),
        ["xrechnung/to/ubl"] = new((c, f, o) => c.ConvertXRechnungToUblAsync(f, o), (c, s, o) => c.ConvertXRechnungToUblAsync(s, o)),
        ["xrechnung/to/cii"] = new((c, f, o) => c.ConvertXRechnungToCiiAsync(f, o), (c, s, o) => c.ConvertXRechnungToCiiAsync(s, o)),
        ["ubl/to/xrechnung"] = new((c, f, o) => c.ConvertUblToXRechnungAsync(f, o), (c, s, o) => c.ConvertUblToXRechnungAsync(s, o)),
        ["cii/to/xrechnung"] = new((c, f, o) => c.ConvertCiiToXRechnungAsync(f, o), (c, s, o) => c.ConvertCiiToXRechnungAsync(s, o)),
    };

    private static readonly Dictionary<string, Route<ConvertToPdfOptions>> PdfRoutes = new()
    {
        ["cii/to/zugferd"] = new((c, f, o) => c.ConvertCiiToZugferdAsync(f, o), (c, s, o) => c.ConvertCiiToZugferdAsync(s, o)),
        ["ubl/to/zugferd"] = new((c, f, o) => c.ConvertUblToZugferdAsync(f, o), (c, s, o) => c.ConvertUblToZugferdAsync(s, o)),
        ["xrechnung/to/zugferd"] = new((c, f, o) => c.ConvertXRechnungToZugferdAsync(f, o), (c, s, o) => c.ConvertXRechnungToZugferdAsync(s, o)),
    };

    private delegate Task<InvoiceFile> ByteCall<TOptions>(IZugferdXRechnungClient client, byte[] file, TOptions? options)
        where TOptions : class;

    private delegate Task<InvoiceFile> StreamCall<TOptions>(IZugferdXRechnungClient client, Stream file, TOptions? options)
        where TOptions : class;

    [Theory]
    [InlineData("xrechnung/to/pdf")]
    [InlineData("cii/to/pdf")]
    public async Task Render_sends_every_option(string route)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        var file = await RenderRoutes[route].Bytes(
            client,
            TestData.Xml,
            new RenderOptions { Language = PdfLanguage.Fr, LogoUrl = "https://example.com/logo.png", IncludeQrCode = true });

        var request = Assert.Single(handler.Requests);
        Assert.Equal($"/v1/render/{route}", request.Uri.AbsolutePath);
        Assert.Equal(TestData.Xml, request.Part("file").Body);
        Assert.Equal("fr", request.Part("language").Text);
        Assert.Equal("https://example.com/logo.png", request.Part("logoUrl").Text);
        Assert.Equal("true", request.Part("includeQrCode").Text);
        Assert.Equal("application/pdf", file.ContentType);
    }

    [Theory]
    [InlineData("xrechnung/to/pdf")]
    [InlineData("cii/to/pdf")]
    public async Task Render_omits_unset_options(string route)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        await RenderRoutes[route].Bytes(client, TestData.Xml, null);

        Assert.Equal("file", Assert.Single(handler.Requests[0].Parts).Name);
    }

    [Fact]
    public async Task Render_sends_a_qr_code_set_to_false()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        await client.RenderCiiToPdfAsync(TestData.Xml, new RenderOptions { IncludeQrCode = false });

        Assert.Equal("false", handler.Requests[0].Part("includeQrCode").Text);
    }

    [Theory]
    [InlineData("xrechnung/to/pdf")]
    [InlineData("cii/to/pdf")]
    public async Task Render_stream_overload_uploads_the_stream(string route)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        using var stream = new MemoryStream(TestData.Xml);
        await RenderRoutes[route].Stream(client, stream, null);

        Assert.Equal($"/v1/render/{route}", handler.Requests[0].Uri.AbsolutePath);
        Assert.Equal(TestData.Xml, handler.Requests[0].Part("file").Body);
    }

    [Theory]
    [InlineData("zugferd/to/ubl")]
    [InlineData("zugferd/to/cii")]
    [InlineData("xrechnung/to/ubl")]
    [InlineData("xrechnung/to/cii")]
    [InlineData("ubl/to/xrechnung")]
    [InlineData("cii/to/xrechnung")]
    public async Task Convert_to_xml_sends_the_validation_language_and_no_language(string route)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Xml, "application/xml", "invoice.xml");

        var file = await XmlRoutes[route].Bytes(client, TestData.Xml, new ConvertOptions { ValidationLanguage = PdfLanguage.De });

        var request = Assert.Single(handler.Requests);
        Assert.Equal($"/v1/convert/{route}", request.Uri.AbsolutePath);
        Assert.Equal(TestData.Xml, request.Part("file").Body);
        Assert.Equal("de", request.Part("validationLanguage").Text);
        Assert.DoesNotContain(request.Parts, p => p.Name == "language");
        Assert.Equal("application/xml", file.ContentType);
    }

    [Theory]
    [InlineData("zugferd/to/ubl")]
    [InlineData("zugferd/to/cii")]
    [InlineData("xrechnung/to/ubl")]
    [InlineData("xrechnung/to/cii")]
    [InlineData("ubl/to/xrechnung")]
    [InlineData("cii/to/xrechnung")]
    public async Task Convert_to_xml_stream_overload_omits_unset_options(string route)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Xml, "application/xml", "invoice.xml");

        using var stream = new MemoryStream(TestData.Xml);
        await XmlRoutes[route].Stream(client, stream, null);

        Assert.Equal($"/v1/convert/{route}", handler.Requests[0].Uri.AbsolutePath);
        var part = Assert.Single(handler.Requests[0].Parts);
        Assert.Equal("file", part.Name);
        Assert.Equal(TestData.Xml, part.Body);
    }

    [Theory]
    [InlineData("cii/to/zugferd")]
    [InlineData("ubl/to/zugferd")]
    [InlineData("xrechnung/to/zugferd")]
    public async Task Convert_to_zugferd_sends_both_languages(string route)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        var file = await PdfRoutes[route].Bytes(
            client,
            TestData.Xml,
            new ConvertToPdfOptions { Language = PdfLanguage.De, ValidationLanguage = PdfLanguage.Fr });

        var request = Assert.Single(handler.Requests);
        Assert.Equal($"/v1/convert/{route}", request.Uri.AbsolutePath);
        Assert.Equal(TestData.Xml, request.Part("file").Body);
        Assert.Equal("de", request.Part("language").Text);
        Assert.Equal("fr", request.Part("validationLanguage").Text);
        Assert.Equal("application/pdf", file.ContentType);
    }

    [Theory]
    [InlineData("cii/to/zugferd")]
    [InlineData("ubl/to/zugferd")]
    [InlineData("xrechnung/to/zugferd")]
    public async Task Convert_to_zugferd_stream_overload_omits_unset_options(string route)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        using var stream = new MemoryStream(TestData.Xml);
        await PdfRoutes[route].Stream(client, stream, null);

        Assert.Equal($"/v1/convert/{route}", handler.Requests[0].Uri.AbsolutePath);
        Assert.Equal("file", Assert.Single(handler.Requests[0].Parts).Name);
    }

    [Fact]
    public async Task Convert_refusal_surfaces_the_findings()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(RefusedInvoice, HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() =>
            client.ConvertUblToXRechnungAsync(TestData.Xml, new ConvertOptions { ValidationLanguage = PdfLanguage.De }));

        Assert.Equal((int)InvoiceXmlErrorCode.XmlValidation, ex.ErrorCode);
        Assert.Equal("BR-CO-15", Assert.Single(ex.Findings).Rule);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, null)]
    public async Task ValidateZugferdReport_returns_the_pdf_and_the_verdict(string? header, bool? expected)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyReport(TestData.Pdf, header);

        var report = await client.ValidateZugferdReportAsync(TestData.Pdf, "rechnung.pdf", PdfLanguage.De);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/validate/zugferd/report", request.Uri.AbsolutePath);
        Assert.Equal("rechnung.pdf", request.Part("file").FileName);
        Assert.Equal("de", request.Part("validationLanguage").Text);
        Assert.Equal(expected, report.Valid);
        Assert.Equal(TestData.Pdf, report.File.ToArray());
        Assert.Equal("application/pdf", report.File.ContentType);
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData(null, null)]
    public async Task ValidateXRechnungReport_returns_the_pdf_and_the_verdict(string? header, bool? expected)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyReport(TestData.Pdf, header);

        var report = await client.ValidateXRechnungReportAsync(TestData.Xml);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/validate/xrechnung/report", request.Uri.AbsolutePath);
        var part = Assert.Single(request.Parts);
        Assert.Equal("file", part.Name);
        Assert.Equal("invoice.xml", part.FileName);
        Assert.Equal(expected, report.Valid);
    }

    [Fact]
    public async Task Report_with_an_unreadable_verdict_is_null()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyReport(TestData.Pdf, "maybe");

        var report = await client.ValidateZugferdReportAsync(TestData.Pdf);

        Assert.Null(report.Valid);
    }

    [Fact]
    public async Task Report_stream_overloads_upload_the_stream()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyReport(TestData.Pdf, "true").ReplyReport(TestData.Pdf, "false");

        using var pdf = new MemoryStream(TestData.Pdf);
        using var xml = new MemoryStream(TestData.Xml);
        await client.ValidateZugferdReportAsync(pdf);
        await client.ValidateXRechnungReportAsync(xml);

        Assert.Equal("/v1/validate/zugferd/report", handler.Requests[0].Uri.AbsolutePath);
        Assert.Equal(TestData.Pdf, handler.Requests[0].Part("file").Body);
        Assert.Equal("/v1/validate/xrechnung/report", handler.Requests[1].Uri.AbsolutePath);
        Assert.Equal(TestData.Xml, handler.Requests[1].Part("file").Body);
    }

    [Fact]
    public async Task Report_problem_response_throws()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(MissingFile, HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.ValidateZugferdReportAsync(TestData.Pdf));

        Assert.Equal((int)InvoiceXmlErrorCode.MissingFile, ex.ErrorCode);
    }

    [Fact]
    public async Task TransformToZugferd_sends_every_option()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "rechnung.pdf");

        var file = await client.TransformToZugferdAsync(
            TestData.Pdf,
            new TransformOptions
            {
                Language = PdfLanguage.De,
                PreserveOriginalPdf = true,
                IncludeAdvancedProperties = false,
                ValidationLanguage = PdfLanguage.Fr,
            },
            "rechnung.pdf");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/transform/to/zugferd", request.Uri.AbsolutePath);
        Assert.Equal("rechnung.pdf", request.Part("file").FileName);
        Assert.Equal("de", request.Part("language").Text);
        Assert.Equal("true", request.Part("preserveOriginalPdf").Text);
        Assert.Equal("false", request.Part("includeAdvancedProperties").Text);
        Assert.Equal("fr", request.Part("validationLanguage").Text);
        Assert.Equal("rechnung.pdf", file.FileName);
    }

    [Fact]
    public async Task TransformToZugferd_omits_unset_options()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf").ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        await client.TransformToZugferdAsync(TestData.Pdf);
        await client.TransformToZugferdAsync(TestData.Pdf, new TransformOptions());

        Assert.All(handler.Requests, request => Assert.Equal("file", Assert.Single(request.Parts).Name));
    }

    [Fact]
    public async Task TransformToXRechnung_sends_the_buyer_reference_and_no_language()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Xml, "application/xml", "invoice.xml");

        var file = await client.TransformToXRechnungAsync(
            TestData.Pdf,
            "04011000-12345-03",
            new TransformToXRechnungOptions { ValidationLanguage = PdfLanguage.De });

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/transform/to/xrechnung", request.Uri.AbsolutePath);
        Assert.Equal(TestData.Pdf, request.Part("file").Body);
        Assert.Equal("04011000-12345-03", request.Part("buyerReference").Text);
        Assert.Equal("de", request.Part("validationLanguage").Text);
        Assert.DoesNotContain(request.Parts, p => p.Name == "language");
        Assert.Equal("application/xml", file.ContentType);
    }

    [Fact]
    public async Task TransformToXRechnung_omits_unset_options()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Xml, "application/xml", "invoice.xml");

        await client.TransformToXRechnungAsync(TestData.Pdf, "04011000-12345-03");

        Assert.Equal(["file", "buyerReference"], handler.Requests[0].Parts.Select(p => p.Name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task TransformToXRechnung_requires_a_buyer_reference(string? buyerReference)
    {
        var (client, handler) = TestData.CreateClient();

        await Assert.ThrowsAnyAsync<ArgumentException>(() => client.TransformToXRechnungAsync(TestData.Pdf, buyerReference!));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Transform_stream_overloads_upload_the_stream()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf").ReplyFile(TestData.Xml, "application/xml", "invoice.xml");

        using var first = new MemoryStream(TestData.Pdf);
        using var second = new MemoryStream(TestData.Pdf);
        await client.TransformToZugferdAsync(first);
        await client.TransformToXRechnungAsync(second, "04011000-12345-03");

        Assert.Equal("/v1/transform/to/zugferd", handler.Requests[0].Uri.AbsolutePath);
        Assert.Equal(TestData.Pdf, handler.Requests[0].Part("file").Body);
        Assert.Equal("/v1/transform/to/xrechnung", handler.Requests[1].Uri.AbsolutePath);
        Assert.Equal("04011000-12345-03", handler.Requests[1].Part("buyerReference").Text);
    }

    [Fact]
    public async Task Transform_refusal_surfaces_the_findings()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(RefusedInvoice, HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.TransformToZugferdAsync(TestData.Pdf));

        Assert.Equal("BR-CO-15", Assert.Single(ex.Findings).Rule);
    }

    [Theory]
    [InlineData("zugferd")]
    [InlineData("xrechnung")]
    public async Task Transform_too_many_requests_makes_one_request_and_throws(string route)
    {
        var (client, handler) = TestData.CreateClient();
        var delays = new List<TimeSpan>();
        client.Transport.Delay = (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        };
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(7));

        var ex = await Assert.ThrowsAsync<InvoiceXmlRateLimitException>(() => route == "zugferd"
            ? client.TransformToZugferdAsync(TestData.Pdf)
            : client.TransformToXRechnungAsync(TestData.Pdf, "04011000-12345-03"));

        Assert.Equal($"/v1/transform/to/{route}", Assert.Single(handler.Requests).Uri.AbsolutePath);
        Assert.Empty(delays);
        Assert.Equal((HttpStatusCode)429, ex.StatusCode);
        Assert.Equal(TimeSpan.FromSeconds(7), ex.RetryAfter);
    }

    [Fact]
    public async Task Render_convert_and_report_still_retry_too_many_requests()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(1)).ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(1)).ReplyFile(TestData.Xml, "application/xml", "invoice.xml");
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(1)).ReplyReport(TestData.Pdf, "true");

        await client.RenderCiiToPdfAsync(TestData.Xml);
        await client.ConvertZugferdToUblAsync(TestData.Pdf);
        await client.ValidateZugferdReportAsync(TestData.Pdf);

        Assert.Equal(
            ["/v1/render/cii/to/pdf", "/v1/render/cii/to/pdf", "/v1/convert/zugferd/to/ubl", "/v1/convert/zugferd/to/ubl", "/v1/validate/zugferd/report", "/v1/validate/zugferd/report"],
            handler.Requests.Select(r => r.Uri.AbsolutePath));
    }

    [Fact]
    public async Task Null_files_are_refused_before_any_request()
    {
        var (client, handler) = TestData.CreateClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.RenderCiiToPdfAsync((byte[])null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.ConvertZugferdToUblAsync((byte[])null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.ConvertCiiToZugferdAsync((byte[])null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.ValidateZugferdReportAsync((byte[])null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.TransformToZugferdAsync((byte[])null!));

        Assert.Empty(handler.Requests);
    }

    private sealed record Route<TOptions>(ByteCall<TOptions> Bytes, StreamCall<TOptions> Stream)
        where TOptions : class;
}
