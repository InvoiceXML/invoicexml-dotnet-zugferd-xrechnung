using System.Net;
using System.Text.Json;

namespace InvoiceXml.Zugferd.XRechnung.Tests;

public class EmailTests
{
    private const string Accepted = """{"status":"accepted","messageId":"0102019a-test"}""";

    private static EmailMessage FullEmail() => new()
    {
        From = "invoices@muster.de",
        FromName = "Muster Billing",
        To = "accounting@kunde.de",
        Cc = { "archive@muster.de", "boss@muster.de" },
        Bcc = { "audit@muster.de" },
        ReplyTo = "support@muster.de",
        Subject = "Rechnung RE-2026-001",
        Message = "Anbei erhalten Sie Ihre Rechnung.",
    };

    private static EmailMessage MinimalEmail() => new() { From = "invoices@muster.de", To = "accounting@kunde.de" };

    [Fact]
    public async Task EmailZugferd_posts_invoice_options_and_email_and_returns_the_message_id()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(Accepted);

        var result = await client.EmailZugferdAsync(TestData.Invoice(), FullEmail(), new ZugferdOptions { Language = PdfLanguage.De });

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.invoicexml.com/v1/create/zugferd/email", request.Uri.ToString());
        Assert.Equal("test-key", request.Authorization?.Parameter);
        Assert.Equal("application/json", request.ContentType);

        using var json = JsonDocument.Parse(request.Body);
        Assert.Equal("RE-2026-001", json.RootElement.GetProperty("invoice").GetProperty("invoiceNumber").GetString());
        Assert.Equal("de", json.RootElement.GetProperty("options").GetProperty("language").GetString());

        var email = json.RootElement.GetProperty("email");
        Assert.Equal("invoices@muster.de", email.GetProperty("from").GetString());
        Assert.Equal("Muster Billing", email.GetProperty("fromName").GetString());
        Assert.Equal("accounting@kunde.de", email.GetProperty("to").GetString());
        Assert.Equal("archive@muster.de", email.GetProperty("cc")[0].GetString());
        Assert.Equal("boss@muster.de", email.GetProperty("cc")[1].GetString());
        Assert.Equal("audit@muster.de", email.GetProperty("bcc")[0].GetString());
        Assert.Equal("support@muster.de", email.GetProperty("replyTo").GetString());
        Assert.Equal("Rechnung RE-2026-001", email.GetProperty("subject").GetString());
        Assert.Equal("Anbei erhalten Sie Ihre Rechnung.", email.GetProperty("message").GetString());

        Assert.Equal("accepted", result.Status);
        Assert.Equal("0102019a-test", result.MessageId);
    }

    [Fact]
    public async Task EmailXRechnung_posts_to_the_xrechnung_email_route_with_syntax()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(Accepted);

        var result = await client.EmailXRechnungAsync(TestData.Invoice(), MinimalEmail(), new XRechnungOptions { Syntax = XRechnungSyntax.Cii });

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/create/xrechnung/email", request.Uri.AbsolutePath);
        using var json = JsonDocument.Parse(request.Body);
        Assert.Equal("cii", json.RootElement.GetProperty("options").GetProperty("syntax").GetString());
        Assert.Equal("04011000-12345-03", json.RootElement.GetProperty("invoice").GetProperty("buyerReference").GetString());
        Assert.Equal("accounting@kunde.de", json.RootElement.GetProperty("email").GetProperty("to").GetString());
        Assert.Equal("0102019a-test", result.MessageId);
    }

    [Fact]
    public async Task Create_email_omits_unset_fields()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(Accepted);

        await client.EmailZugferdAsync(TestData.Invoice(), MinimalEmail());

        using var json = JsonDocument.Parse(handler.Requests[0].Body);
        Assert.False(json.RootElement.TryGetProperty("options", out _));
        var names = json.RootElement.GetProperty("email").EnumerateObject().Select(p => p.Name).ToArray();
        Assert.Equal(["from", "to"], names);
    }

    [Fact]
    public async Task Blank_copy_addresses_are_dropped()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(Accepted);
        var email = MinimalEmail();
        email.Cc = [" ", " archive@muster.de "];
        email.Bcc = [""];

        await client.EmailZugferdAsync(TestData.Invoice(), email);

        using var json = JsonDocument.Parse(handler.Requests[0].Body);
        var body = json.RootElement.GetProperty("email");
        Assert.Equal("archive@muster.de", Assert.Single(body.GetProperty("cc").EnumerateArray()).GetString());
        Assert.False(body.TryGetProperty("bcc", out _));
    }

    [Fact]
    public async Task EmailEmbeddedZugferd_sends_files_and_email_fields()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(Accepted);

        var result = await client.EmailEmbeddedZugferdAsync(TestData.Pdf, TestData.Xml, FullEmail(), "rechnung.pdf");

        var request = Assert.Single(handler.Requests);
        Assert.Equal("/v1/embed/zugferd/email", request.Uri.AbsolutePath);
        Assert.Equal(TestData.Pdf, request.Part("pdf").Body);
        Assert.Equal("rechnung.pdf", request.Part("pdf").FileName);
        Assert.Equal(TestData.Xml, request.Part("xml").Body);
        Assert.Equal("application/xml", request.Part("xml").ContentType);
        Assert.Equal("invoices@muster.de", request.Part("email.from").Text);
        Assert.Equal("Muster Billing", request.Part("email.fromName").Text);
        Assert.Equal("accounting@kunde.de", request.Part("email.to").Text);
        Assert.Equal("audit@muster.de", request.Part("email.bcc").Text);
        Assert.Equal("support@muster.de", request.Part("email.replyTo").Text);
        Assert.Equal("Rechnung RE-2026-001", request.Part("email.subject").Text);
        Assert.Equal("Anbei erhalten Sie Ihre Rechnung.", request.Part("email.message").Text);

        // These routes always validate.
        Assert.DoesNotContain(request.Parts, p => p.Name == "skipValidation");

        Assert.Equal("accepted", result.Status);
        Assert.Equal("0102019a-test", result.MessageId);
    }

    [Fact]
    public async Task EmailEmbeddedZugferd_sends_one_form_part_per_cc_address()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(Accepted);

        await client.EmailEmbeddedZugferdAsync(TestData.Pdf, TestData.Xml, FullEmail());

        var cc = handler.Requests[0].Parts.Where(p => p.Name == "email.cc").Select(p => p.Text).ToArray();
        Assert.Equal(["archive@muster.de", "boss@muster.de"], cc);
    }

    [Fact]
    public async Task EmailEmbeddedZugferd_omits_unset_fields()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(Accepted);

        await client.EmailEmbeddedZugferdAsync(TestData.Pdf, TestData.Xml, MinimalEmail());

        var names = handler.Requests[0].Parts.Select(p => p.Name!).ToArray();
        Assert.Equal(["pdf", "xml", "email.from", "email.to"], names);
        Assert.Equal("invoice.pdf", handler.Requests[0].Part("pdf").FileName);
    }

    [Fact]
    public async Task EmailEmbeddedZugferd_sends_a_non_ascii_subject_and_sender_name_as_utf8()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(Accepted);
        var email = MinimalEmail();
        email.FromName = "Müller & Söhne";
        email.Subject = "Rechnung für März";

        await client.EmailEmbeddedZugferdAsync(TestData.Pdf, TestData.Xml, email);

        Assert.Equal("Müller & Söhne", handler.Requests[0].Part("email.fromName").Text);
        Assert.Equal("Rechnung für März", handler.Requests[0].Part("email.subject").Text);
    }

    [Fact]
    public async Task Stream_overload_of_embed_email_buffers_the_uploads()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(Accepted);

        using var pdf = new MemoryStream(TestData.Pdf);
        using var xml = new MemoryStream(TestData.Xml);
        await client.EmailEmbeddedZugferdAsync(pdf, xml, MinimalEmail());

        Assert.Equal(TestData.Pdf, handler.Requests[0].Part("pdf").Body);
        Assert.Equal(TestData.Xml, handler.Requests[0].Part("xml").Body);
    }

    [Fact]
    public async Task Unverified_sender_domain_surfaces_through_the_exception()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(
            """{"title":"Sending domain not verified","status":400,"detail":"The sending domain muster.de is not verified yet.","errorCode":4022,"traceId":"00-abc-01"}""",
            HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.EmailZugferdAsync(TestData.Invoice(), MinimalEmail()));

        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
        Assert.Equal((int)InvoiceXmlErrorCode.EmailDomainNotVerified, ex.ErrorCode);
        Assert.False(ex.IsValidationError);
        Assert.Equal("The sending domain muster.de is not verified yet.", ex.Detail);
        Assert.Contains("4022", ex.Message);
    }

    [Theory]
    [InlineData(4020, InvoiceXmlErrorCode.EmailNotInPlan)]
    [InlineData(4021, InvoiceXmlErrorCode.EmailSendingDisabled)]
    public async Task Refused_email_sending_surfaces_its_error_code(int code, InvoiceXmlErrorCode expected)
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson($$"""{"title":"Refused","status":403,"detail":"Refused.","errorCode":{{code}}}""", HttpStatusCode.Forbidden);

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.EmailEmbeddedZugferdAsync(TestData.Pdf, TestData.Xml, MinimalEmail()));

        Assert.Equal(HttpStatusCode.Forbidden, ex.StatusCode);
        Assert.Equal((int)expected, ex.ErrorCode);
    }

    [Fact]
    public async Task Bad_address_surfaces_as_a_field_finding()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson(
            """{"title":"One or more validation errors occurred.","status":400,"errorCode":4002,"errors":[{"rule":null,"layer":null,"line":null,"message":"The address is not valid.","fields":["email.to"],"raw":null}]}""",
            HttpStatusCode.BadRequest);

        var ex = await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.EmailXRechnungAsync(TestData.Invoice(), MinimalEmail()));

        Assert.True(ex.IsValidationError);
        Assert.Equal(["email.to"], Assert.Single(ex.Findings).Fields);
    }

    [Fact]
    public async Task Too_many_requests_is_retried_because_nothing_was_sent()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(1));
        handler.ReplyJson(Accepted);

        var result = await client.EmailEmbeddedZugferdAsync(TestData.Pdf, TestData.Xml, FullEmail());

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("accounting@kunde.de", handler.Requests[1].Part("email.to").Text);
        Assert.Equal("accepted", result.Status);
    }

    [Fact]
    public async Task Server_error_is_not_retried_because_the_email_may_have_gone_out()
    {
        var (client, handler) = TestData.CreateClient();
        handler.Reply(() => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>Bad gateway</html>") });

        await Assert.ThrowsAsync<InvoiceXmlApiException>(() => client.EmailZugferdAsync(TestData.Invoice(), MinimalEmail()));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Network_failure_is_not_retried_because_the_email_may_have_gone_out()
    {
        var (client, handler) = TestData.CreateClient();
        handler.Reply(() => throw new HttpRequestException("Connection reset."));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.EmailEmbeddedZugferdAsync(TestData.Pdf, TestData.Xml, MinimalEmail()));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Email_methods_reject_null_arguments()
    {
        var (client, _) = TestData.CreateClient();

        await Assert.ThrowsAsync<ArgumentNullException>(() => client.EmailZugferdAsync(null!, MinimalEmail()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.EmailZugferdAsync(TestData.Invoice(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.EmailXRechnungAsync(null!, MinimalEmail()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.EmailXRechnungAsync(TestData.Invoice(), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.EmailEmbeddedZugferdAsync((byte[])null!, TestData.Xml, MinimalEmail()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.EmailEmbeddedZugferdAsync(TestData.Pdf, (byte[])null!, MinimalEmail()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.EmailEmbeddedZugferdAsync(TestData.Pdf, TestData.Xml, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.EmailEmbeddedZugferdAsync(Stream.Null, Stream.Null, null!));
    }
}
