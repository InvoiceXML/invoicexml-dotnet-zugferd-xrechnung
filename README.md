# ZUGFeRD and XRechnung for .NET

[![NuGet](https://img.shields.io/nuget/v/InvoiceXml.Zugferd.XRechnung.svg)](https://www.nuget.org/packages/InvoiceXml.Zugferd.XRechnung)

Create, validate, embed, render, convert and read German e-invoices from C#. `InvoiceXml.Zugferd.XRechnung` is a thin, typed client for the [InvoiceXML API](https://www.invoicexml.com): you pass an EN 16931 invoice object and get back a ZUGFeRD PDF/A-3 or an XRechnung XML that has already passed the official rules.

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

Totals, VAT breakdowns, the issue date and default payment terms are filled in by the API when you leave them empty. Get an API key with the **30-day free trial: 100 credits, no credit card required** at [www.invoicexml.com/account/signup](https://www.invoicexml.com/account/signup).

## Why InvoiceXML

- **Rule sets updated on their legal effective dates.** New EN 16931, KoSIT and FeRD releases switch server-side on the date they become binding. Your integration and this package stay the same: no package update, no redeploy.
- **Validated before delivery.** Every invoice is checked against the official rules before it is returned. Data that cannot yield a compliant invoice is refused with the violated rules and the fields to correct.
- **Zero data retention.** Documents are processed in memory and never stored. See [security](https://www.invoicexml.com/trust). When you email an invoice, it goes to our email service for delivery, and we keep no copy.
- **Processing in Frankfurt.** EU-only processing in Frankfurt am Main, Germany, GDPR compliant by design.
- **Integration assistance.** Our engineers help you map your data and get to production. [Contact us](https://www.invoicexml.com/contact).

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

### Create a UBL invoice

The same invoice object also produces a UBL invoice for other networks and countries. `Profile` takes a value from `UblProfiles` and defaults to Peppol BIS Billing 3.0.

```csharp
var ubl = await client.CreateUblAsync(invoice, new UblOptions { Profile = UblProfiles.PeppolBis3 });
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

// Or build the CII XML from your invoice object, then embed it into your own PDF.
var cii = await client.CreateCiiAsync(invoice);
var ownLayout = await client.EmbedZugferdAsync(File.ReadAllBytes("invoice.pdf"), cii.ToArray());
```

Set `InvoiceDocument.SpecificationId` to a value from `ZugferdProfiles` to create a profile other than EN 16931, for example `ZugferdProfiles.Extended`.

### Send an invoice by email

The email methods build the invoice exactly like their create and embed counterparts, then send it as a real email from your own address. They return the message id, not the file.

```csharp
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
Console.WriteLine($"{sent.Status}: {sent.MessageId}");

// An XRechnung works the same way.
await client.EmailXRechnungAsync(invoice, email, new XRechnungOptions { Syntax = XRechnungSyntax.Cii });

// Or send a PDF and a CII XML you already have, always with full validation.
await client.EmailEmbeddedZugferdAsync(File.ReadAllBytes("invoice.pdf"), File.ReadAllBytes("factur-x.xml"), email);
```

The sender must be on a domain you have added and verified on the Verified email domains page of your dashboard. A domain that is not verified yet throws `InvoiceXmlApiException` with `ErrorCode` `InvoiceXmlErrorCode.EmailDomainNotVerified`. `accepted` means the email was handed over for delivery, not that the recipient's server has received it yet. The client retries these calls only on HTTP 429, when nothing was sent. A timeout or `HttpRequestException` may leave the email sent, so check before you send it again. If you add your own retry handler to the `HttpClient`, leave these routes out: a second attempt could send a second email. See the [email delivery guide](https://www.invoicexml.com/docs/api/create/zugferd/email).

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

### Get a printable validation report

`ValidateZugferdReportAsync` and `ValidateXRechnungReportAsync` run the same validation and return a PDF report for your records, with the verdict next to it. An invalid invoice is not an exception: check `Valid`. It is null when the response carried no verdict.

```csharp
var report = await client.ValidateZugferdReportAsync(File.ReadAllBytes("supplier-invoice.pdf"), "supplier-invoice.pdf", PdfLanguage.De);
await report.File.SaveAsync("validation-report.pdf");

if (report.Valid == false)
    Console.WriteLine("The invoice failed validation, see the report.");
```

`validationLanguage` words the whole report. See the [validation report docs](https://www.invoicexml.com/docs/api/validate/zugferd/report).

### Render an invoice as a PDF

Turns any invoice XML into a readable PDF, for approval workflows and archives. XRechnung, CII and UBL are all accepted, and the format is detected from the XML.

```csharp
var pdf = await client.RenderXmlToPdfAsync(File.ReadAllBytes("rechnung.xml"), new RenderOptions
{
    Language = PdfLanguage.De,
    LogoUrl = "https://example.com/logo.png",
    IncludeQrCode = true,
});
```

See the [render docs](https://www.invoicexml.com/docs/api/render/xml/to/pdf).

### Convert between formats

Each convert method names its route: the source format, then the target. PDF sources must carry an embedded XML.

```csharp
// ZUGFeRD PDF in, UBL XML out.
var ubl = await client.ConvertZugferdToUblAsync(File.ReadAllBytes("rechnung.pdf"));

// XRechnung XML in, ZUGFeRD PDF with German labels out.
var zugferd = await client.ConvertXRechnungToZugferdAsync(File.ReadAllBytes("rechnung.xml"), new ConvertToPdfOptions { Language = PdfLanguage.De });
```

The result is validated before delivery. When it would not be compliant, the method throws `InvoiceXmlApiException` with the findings, worded in `ValidationLanguage` when you set it. See the [convert docs](https://www.invoicexml.com/docs/api/convert/zugferd/to/ubl).

### Transform a PDF invoice

`TransformToZugferdAsync` and `TransformToXRechnungAsync` take an ordinary PDF invoice. AI reads the PDF, so a person must check the result before it is used.

```csharp
var hybrid = await client.TransformToZugferdAsync(File.ReadAllBytes("rechnung.pdf"), new TransformOptions { Language = PdfLanguage.De });

// The Leitweg-ID is required for an XRechnung.
var xml = await client.TransformToXRechnungAsync(File.ReadAllBytes("rechnung.pdf"), "04011000-12345-03");
```

Leave `Language` empty to use the language the invoice is written in. The rendered PDF ends with a section of technical properties for that check; set `IncludeAdvancedProperties = false` for the classic layout alone, or `PreserveOriginalPdf = true` to keep the uploaded PDF as the visual layer. A 429 on these methods is not retried automatically, so wait for `Retry-After` before calling again if you want to retry. See the [transform docs](https://www.invoicexml.com/docs/api/transform/to/zugferd).

### Read invoices and attachments

```csharp
// ZUGFeRD PDF or XRechnung XML in, the same InvoiceDocument model out.
var received = await client.ExtractJsonAsync(File.ReadAllBytes("supplier-invoice.pdf"));
Console.WriteLine($"{received.InvoiceNumber} from {received.Seller?.Name}: {received.Totals?.DuePayableAmount} {received.Currency}");

// Embedded supporting documents (timesheets, delivery notes) as one ZIP.
var zip = await client.ExtractAttachmentsAsync(File.ReadAllBytes("rechnung.xml"));
await zip.SaveAsync("attachments.zip");
```

An extracted `InvoiceDocument` can be passed straight back to a create method, for example to turn an incoming XRechnung into a ZUGFeRD PDF.

### Read a PDF invoice with AI

`ParseJsonAsync` reads an ordinary PDF invoice, typed, scanned or photographed, into the same `InvoiceDocument` model. AI reads the PDF, so a person must check the data before it is used. The result says how sure the read was and which language the invoice is written in.

```csharp
var parsed = await client.ParseJsonAsync(File.ReadAllBytes("scanned-invoice.pdf"));
Console.WriteLine($"{parsed.Invoice.InvoiceNumber}: {parsed.Invoice.Totals?.DuePayableAmount} {parsed.Invoice.Currency}");
Console.WriteLine($"Confidence {parsed.Confidence?.Overall}, line items {parsed.Confidence?.Areas.LineItems}, language {parsed.Language}");
```

Any XML embedded in the PDF is ignored; use `ExtractJsonAsync` to read that. A 429 on this method is not retried automatically, so wait for `Retry-After` before calling again if you want to retry. See the [parse docs](https://www.invoicexml.com/docs/api/parse/json).

### ASP.NET Core and IHttpClientFactory

```csharp
builder.Services.AddHttpClient<IZugferdXRechnungClient, ZugferdXRechnungClient>((http, services) =>
    new ZugferdXRechnungClient(http, builder.Configuration["InvoiceXml:ApiKey"]!));
```

Inject `IZugferdXRechnungClient` where you need it, and fake it in your own unit tests. The client never disposes an `HttpClient` you pass in.

### Rate limits and retries

When more requests of your account run at the same time than your plan allows, the API answers HTTP 429 with a `Retry-After` header. A 429 is not billed. Every call except transform waits the requested interval and retries, up to three times by default:

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

Any other refused request throws `InvoiceXmlApiException` with the HTTP `StatusCode`, the API's numeric `ErrorCode` (see `InvoiceXmlErrorCode`), `Title`, `Detail` and a `TraceId` to quote when contacting support. The full list is in the [error handling docs](https://www.invoicexml.com/docs/error-handling).

## Examples

The [examples](https://github.com/InvoiceXML/invoicexml-dotnet-zugferd-xrechnung/tree/main/examples) folder holds small console apps that each show one common task from start to finish: creating a ZUGFeRD invoice with your own PDF, creating a CII XML and embedding it, emailing an invoice or your own PDF, validating, rendering, converting, transforming a PDF invoice, reading a PDF invoice with AI and reading e-invoices. See the [examples overview](https://github.com/InvoiceXML/invoicexml-dotnet-zugferd-xrechnung/blob/main/examples/README.md) for the list and how to run them.

## Endpoints

| Method | Endpoint | Docs |
|---|---|---|
| `CreateZugferdAsync` | `POST /v1/create/zugferd` | [reference](https://www.invoicexml.com/docs/api/create/zugferd) |
| `CreateXRechnungAsync` | `POST /v1/create/xrechnung` | [reference](https://www.invoicexml.com/docs/api/create/xrechnung) |
| `CreateCiiAsync` | `POST /v1/create/cii` | [reference](https://www.invoicexml.com/docs/api/create/cii) |
| `CreateUblAsync` | `POST /v1/create/ubl` | [reference](https://www.invoicexml.com/docs/api/create/ubl) |
| `EmailZugferdAsync` | `POST /v1/create/zugferd/email` | [reference](https://www.invoicexml.com/docs/api/create/zugferd/email) |
| `EmailXRechnungAsync` | `POST /v1/create/xrechnung/email` | [reference](https://www.invoicexml.com/docs/api/create/xrechnung/email) |
| `ValidateZugferdAsync` | `POST /v1/validate/zugferd` | [reference](https://www.invoicexml.com/docs/api/validate/zugferd) |
| `ValidateXRechnungAsync` | `POST /v1/validate/xrechnung` | [reference](https://www.invoicexml.com/docs/api/validate/xrechnung) |
| `ValidateZugferdReportAsync` | `POST /v1/validate/zugferd/report` | [reference](https://www.invoicexml.com/docs/api/validate/zugferd/report) |
| `ValidateXRechnungReportAsync` | `POST /v1/validate/xrechnung/report` | [reference](https://www.invoicexml.com/docs/api/validate/xrechnung/report) |
| `EmbedZugferdAsync` | `POST /v1/embed/zugferd` | [reference](https://www.invoicexml.com/docs/api/embed/zugferd) |
| `EmailEmbeddedZugferdAsync` | `POST /v1/embed/zugferd/email` | [reference](https://www.invoicexml.com/docs/api/embed/zugferd/email) |
| `RenderXmlToPdfAsync` | `POST /v1/render/xml/to/pdf` | [reference](https://www.invoicexml.com/docs/api/render/xml/to/pdf) |
| `ConvertZugferdToUblAsync` | `POST /v1/convert/zugferd/to/ubl` | [reference](https://www.invoicexml.com/docs/api/convert/zugferd/to/ubl) |
| `ConvertZugferdToCiiAsync` | `POST /v1/convert/zugferd/to/cii` | [reference](https://www.invoicexml.com/docs/api/convert/zugferd/to/cii) |
| `ConvertXRechnungToUblAsync` | `POST /v1/convert/xrechnung/to/ubl` | [reference](https://www.invoicexml.com/docs/api/convert/xrechnung/to/ubl) |
| `ConvertXRechnungToCiiAsync` | `POST /v1/convert/xrechnung/to/cii` | [reference](https://www.invoicexml.com/docs/api/convert/xrechnung/to/cii) |
| `ConvertUblToXRechnungAsync` | `POST /v1/convert/ubl/to/xrechnung` | [reference](https://www.invoicexml.com/docs/api/convert/ubl/to/xrechnung) |
| `ConvertCiiToXRechnungAsync` | `POST /v1/convert/cii/to/xrechnung` | [reference](https://www.invoicexml.com/docs/api/convert/cii/to/xrechnung) |
| `ConvertCiiToZugferdAsync` | `POST /v1/convert/cii/to/zugferd` | [reference](https://www.invoicexml.com/docs/api/convert/cii/to/zugferd) |
| `ConvertUblToZugferdAsync` | `POST /v1/convert/ubl/to/zugferd` | [reference](https://www.invoicexml.com/docs/api/convert/ubl/to/zugferd) |
| `ConvertXRechnungToZugferdAsync` | `POST /v1/convert/xrechnung/to/zugferd` | [reference](https://www.invoicexml.com/docs/api/convert/xrechnung/to/zugferd) |
| `ExtractJsonAsync` | `POST /v1/extract/json` | [reference](https://www.invoicexml.com/docs/api/extract/json) |
| `ExtractAttachmentsAsync` | `POST /v1/extract/attachments` | [reference](https://www.invoicexml.com/docs/api/extract/attachments) |
| `ParseJsonAsync` | `POST /v1/parse/json` | [reference](https://www.invoicexml.com/docs/api/parse/json) |
| `TransformToZugferdAsync` | `POST /v1/transform/to/zugferd` | [reference](https://www.invoicexml.com/docs/api/transform/to/zugferd) |
| `TransformToXRechnungAsync` | `POST /v1/transform/to/xrechnung` | [reference](https://www.invoicexml.com/docs/api/transform/to/xrechnung) |

The complete OpenAPI description is at [api.invoicexml.com/v1/openapi](https://api.invoicexml.com/v1/openapi), with an interactive explorer at [api.invoicexml.com/v1/scalar](https://api.invoicexml.com/v1/scalar/).

## Support

Questions, or a field you need for your integration? [Contact us](https://www.invoicexml.com/contact) and we will help you get it working.

## License

MIT. See [LICENSE](https://github.com/InvoiceXML/invoicexml-dotnet-zugferd-xrechnung/blob/main/LICENSE).
