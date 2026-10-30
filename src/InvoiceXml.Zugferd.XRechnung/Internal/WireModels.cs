using System;
using System.Collections.Generic;
using System.Linq;

namespace InvoiceXml.Zugferd.XRechnung.Internal;

/// <summary>The <c>{ "invoice": ..., "options": ... }</c> envelope of the create endpoints.</summary>
internal sealed class CreateInvoiceRequestBody
{
    public InvoiceDocument? Invoice { get; set; }

    public InvoiceOptionsBody? Options { get; set; }
}

/// <summary>The <c>{ "invoice": ..., "options": ..., "email": ... }</c> envelope of the create email endpoints.</summary>
internal sealed class CreateInvoiceEmailRequestBody
{
    public InvoiceDocument? Invoice { get; set; }

    public InvoiceOptionsBody? Options { get; set; }

    public EmailBody? Email { get; set; }
}

/// <summary>
/// The <c>email</c> object of the create email endpoints, also sent as the <c>email.*</c> form
/// fields of the embed email endpoints.
/// </summary>
internal sealed class EmailBody
{
    public string? From { get; set; }

    public string? FromName { get; set; }

    public string? To { get; set; }

    public string[]? Cc { get; set; }

    public string[]? Bcc { get; set; }

    public string? ReplyTo { get; set; }

    public string? Subject { get; set; }

    public string? Message { get; set; }

    public static EmailBody FromMessage(EmailMessage email) => new()
    {
        From = email.From,
        FromName = email.FromName,
        To = email.To,
        Cc = Addresses(email.Cc),
        Bcc = Addresses(email.Bcc),
        ReplyTo = email.ReplyTo,
        Subject = email.Subject,
        Message = email.Message,
    };

    // Each cc and bcc address travels as its own form field.
    public IEnumerable<FormPart> FormParts()
    {
        yield return FormPart.FromValue("email.from", From);
        yield return FormPart.FromValue("email.fromName", FromName);
        yield return FormPart.FromValue("email.to", To);
        foreach (var address in Cc ?? Array.Empty<string>())
            yield return FormPart.FromValue("email.cc", address);
        foreach (var address in Bcc ?? Array.Empty<string>())
            yield return FormPart.FromValue("email.bcc", address);
        yield return FormPart.FromValue("email.replyTo", ReplyTo);
        yield return FormPart.FromValue("email.subject", Subject);
        yield return FormPart.FromValue("email.message", Message);
    }

    private static string[]? Addresses(IList<string>? addresses)
    {
        var kept = addresses?
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .Select(address => address.Trim())
            .ToArray();
        return kept is { Length: > 0 } ? kept : null;
    }
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
