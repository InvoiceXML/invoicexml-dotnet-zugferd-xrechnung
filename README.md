# ZUGFeRD and XRechnung for .NET

[![NuGet](https://img.shields.io/nuget/v/InvoiceXml.Zugferd.XRechnung.svg)](https://www.nuget.org/packages/InvoiceXml.Zugferd.XRechnung)

Create, validate, embed, render and read German e-invoices from C#. `InvoiceXml.Zugferd.XRechnung` is a thin, typed client for the [InvoiceXML API](https://invoicexml.com): you pass an EN 16931 invoice object and get back a ZUGFeRD PDF/A-3 or an XRechnung XML that has already passed the official rules.

- **ZUGFeRD 2.x**: hybrid PDF/A-3 with the invoice XML embedded, EN 16931 profile by default, every official profile selectable.
- **XRechnung**: UBL or CII syntax, checked against EN 16931 and the KoSIT rules for German public sector buyers.
- **Validation** of ZUGFeRD PDFs and XRechnung XML from any producer, with plain-language findings and a field report.
- **Reading** of incoming invoices into the same object model, plus extraction of embedded attachments.

## Install

```bash
dotnet add package InvoiceXml.Zugferd.XRechnung
```

Targets .NET 8 and .NET Standard 2.0 (.NET Framework 4.6.2+, .NET Core, Mono). The only dependency is `System.Text.Json` on .NET Standard.

## Quick start

```csharp
using InvoiceXml.Zugferd.XRechnung;

using var client = new ZugferdXRechnungClient("YOUR_API_KEY");
var pdf = await client.CreateZugferdAsync(new InvoiceDocument
{
    InvoiceNumber = "RE-2026-001",
    Currency = CurrencyCode.EUR,
    Seller = new SellerParty { Name = "Muster GmbH", VatIdentifier = "DE123456789", PostalAddress = new PostalAddress { City = "Berlin", PostCode = "10115", Country = CountryCode.DE } },
    Buyer = new BuyerParty { Name = "Kunde AG", PostalAddress = new PostalAddress { City = "Hamburg", PostCode = "20095", Country = CountryCode.DE } },
    Lines = { new InvoiceLine { Quantity = 10, UnitCode = UnitCodes.Hour, Item = new ItemInformation { Name = "Consulting" }, PriceDetails = new PriceDetails { NetPrice = 95.50m }, VatInformation = new LineVatInformation { CategoryCode = VatCategoryCode.S, Rate = 19 } } },
});
await pdf.SaveAsync("invoice.pdf");
```

Totals, VAT breakdowns, the issue date and default payment terms are filled in by the API when you leave them empty. Get an API key with the **30-day free trial: 100 credits, no credit card required** at [invoicexml.com/signup](https://invoicexml.com/signup).

## Why InvoiceXML

- **Rule sets updated on their legal effective dates.** New EN 16931, KoSIT and FeRD releases switch server-side on the date they become binding. Your integration and this package stay the same: no package update, no redeploy.
- **Validated before delivery.** Every invoice is checked against the official rules before it is returned. Data that cannot yield a compliant invoice is refused with the violated rules and the fields to correct.
- **Zero data retention.** Documents are processed in memory and never stored. See [security](https://invoicexml.com/trust).
- **Processing in Frankfurt.** EU-only processing in Frankfurt am Main, Germany, GDPR compliant by design.
- **Integration assistance.** Our engineers help you map your data and get to production. [Contact us](https://invoicexml.com/contact).

## Usage

### Create an XRechnung

XRechnung adds German requirements on top of EN 16931: the Leitweg-ID in `BuyerReference`, a seller contact, street addresses, and electronic addresses for both parties.

```csharp
var invoice = new InvoiceDocument
{
    InvoiceNumber = "RE-2026-4408",
    Currency = CurrencyCode.EUR,
    DueDate = new DateTime(2026, 8, 27),
    BuyerReference = "04011000-12345-03", // Leitweg-ID
    InvoicingPeriod = new Period { StartDate = new DateTime(2026, 7, 1), EndDate = new DateTime(2026, 7, 31) },
    Seller = new SellerParty
    {
        Name = "Elbufer Systeme GmbH",
        VatIdentifier = "DE243871650",
        PostalAddress = new PostalAddress { Line1 = "Kaethe-Kollwitz-Ufer 21", City = "Dresden", PostCode = "01307", Country = CountryCode.DE },
        Contact = new Contact { Name = "Katrin Seidel", Phone = "+49 351 8802340", Email = "rechnung@elbufer-systeme.de" },
        ElectronicAddress = new SchemeIdentifier("rechnung@elbufer-systeme.de", "EM"),
    },
    Buyer = new BuyerParty
    {
        Name = "Landeshauptstadt Dresden",
        PostalAddress = new PostalAddress { Line1 = "Dr.-Kuelz-Ring 19", City = "Dresden", PostCode = "01067", Country = CountryCode.DE },
        ElectronicAddress = new SchemeIdentifier("04011000-12345-03", "0204"),
    },
    PaymentDetails = new PaymentDetails
    {
        PaymentMeansCode = PaymentMeansCodes.SepaCreditTransfer,
        PaymentAccountIdentifier = "DE27100777770209299700",
    },
    Lines =
    {
        new InvoiceLine
        {
            Quantity = 140,
            UnitCode = UnitCodes.Hour,
            Item = new ItemInformation { Name = "Operation of school administration software, July 2026" },
            PriceDetails = new PriceDetails { NetPrice = 98.00m },
            VatInformation = new LineVatInformation { CategoryCode = VatCategoryCode.S, Rate = 19 },
        },
    },
};

var xml = await client.CreateXRechnungAsync(invoice);                     // UBL
var cii = await client.CreateXRechnungAsync(invoice, new XRechnungOptions // or CII
{
    Syntax = XRechnungSyntax.Cii,
    Language = PdfLanguage.De,
});
await xml.SaveAsync(xml.FileName ?? "invoice.xml");
```

### Handle a refused invoice

When the data cannot produce a compliant invoice, the create methods throw `InvoiceXmlApiException` with every violated rule and the JSON path of the field to correct.

```csharp
try
{
    var pdf = await client.CreateZugferdAsync(invoice);
}
catch (InvoiceXmlApiException ex) when (ex.IsValidationError)
{
    foreach (var finding in ex.Findings)
        Console.WriteLine($"[{finding.Rule}] {finding.Message} ({string.Join(", ", finding.Fields)})");
}
```

### Branded ZUGFeRD PDFs, or your own layout

```csharp
var pdf = await client.CreateZugferdAsync(invoice, new ZugferdOptions
{
    Language = PdfLanguage.De,
    BrandColor = "#0F766E",
    LogoUrl = "https://example.com/logo.png",
});

// Keep your existing PDF layout: the API embeds the generated XML into it.
var hybrid = await client.CreateZugferdAsync(invoice, new ZugferdOptions { PdfUrl = "https://example.com/invoice-1234.pdf" });

// Or combine a PDF and a CII XML you already have.
var embedded = await client.EmbedZugferdAsync(File.ReadAllBytes("invoice.pdf"), File.ReadAllBytes("factur-x.xml"));
```

Set `InvoiceDocument.SpecificationId` to a value from `ZugferdProfiles` to create a profile other than EN 16931, for example `ZugferdProfiles.Extended`.

### Validate incoming invoices

A completed validation always returns a result. An invalid invoice is not an exception: check `Valid`.

```csharp
using var file = File.OpenRead("supplier-invoice.pdf");
var result = await client.ValidateZugferdAsync(file, "supplier-invoice.pdf");

if (result.Valid)
{
    Console.WriteLine($"Valid ZUGFeRD, profile {result.Data?.Profile}");
}
else
{
    foreach (var error in result.Errors)
        Console.WriteLine($"[{error.Rule}] ({error.Layer}) {error.Message}");
}
```

`ValidateXRechnungAsync` does the same for XRechnung XML in UBL or CII syntax. A finding with layer `cius` means the invoice meets EN 16931 but misses a German requirement, such as a missing Leitweg-ID. `result.Report` lists every invoice field with its value and the findings that concern it, ready for a review screen.

### Read invoices and attachments

```csharp
// ZUGFeRD PDF or XRechnung XML in, the same InvoiceDocument model out.
var received = await client.ExtractJsonAsync(File.ReadAllBytes("supplier-invoice.pdf"));
Console.WriteLine($"{received.InvoiceNumber} from {received.Seller?.Name}: {received.Totals?.DuePayableAmount} {received.Currency}");

// Embedded supporting documents (timesheets, delivery notes) as one ZIP.
var zip = await client.ExtractAttachmentsAsync(File.ReadAllBytes("rechnung.xml"));
await zip.SaveAsync("attachments.zip");

// A readable PDF of an XRechnung, for approval workflows and archives.
var preview = await client.RenderXRechnungToPdfAsync(File.ReadAllBytes("rechnung.xml"), new RenderOptions { Language = PdfLanguage.De });
```

An extracted `InvoiceDocument` can be passed straight back to a create method, for example to turn an incoming XRechnung into a ZUGFeRD PDF.

### ASP.NET Core and IHttpClientFactory

```csharp
builder.Services.AddHttpClient<IZugferdXRechnungClient, ZugferdXRechnungClient>((http, services) =>
    new ZugferdXRechnungClient(http, builder.Configuration["InvoiceXml:ApiKey"]!));
```

Inject `IZugferdXRechnungClient` where you need it, and fake it in your own unit tests. The client never disposes an `HttpClient` you pass in.

### Rate limits and retries

When more requests of your account run at the same time than your plan allows, the API answers HTTP 429 with a `Retry-After` header. Such a request is not processed and not billed. The client waits the requested interval and retries, up to three times by default:

```csharp
var client = new ZugferdXRechnungClient(new InvoiceXmlClientOptions
{
    ApiKey = "YOUR_API_KEY",
    MaxRetries = 5,
    MaxRetryDelay = TimeSpan.FromSeconds(60),
});
```

If the API still answers 429, the client throws `InvoiceXmlRateLimitException` with the `RetryAfter` interval. Every method takes a `CancellationToken`, which also cancels a pending retry wait.

### Errors

Any other refused request throws `InvoiceXmlApiException` with the HTTP `StatusCode`, the API's numeric `ErrorCode` (see `InvoiceXmlErrorCode`), `Title`, `Detail` and a `TraceId` to quote when contacting support. The full list is in the [error handling docs](https://invoicexml.com/docs/error-handling).

## Endpoints

| Method | Endpoint | Docs |
|---|---|---|
| `CreateZugferdAsync` | `POST /v1/create/zugferd` | [reference](https://invoicexml.com/docs/api/create/zugferd) |
| `CreateXRechnungAsync` | `POST /v1/create/xrechnung` | [reference](https://invoicexml.com/docs/api/create/xrechnung) |
| `ValidateZugferdAsync` | `POST /v1/validate/zugferd` | [reference](https://invoicexml.com/docs/api/validate/zugferd) |
| `ValidateXRechnungAsync` | `POST /v1/validate/xrechnung` | [reference](https://invoicexml.com/docs/api/validate/xrechnung) |
| `EmbedZugferdAsync` | `POST /v1/embed/zugferd` | [reference](https://invoicexml.com/docs/api/embed/zugferd) |
| `RenderXRechnungToPdfAsync` | `POST /v1/render/xrechnung/to/pdf` | [reference](https://invoicexml.com/docs/api/render/xrechnung/to/pdf) |
| `ExtractJsonAsync` | `POST /v1/extract/json` | [reference](https://invoicexml.com/docs/api/extract/json) |
| `ExtractAttachmentsAsync` | `POST /v1/extract/attachments` | [reference](https://invoicexml.com/docs/api/extract/attachments) |

The complete OpenAPI description is at [api.invoicexml.com/v1/openapi](https://api.invoicexml.com/v1/openapi), with an interactive explorer at [api.invoicexml.com/v1/scalar](https://api.invoicexml.com/v1/scalar/).

## Support

Questions, or a field you need for your integration? [Contact us](https://invoicexml.com/contact) and we will help you get it working.

## License

MIT. See [LICENSE](LICENSE).
