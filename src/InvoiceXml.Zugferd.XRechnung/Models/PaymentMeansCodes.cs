namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// Common UNTDID 4461 payment means codes for <see cref="PaymentDetails.PaymentMeansCode"/>.
/// </summary>
public static class PaymentMeansCodes
{
    /// <summary>Instrument not defined (1).</summary>
    public const string NotDefined = "1";

    /// <summary>In cash (10).</summary>
    public const string InCash = "10";

    /// <summary>Cheque (20).</summary>
    public const string Cheque = "20";

    /// <summary>Credit transfer (30).</summary>
    public const string CreditTransfer = "30";

    /// <summary>Bank account transfer, IBAN (42).</summary>
    public const string BankAccountTransfer = "42";

    /// <summary>Bank card (48).</summary>
    public const string BankCard = "48";

    /// <summary>Direct debit (49).</summary>
    public const string DirectDebit = "49";

    /// <summary>Standing agreement (57).</summary>
    public const string StandingAgreement = "57";

    /// <summary>SEPA credit transfer (58).</summary>
    public const string SepaCreditTransfer = "58";

    /// <summary>SEPA direct debit (59).</summary>
    public const string SepaDirectDebit = "59";
}
