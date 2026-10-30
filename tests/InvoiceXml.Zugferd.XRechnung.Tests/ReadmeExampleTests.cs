using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceXml.Zugferd.XRechnung.Tests;

/// <summary>Keeps the README snippets compiling against the real API surface.</summary>
public class ReadmeExampleTests
{
    [Fact]
    public async Task Quick_start_invoice_is_sent_as_documented()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyFile(TestData.Pdf, "application/pdf", "invoice-zugferd-RE-2026-001.pdf");

        // Same statements as the README quick start, on a mocked transport.
        var pdf = await client.CreateZugferdAsync(new InvoiceDocument
        {
            InvoiceNumber = "RE-2026-001",
            Currency = CurrencyCode.EUR,
            Seller = new SellerParty { Name = "Muster GmbH", VatIdentifier = "DE123456789", PostalAddress = new PostalAddress { City = "Berlin", PostCode = "10115", Country = CountryCode.DE } },
            Buyer = new BuyerParty { Name = "Kunde AG", PostalAddress = new PostalAddress { City = "Hamburg", PostCode = "20095", Country = CountryCode.DE } },
            Lines = { new InvoiceLine { Quantity = 10, UnitCode = UnitCodes.Hour, Item = new ItemInformation { Name = "Consulting" }, PriceDetails = new PriceDetails { NetPrice = 95.50m }, VatInformation = new LineVatInformation { CategoryCode = VatCategoryCode.S, Rate = 19 } } },
        });

        using var json = JsonDocument.Parse(handler.Requests[0].Body);
        var invoice = json.RootElement.GetProperty("invoice");
        Assert.Equal("Muster GmbH", invoice.GetProperty("seller").GetProperty("name").GetString());
        Assert.Equal("Consulting", invoice.GetProperty("lines")[0].GetProperty("item").GetProperty("name").GetString());
        Assert.Equal("invoice-zugferd-RE-2026-001.pdf", pdf.FileName);
    }

    [Fact]
    public async Task Email_example_is_sent_as_documented()
    {
        var (client, handler) = TestData.CreateClient();
        handler.ReplyJson("""{"status":"accepted","messageId":"0102019a-test"}""");
        handler.ReplyJson("""{"status":"accepted","messageId":"0102019a-test2"}""");
        handler.ReplyJson("""{"status":"accepted","messageId":"0102019a-test3"}""");
        var invoice = TestData.Invoice();

        // Same statements as the README email section, on a mocked transport.
        var email = new EmailMessage
        {
            From = "invoices@yourcompany.com",
            FromName = "Your Company Billing",
            To = "accounting@customer.example",
            Cc = { "archive@yourcompany.com" },
            Subject = "Rechnung RE-2026-001",
            Message = "Anbei erhalten Sie Ihre Rechnung.",
        };

        var sent = await client.EmailZugferdAsync(invoice, email);
        await client.EmailXRechnungAsync(invoice, email, new XRechnungOptions { Syntax = XRechnungSyntax.Cii });
        await client.EmailEmbeddedZugferdAsync(TestData.Pdf, TestData.Xml, email);

        Assert.Equal("accepted", sent.Status);
        Assert.Equal("0102019a-test", sent.MessageId);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public void Http_client_factory_registration_resolves_the_interface()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<IZugferdXRechnungClient, ZugferdXRechnungClient>((http, _) =>
            new ZugferdXRechnungClient(http, "YOUR_API_KEY"));

        using var provider = services.BuildServiceProvider();

        Assert.IsType<ZugferdXRechnungClient>(provider.GetRequiredService<IZugferdXRechnungClient>());
    }
}
