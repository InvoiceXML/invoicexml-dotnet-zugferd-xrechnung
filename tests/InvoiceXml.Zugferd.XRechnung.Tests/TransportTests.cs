using System.Net;

namespace InvoiceXml.Zugferd.XRechnung.Tests;

public class TransportTests
{
    [Fact]
    public async Task Too_many_requests_is_retried_after_the_retry_after_interval()
    {
        var (client, handler) = TestData.CreateClient();
        var delays = new List<TimeSpan>();
        client.Transport.Delay = (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        };
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(2));
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        var file = await client.EmbedZugferdAsync(TestData.Pdf, TestData.Xml);

        Assert.Equal([TimeSpan.FromSeconds(2)], delays);
        Assert.Equal(2, handler.Requests.Count);
        // The upload is rebuilt for the retry, not sent empty.
        Assert.Equal(TestData.Pdf, handler.Requests[1].Part("pdf").Body);
        Assert.Equal(TestData.Pdf, file.ToArray());
    }

    [Fact]
    public async Task Json_body_is_resent_on_retry()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(1));
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice.pdf");

        await client.CreateZugferdAsync(TestData.Invoice());

        Assert.Equal(handler.Requests[0].Body, handler.Requests[1].Body);
        Assert.NotEmpty(handler.Requests[1].Body);
    }

    [Fact]
    public async Task Missing_retry_after_falls_back_to_exponential_backoff()
    {
        var (client, handler) = TestData.CreateClient();
        var delays = new List<TimeSpan>();
        client.Transport.Delay = (delay, _) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        };
        handler.ReplyTooManyRequests(null).ReplyTooManyRequests(null);
        handler.ReplyJson("""{"valid":true,"errors":[],"warnings":[]}""");

        await client.ValidateXRechnungAsync(TestData.Xml);

        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2)], delays);
    }

    [Fact]
    public async Task Exhausted_retries_throw_rate_limit_exception()
    {
        var (client, handler) = TestData.CreateClient(new InvoiceXmlClientOptions { ApiKey = "k", MaxRetries = 2 });
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(1))
            .ReplyTooManyRequests(TimeSpan.FromSeconds(1))
            .ReplyTooManyRequests(TimeSpan.FromSeconds(5));

        var ex = await Assert.ThrowsAsync<InvoiceXmlRateLimitException>(() => client.ValidateZugferdAsync(TestData.Pdf));

        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(TimeSpan.FromSeconds(5), ex.RetryAfter);
        Assert.Equal((HttpStatusCode)429, ex.StatusCode);
        Assert.Equal((int)InvoiceXmlErrorCode.TooManyRequests, ex.ErrorCode);
    }

    [Fact]
    public async Task Retry_after_longer_than_the_maximum_delay_throws_without_waiting()
    {
        var (client, handler) = TestData.CreateClient(new InvoiceXmlClientOptions { ApiKey = "k", MaxRetryDelay = TimeSpan.FromSeconds(10) });
        var waited = false;
        client.Transport.Delay = (_, _) =>
        {
            waited = true;
            return Task.CompletedTask;
        };
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(120));

        var ex = await Assert.ThrowsAsync<InvoiceXmlRateLimitException>(() => client.ValidateZugferdAsync(TestData.Pdf));

        Assert.False(waited);
        Assert.Single(handler.Requests);
        Assert.Equal(TimeSpan.FromSeconds(120), ex.RetryAfter);
    }

    [Fact]
    public async Task Zero_max_retries_disables_retrying()
    {
        var (client, handler) = TestData.CreateClient(new InvoiceXmlClientOptions { ApiKey = "k", MaxRetries = 0 });
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(1));

        await Assert.ThrowsAsync<InvoiceXmlRateLimitException>(() => client.ValidateZugferdAsync(TestData.Pdf));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Cancellation_is_honoured_while_waiting_to_retry()
    {
        var (client, handler) = TestData.CreateClient();
        client.Transport.Delay = Task.Delay;
        handler.ReplyTooManyRequests(TimeSpan.FromSeconds(20));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.ValidateZugferdAsync(TestData.Pdf, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task Cancelled_token_stops_the_request()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson("""{"valid":true,"errors":[],"warnings":[]}""");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.ValidateZugferdAsync(TestData.Pdf, cancellationToken: new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task Custom_base_address_with_path_is_respected()
    {
        var (client, handler) = TestData.CreateClient(new InvoiceXmlClientOptions { ApiKey = "k", BaseAddress = new Uri("https://proxy.example.com/invoicexml") });
        handler.ReplyJson("""{"valid":true,"errors":[],"warnings":[]}""");

        await client.ValidateXRechnungAsync(TestData.Xml);

        Assert.Equal("https://proxy.example.com/invoicexml/v1/validate/xrechnung", handler.Requests[0].Uri.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_api_key_is_rejected(string? apiKey)
    {
        Assert.Throws<ArgumentException>(() => new ZugferdXRechnungClient(new HttpClient(), apiKey!));
    }

    [Fact]
    public void Negative_max_retries_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ZugferdXRechnungClient(new HttpClient(), new InvoiceXmlClientOptions { ApiKey = "k", MaxRetries = -1 }));
    }

    [Fact]
    public async Task Dispose_leaves_a_caller_supplied_http_client_usable()
    {
        var handler = new MockHandler().ReplyJson("""{"valid":true,"errors":[],"warnings":[]}""");
        using var http = new HttpClient(handler);
        var client = new ZugferdXRechnungClient(http, "k");

        client.Dispose();

        using var response = await http.PostAsync(new Uri("https://api.invoicexml.com/v1/validate/xrechnung"), new StringContent(""));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task InvoiceFile_saves_to_disk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"invoicexml-{Guid.NewGuid():N}.pdf");
        try
        {
            await new InvoiceFile(TestData.Pdf, "application/pdf").SaveAsync(path);
            Assert.Equal(TestData.Pdf, File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
