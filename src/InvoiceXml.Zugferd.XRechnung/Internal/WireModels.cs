using System.Collections.Generic;

namespace InvoiceXml.Zugferd.XRechnung.Internal;

/// <summary>The <c>{ "invoice": ..., "options": ... }</c> envelope of the create endpoints.</summary>
internal sealed class CreateInvoiceRequestBody
{
    public InvoiceDocument? Invoice { get; set; }

    public InvoiceOptionsBody? Options { get; set; }
}

/// <summary>The <c>options</c> object of the create endpoints.</summary>
internal sealed class InvoiceOptionsBody
{
    public XRechnungSyntax? Syntax { get; set; }

    public PdfLanguage? Language { get; set; }

    public string? BrandColor { get; set; }

    public string? PdfUrl { get; set; }

    public string? LogoUrl { get; set; }

    public bool? IncludeAdvancedProperties { get; set; }

    public static InvoiceOptionsBody? From(ZugferdOptions? options) => options is null
        ? null
        : new InvoiceOptionsBody
        {
            Language = options.Language,
            BrandColor = options.BrandColor,
            PdfUrl = options.PdfUrl,
            LogoUrl = options.LogoUrl,
            IncludeAdvancedProperties = options.IncludeAdvancedProperties,
        };

    public static InvoiceOptionsBody? From(XRechnungOptions? options) => options is null
        ? null
        : new InvoiceOptionsBody
        {
            Syntax = options.Syntax,
            Language = options.Language,
        };
}

/// <summary>The <c>{ "invoice": ... }</c> envelope returned by <c>/v1/extract/json</c>.</summary>
internal sealed class InvoiceJsonResponseBody
{
    public InvoiceDocument? Invoice { get; set; }
}

/// <summary>RFC 7807 problem details with the API's extensions.</summary>
internal sealed class ProblemBody
{
    public string? Title { get; set; }

    public string? Detail { get; set; }

    public int? Status { get; set; }

    public int? ErrorCode { get; set; }

    public string? TraceId { get; set; }

    public List<ValidationFinding>? Errors { get; set; }
}
