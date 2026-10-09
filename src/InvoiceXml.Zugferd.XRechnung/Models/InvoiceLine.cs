using System.Collections.Generic;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>One invoice line.</summary>
public sealed class InvoiceLine
{
    /// <summary>Line identifier. Numbered automatically when omitted.</summary>
    public string? LineId { get; set; }

    /// <summary>Free-text note on the line.</summary>
    public string? LineNote { get; set; }

    /// <summary>Identifier of an object the line relates to, for example a meter or subscription.</summary>
    public SchemeIdentifier? ObjectIdentifier { get; set; }

    /// <summary>Invoiced quantity. Required.</summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// UN/ECE Recommendation 20 unit of the quantity, for example <see cref="UnitCodes.Hour"/>.
    /// Defaults to one piece when omitted.
    /// </summary>
    public string? UnitCode { get; set; }

    /// <summary>Net amount of the line. Calculated from quantity, price, allowances and charges when omitted.</summary>
    public decimal? LineNetAmount { get; set; }

    /// <summary>Line number of the buyer's purchase order this line refers to.</summary>
    public string? BuyerOrderLineReference { get; set; }

    /// <summary>The buyer's accounting reference (for example a cost centre) for this line.</summary>
    public string? LineBuyerAccountingReference { get; set; }

    /// <summary>Period the line covers.</summary>
    public Period? LinePeriod { get; set; }

    /// <summary>Price of the item. Required.</summary>
    public PriceDetails? PriceDetails { get; set; }

    /// <summary>VAT category and rate of the line. Required.</summary>
    public LineVatInformation? VatInformation { get; set; }

    /// <summary>The invoiced item. Required.</summary>
    public ItemInformation? Item { get; set; }

    /// <summary>Allowances (discounts) on this line.</summary>
    public IList<LineAllowanceCharge> Allowances { get; set; } = new List<LineAllowanceCharge>();

    /// <summary>Charges (surcharges) on this line.</summary>
    public IList<LineAllowanceCharge> Charges { get; set; } = new List<LineAllowanceCharge>();
}

/// <summary>Price of an invoice line item.</summary>
public sealed class PriceDetails
{
    /// <summary>Net unit price after any price discount. Required.</summary>
    public decimal? NetPrice { get; set; }

    /// <summary>Discount per unit, the difference between gross and net price.</summary>
    public decimal? DiscountAmount { get; set; }

    /// <summary>Unit price before the discount.</summary>
    public decimal? GrossPrice { get; set; }

    /// <summary>Number of units the price applies to. Defaults to 1.</summary>
    public decimal? PriceBaseQuantity { get; set; }

    /// <summary>Unit of <see cref="PriceBaseQuantity"/>.</summary>
    public string? PriceBaseUnit { get; set; }
}

/// <summary>VAT category and rate of an invoice line.</summary>
public sealed class LineVatInformation
{
    /// <summary>VAT category, for example <see cref="VatCategoryCode.S"/> for the standard rate.</summary>
    public VatCategoryCode? CategoryCode { get; set; }

    /// <summary>VAT rate in percent, for example <c>19</c>.</summary>
    public decimal? Rate { get; set; }
}

/// <summary>The item invoiced on a line.</summary>
public sealed class ItemInformation
{
    /// <summary>Item name. Required.</summary>
    public string? Name { get; set; }

    /// <summary>Item description.</summary>
    public string? Description { get; set; }

    /// <summary>The seller's article number.</summary>
    public string? SellerIdentifier { get; set; }

    /// <summary>The buyer's article number.</summary>
    public string? BuyerIdentifier { get; set; }

    /// <summary>Standard identifier such as a GTIN, with its scheme.</summary>
    public SchemeIdentifier? StandardIdentifier { get; set; }

    /// <summary>Classifications of the item, for example a CPV or UNSPSC code.</summary>
    public IList<ItemClassification> Classifications { get; set; } = new List<ItemClassification>();

    /// <summary>Item attributes such as colour or size.</summary>
    public IList<ItemAttribute> Attributes { get; set; } = new List<ItemAttribute>();

    /// <summary>Country of origin.</summary>
    public CountryCode? CountryOfOrigin { get; set; }
}

/// <summary>A classification code of an item.</summary>
public sealed class ItemClassification
{
    /// <summary>The classification code.</summary>
    public string? Identifier { get; set; }

    /// <summary>UNTDID 7143 code of the classification scheme, for example <c>STI</c> for CPV.</summary>
    public string? SchemeId { get; set; }

    /// <summary>Version of the classification scheme.</summary>
    public string? SchemeVersion { get; set; }
}

/// <summary>A named attribute of an item.</summary>
#pragma warning disable CA1711 // Same name as the API schema; it is an item attribute, not a .NET attribute.
public sealed class ItemAttribute
#pragma warning restore CA1711
{
    /// <summary>Creates an empty attribute.</summary>
    public ItemAttribute()
    {
    }

    /// <summary>Creates an attribute with a name and value.</summary>
    /// <param name="name">Attribute name, for example <c>Colour</c>.</param>
    /// <param name="value">Attribute value, for example <c>Blue</c>.</param>
    public ItemAttribute(string name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>Attribute name.</summary>
    public string? Name { get; set; }

    /// <summary>Attribute value.</summary>
    public string? Value { get; set; }
}

/// <summary>An allowance (discount) or charge (surcharge) on an invoice line.</summary>
public sealed class LineAllowanceCharge
{
    /// <summary>Amount without VAT.</summary>
    public decimal? Amount { get; set; }

    /// <summary>Base amount the percentage applies to.</summary>
    public decimal? BaseAmount { get; set; }

    /// <summary>Percentage applied to <see cref="BaseAmount"/>.</summary>
    public decimal? Percentage { get; set; }

    /// <summary>Reason as free text.</summary>
    public string? Reason { get; set; }

    /// <summary>Reason as a code (UNTDID 5189 for allowances, UNTDID 7161 for charges).</summary>
    public string? ReasonCode { get; set; }
}
