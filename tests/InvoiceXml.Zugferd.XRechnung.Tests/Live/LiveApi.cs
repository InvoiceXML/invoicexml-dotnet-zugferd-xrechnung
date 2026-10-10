using Microsoft.Extensions.Configuration;

namespace InvoiceXml.Zugferd.XRechnung.Tests.Live;

/// <summary>
/// Access to the real API for the live tests. The key comes from user secrets
/// (<c>InvoiceXml:ApiKey</c>) or the <c>INVOICEXML_API_KEY</c> environment variable; without
/// one, every live test is skipped. Live calls consume credits on the key's account.
/// </summary>
internal static class LiveApi
{
    public static readonly string? ApiKey = ResolveApiKey();

    /// <summary>Files produced by the live run, for manual inspection (git-ignored).</summary>
    public static readonly string OutputDirectory = Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "artifacts", "live");

    private static string? ResolveApiKey()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("INVOICEXML_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment;

        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(LiveApi).Assembly, optional: true)
            .Build();
        var fromSecrets = configuration["InvoiceXml:ApiKey"];
        return string.IsNullOrWhiteSpace(fromSecrets) ? null : fromSecrets;
    }

    public static async Task SaveAsync(InvoiceFile file, string name)
    {
        Directory.CreateDirectory(OutputDirectory);
        await file.SaveAsync(Path.Combine(OutputDirectory, name));
    }
}

/// <summary>A fact that only runs when an API key is configured.</summary>
public sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute()
    {
        if (LiveApi.ApiKey is null)
            Skip = "No InvoiceXML API key configured (user secret InvoiceXml:ApiKey or INVOICEXML_API_KEY).";
    }
}

/// <summary>Invoices taken from the README, so the documented examples are checked against the real API.</summary>
internal static class LiveInvoices
{
    public static InvoiceDocument QuickStart() => new()
    {
        InvoiceNumber = "RE-2026-001",
        Currency = CurrencyCode.EUR,
        Seller = new SellerParty { Name = "Muster GmbH", VatIdentifier = "DE123456789", PostalAddress = new PostalAddress { City = "Berlin", PostCode = "10115", Country = CountryCode.DE } },
        Buyer = new BuyerParty { Name = "Kunde AG", PostalAddress = new PostalAddress { City = "Hamburg", PostCode = "20095", Country = CountryCode.DE } },
        Lines = { new InvoiceLine { Quantity = 10, UnitCode = UnitCodes.Hour, Item = new ItemInformation { Name = "Consulting" }, PriceDetails = new PriceDetails { NetPrice = 95.50m }, VatInformation = new LineVatInformation { CategoryCode = VatCategoryCode.S, Rate = 19 } } },
    };

    public static InvoiceDocument XRechnung() => new()
    {
        InvoiceNumber = "RE-2026-4408",
        Currency = CurrencyCode.EUR,
        DueDate = new DateTime(2026, 8, 27),
        BuyerReference = "04011000-12345-03",
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
}
