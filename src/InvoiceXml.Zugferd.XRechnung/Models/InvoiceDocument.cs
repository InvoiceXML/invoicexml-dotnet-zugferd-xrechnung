using System;
using System.Collections.Generic;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// An invoice modelled on the European standard EN 16931. The same object creates a ZUGFeRD PDF
/// or an XRechnung XML, and is what <c>ExtractJsonAsync</c> returns when reading an invoice, so
/// an extracted invoice can be passed straight back to a create method.
/// </summary>
/// <remarks>
/// Only <see cref="InvoiceNumber"/>, <see cref="Currency"/>, <see cref="Seller"/>,
/// <see cref="Buyer"/> and <see cref="Lines"/> are required. Totals and VAT breakdowns are
/// calculated by the API when you leave them empty.
/// </remarks>
public sealed class InvoiceDocument
{
    /// <summary>Invoice number. Required.</summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>Issue date. Defaults to today when omitted.</summary>
    public DateTime? IssueDate { get; set; }

    /// <summary>Payment due date.</summary>
    public DateTime? DueDate { get; set; }

    /// <summary>Document type. Defaults to <see cref="InvoiceTypeCode.Invoice"/> when omitted.</summary>
    public InvoiceTypeCode? TypeCode { get; set; }

    /// <summary>Invoice currency. Required.</summary>
    public CurrencyCode? Currency { get; set; }

    /// <summary>Currency in which VAT is accounted, when it differs from <see cref="Currency"/>.</summary>
    public CurrencyCode? TaxCurrency { get; set; }

    /// <summary>
    /// Specification identifier the invoice declares. Leave empty for the default: EN 16931 for
    /// ZUGFeRD, the current XRechnung version for XRechnung. For ZUGFeRD, a value from
    /// <see cref="ZugferdProfiles"/> selects that profile instead.
    /// </summary>
    public string? SpecificationId { get; set; }

    /// <summary>Business process identifier, when the receiver requires one.</summary>
    public string? BusinessProcessType { get; set; }

    /// <summary>
    /// Reference assigned by the buyer. For XRechnung to German public authorities this carries
    /// the Leitweg-ID and is mandatory.
    /// </summary>
    public string? BuyerReference { get; set; }

    /// <summary>Purchase order number issued by the buyer.</summary>
    public string? PurchaseOrderReference { get; set; }

    /// <summary>Delivery note number of the shipment this invoice covers.</summary>
    public string? DespatchAdviceReference { get; set; }

    /// <summary>Free-text notes. A plain string converts to a note implicitly.</summary>
    public IList<InvoiceNote> Notes { get; set; } = new List<InvoiceNote>();

    /// <summary>UNTDID 2005 code stating when VAT becomes due, when no tax point date applies.</summary>
    public string? VatPointDateCode { get; set; }

    /// <summary>Supporting documents such as timesheets or delivery notes, referenced or embedded.</summary>
    public IList<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();

    /// <summary>Earlier invoices this document refers to, for example the invoice a credit note corrects.</summary>
    public IList<PrecedingInvoiceReference> PrecedingInvoiceReferences { get; set; } = new List<PrecedingInvoiceReference>();

    /// <summary>The party issuing the invoice. Required.</summary>
    public SellerParty? Seller { get; set; }

    /// <summary>The party receiving the invoice. Required.</summary>
    public BuyerParty? Buyer { get; set; }

    /// <summary>Where and when goods or services were delivered.</summary>
    public DeliveryInformation? Delivery { get; set; }

    /// <summary>Period the invoice covers.</summary>
    public Period? InvoicingPeriod { get; set; }

    /// <summary>How the invoice is paid.</summary>
    public PaymentDetails? PaymentDetails { get; set; }

    /// <summary>Document totals. Calculated by the API when omitted.</summary>
    public DocumentTotals? Totals { get; set; }

    /// <summary>VAT breakdown per category and rate. Calculated from the lines when empty.</summary>
    public IList<VatBreakdown> VatBreakdowns { get; set; } = new List<VatBreakdown>();

    /// <summary>Invoice lines. At least one is required.</summary>
    public IList<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();

    /// <summary>Allowances (discounts) on document level.</summary>
    public IList<DocumentAllowanceCharge> Allowances { get; set; } = new List<DocumentAllowanceCharge>();

    /// <summary>Charges (surcharges) on document level.</summary>
    public IList<DocumentAllowanceCharge> Charges { get; set; } = new List<DocumentAllowanceCharge>();
}
