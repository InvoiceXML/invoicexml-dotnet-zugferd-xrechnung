using System;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>Totals of the invoice. All amounts are calculated by the API when omitted.</summary>
public sealed class DocumentTotals
{
    /// <summary>Sum of all line net amounts.</summary>
    public decimal? SumOfLineNetAmounts { get; set; }

    /// <summary>Sum of all document level allowances.</summary>
    public decimal? SumOfAllowances { get; set; }

    /// <summary>Sum of all document level charges.</summary>
    public decimal? SumOfCharges { get; set; }

    /// <summary>Total amount without VAT.</summary>
    public decimal? TaxBasisTotalAmount { get; set; }

    /// <summary>Total VAT amount.</summary>
    public decimal? TaxTotalAmount { get; set; }

    /// <summary>Total VAT amount in the tax currency, when <see cref="InvoiceDocument.TaxCurrency"/> is set.</summary>
    public decimal? TaxTotalAmountInAccountingCurrency { get; set; }

    /// <summary>Total amount including VAT.</summary>
    public decimal? GrandTotalAmount { get; set; }

    /// <summary>Amount already paid, for example a prepayment.</summary>
    public decimal? PaidAmount { get; set; }

    /// <summary>Rounding amount added to the total.</summary>
    public decimal? RoundingAmount { get; set; }

    /// <summary>Amount due for payment.</summary>
    public decimal? DuePayableAmount { get; set; }
}

/// <summary>VAT subtotal for one VAT category and rate.</summary>
public sealed class VatBreakdown
{
    /// <summary>Taxable amount of this category and rate.</summary>
    public decimal? TaxableAmount { get; set; }

    /// <summary>VAT amount of this category and rate.</summary>
    public decimal? TaxAmount { get; set; }

    /// <summary>VAT category.</summary>
    public VatCategoryCode? CategoryCode { get; set; }

    /// <summary>VAT rate in percent.</summary>
    public decimal? Rate { get; set; }

    /// <summary>Reason for a VAT exemption as text. Filled in by the API for exempt categories when omitted.</summary>
    public string? ExemptionReasonText { get; set; }

    /// <summary>Reason for a VAT exemption as a VATEX code.</summary>
    public string? ExemptionReasonCode { get; set; }
}

/// <summary>An allowance (discount) or charge (surcharge) on document level.</summary>
public sealed class DocumentAllowanceCharge
{
    /// <summary>Amount without VAT.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Base amount the percentage applies to.</summary>
    public decimal? BaseAmount { get; set; }

    /// <summary>Percentage applied to <see cref="BaseAmount"/>.</summary>
    public decimal? Percentage { get; set; }

    /// <summary>VAT category of the allowance or charge.</summary>
    public VatCategoryCode? VatCategoryCode { get; set; }

    /// <summary>VAT rate in percent.</summary>
    public decimal? VatRate { get; set; }

    /// <summary>Reason as free text.</summary>
    public string? Reason { get; set; }

    /// <summary>Reason as a code (UNTDID 5189 for allowances, UNTDID 7161 for charges).</summary>
    public string? ReasonCode { get; set; }
}

/// <summary>How the invoice is paid.</summary>
public sealed class PaymentDetails
{
    /// <summary>UNTDID 4461 payment means code, for example <see cref="PaymentMeansCodes.SepaCreditTransfer"/>.</summary>
    public string? PaymentMeansCode { get; set; }

    /// <summary>Payment means as text.</summary>
    public string? PaymentMeansText { get; set; }

    /// <summary>Remittance information the payer should quote, for example the invoice number.</summary>
    public string? RemittanceInformation { get; set; }

    /// <summary>Account to pay into, usually an IBAN.</summary>
    public string? PaymentAccountIdentifier { get; set; }

    /// <summary>Name of the account holder.</summary>
    public string? PaymentAccountName { get; set; }

    /// <summary>BIC of the payment service provider.</summary>
    public string? Bic { get; set; }

    /// <summary>SEPA direct debit mandate reference.</summary>
    public string? MandateReference { get; set; }

    /// <summary>Payment terms as text. Filled in by the API when an amount is due and none is given.</summary>
    public string? PaymentTerms { get; set; }
}

/// <summary>Where and when goods or services were delivered.</summary>
public sealed class DeliveryInformation
{
    /// <summary>Name of the party receiving the delivery.</summary>
    public string? ReceiverName { get; set; }

    /// <summary>Identifier of the delivery location, for example a GLN.</summary>
    public string? LocationIdentifier { get; set; }

    /// <summary>Scheme of <see cref="LocationIdentifier"/>.</summary>
    public string? LocationIdentifierSchemeId { get; set; }

    /// <summary>Date the delivery actually took place.</summary>
    public DateTime? ActualDeliveryDate { get; set; }

    /// <summary>Delivery address.</summary>
    public PostalAddress? DeliveryAddress { get; set; }
}

/// <summary>A date range.</summary>
public sealed class Period
{
    /// <summary>First day of the period.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Last day of the period.</summary>
    public DateTime? EndDate { get; set; }
}

/// <summary>Reference to an earlier invoice.</summary>
public sealed class PrecedingInvoiceReference
{
    /// <summary>Number of the earlier invoice.</summary>
    public string? Reference { get; set; }

    /// <summary>Issue date of the earlier invoice.</summary>
    public DateTime? IssueDate { get; set; }
}

/// <summary>
/// A supporting document such as a timesheet or delivery note, either referenced by URL or
/// embedded in the invoice as a binary attachment.
/// </summary>
public sealed class SupportingDocument
{
    /// <summary>Reference of the document. Required.</summary>
    public string? Reference { get; set; }

    /// <summary>Scheme of the reference, when it is an identifier.</summary>
    public string? SchemeId { get; set; }

    /// <summary>UNTDID 1001 document type code, for example <c>916</c> for a related document.</summary>
    public string? DocumentTypeCode { get; set; }

    /// <summary>Description of the document.</summary>
    public string? Description { get; set; }

    /// <summary>URL where the document can be retrieved.</summary>
    public string? ExternalUri { get; set; }

    /// <summary>The embedded file content. Sent and received as base64.</summary>
#pragma warning disable CA1819 // Byte content of an embedded file is naturally an array.
    public byte[]? Attachment { get; set; }
#pragma warning restore CA1819

    /// <summary>MIME type of <see cref="Attachment"/>, for example <c>application/pdf</c>.</summary>
    public string? AttachmentMimeCode { get; set; }

    /// <summary>File name of <see cref="Attachment"/>.</summary>
    public string? AttachmentFilename { get; set; }
}

/// <summary>
/// A free-text note on the invoice, optionally qualified by a UNTDID 4451 subject code.
/// A plain string converts to a note implicitly.
/// </summary>
public sealed class InvoiceNote
{
    /// <summary>Creates an empty note.</summary>
    public InvoiceNote()
    {
    }

    /// <summary>Creates a note with optional subject code.</summary>
    /// <param name="note">The note text.</param>
    /// <param name="subjectCode">UNTDID 4451 subject code, for example <c>AAB</c> or <c>REG</c>.</param>
    public InvoiceNote(string note, string? subjectCode = null)
    {
        Note = note;
        SubjectCode = subjectCode;
    }

    /// <summary>The note text.</summary>
    public string? Note { get; set; }

    /// <summary>UNTDID 4451 subject code of the note.</summary>
    public string? SubjectCode { get; set; }

    /// <summary>Creates a note without subject code from a string.</summary>
    /// <param name="note">The note text.</param>
#pragma warning disable CA2225 // The constructor is the named alternative.
    public static implicit operator InvoiceNote(string note) => new(note);
#pragma warning restore CA2225

    /// <inheritdoc />
    public override string ToString() => Note ?? string.Empty;
}
