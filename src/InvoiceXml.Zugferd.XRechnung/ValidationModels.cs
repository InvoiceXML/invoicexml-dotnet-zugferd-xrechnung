using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// The verdict of a validation. A completed validation always returns this result: check
/// <see cref="Valid"/>, and read <see cref="Errors"/> when it is false.
/// </summary>
public sealed class InvoiceValidationResult
{
    private IReadOnlyList<ValidationFinding> _errors = Array.Empty<ValidationFinding>();
    private IReadOnlyList<ValidationFinding> _warnings = Array.Empty<ValidationFinding>();

    /// <summary>True when the invoice passed every validation layer.</summary>
    public bool Valid { get; set; }

    /// <summary>One-sentence summary of the outcome.</summary>
    public string? Detail { get; set; }

    /// <summary>Details about what was validated: layers passed, detected profile, declared identifier.</summary>
    public ValidationDetails? Data { get; set; }

    /// <summary>Errors that make the invoice non-compliant. Empty when <see cref="Valid"/> is true.</summary>
    public IReadOnlyList<ValidationFinding> Errors
    {
        get => _errors;
        set => _errors = value ?? Array.Empty<ValidationFinding>();
    }

    /// <summary>Advisory findings. They never make an invoice invalid, but are worth reviewing.</summary>
    public IReadOnlyList<ValidationFinding> Warnings
    {
        get => _warnings;
        set => _warnings = value ?? Array.Empty<ValidationFinding>();
    }

    /// <summary>
    /// Field report: one row per invoice field with its current value and the findings that
    /// concern it. Null when the document could not be read far enough to build one.
    /// </summary>
    public IReadOnlyList<FieldReportRow>? Report { get; set; }
}

/// <summary>Details of a validation run.</summary>
public sealed class ValidationDetails
{
    /// <summary>True when the XML matched its XML schema.</summary>
    public bool? SchemaValid { get; set; }

    /// <summary>True when the XML passed the business rules (EN 16931 and the profile rules).</summary>
    public bool? SchematronValid { get; set; }

    /// <summary>Conformance level of the endpoint, for example <c>EN16931</c> or <c>XRechnung</c>.</summary>
    public string? ConformanceLevel { get; set; }

    /// <summary>
    /// The profile whose rules were applied, as a slug such as <c>en16931</c>, <c>basic</c>,
    /// <c>extended</c> or <c>xrechnung</c>.
    /// </summary>
    public string? Profile { get; set; }

    /// <summary>The specification identifier exactly as the document declares it.</summary>
    public string? CustomizationId { get; set; }

    /// <summary>For ZUGFeRD PDFs: whether an embedded invoice XML was found.</summary>
    public bool? HasEmbeddedXml { get; set; }

    /// <summary>Any further fields the API returns.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>
/// One validation error or warning. Also returned in <see cref="InvoiceXmlApiException.Findings"/>
/// when a create or embed request is refused because the invoice would not be compliant.
/// </summary>
public sealed class ValidationFinding
{
    private IReadOnlyList<string> _fields = Array.Empty<string>();

    /// <summary>
    /// Identifier of the rule, for example <c>BR-CO-15</c> or <c>BR-DE-15</c>. Null for request
    /// format errors, which are not rule violations.
    /// </summary>
    public string? Rule { get; set; }

    /// <summary>
    /// Validation layer that produced the finding: <c>xsd</c> (structure), <c>en16931</c>
    /// (European core rules) or <c>cius</c> (profile rules such as the German XRechnung rules).
    /// See <see cref="ValidationLayers"/>.
    /// </summary>
    public string? Layer { get; set; }

    /// <summary>1-based invoice line number, or null for document level findings.</summary>
    public int? Line { get; set; }

    /// <summary>Plain-language explanation of the problem.</summary>
    public string? Message { get; set; }

    /// <summary>
    /// JSON paths into <see cref="InvoiceDocument"/> to correct, for example
    /// <c>lines[0].priceDetails.netPrice</c>.
    /// </summary>
    public IReadOnlyList<string> Fields
    {
        get => _fields;
        set => _fields = value ?? Array.Empty<string>();
    }

    /// <summary>The validator's original output, for technical diagnostics.</summary>
    public string? Raw { get; set; }

    /// <summary>Any further fields the API returns.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        Rule is null ? Message ?? string.Empty : $"[{Rule}] {Message}";
}

/// <summary>Values of <see cref="ValidationFinding.Layer"/>.</summary>
public static class ValidationLayers
{
    /// <summary>XML schema (structure).</summary>
    public const string Xsd = "xsd";

    /// <summary>EN 16931 core business rules.</summary>
    public const string En16931 = "en16931";

    /// <summary>Profile rules on top of EN 16931, such as the German XRechnung rules.</summary>
    public const string Cius = "cius";
}

/// <summary>One row of the field report: an invoice field, its value and its findings.</summary>
public sealed class FieldReportRow
{
    private IReadOnlyList<FieldReportFinding> _errors = Array.Empty<FieldReportFinding>();
    private IReadOnlyList<FieldReportFinding> _warnings = Array.Empty<FieldReportFinding>();

    /// <summary>Human-readable field name.</summary>
    public string? Name { get; set; }

    /// <summary>Logical group: <c>header</c>, <c>seller</c>, <c>buyer</c>, <c>lines</c>, <c>payment</c>, <c>totals</c> or <c>tax</c>.</summary>
    public string? Section { get; set; }

    /// <summary>JSON path into <see cref="InvoiceDocument"/>, for example <c>seller.vatIdentifier</c>.</summary>
    public string? Path { get; set; }

    /// <summary>Current value for display, shortened when long. Null when the field is absent.</summary>
    public string? Value { get; set; }

    /// <summary>True when no error concerns this field.</summary>
    public bool IsValid { get; set; }

    /// <summary>Errors that concern this field.</summary>
    public IReadOnlyList<FieldReportFinding> Errors
    {
        get => _errors;
        set => _errors = value ?? Array.Empty<FieldReportFinding>();
    }

    /// <summary>Warnings that concern this field.</summary>
    public IReadOnlyList<FieldReportFinding> Warnings
    {
        get => _warnings;
        set => _warnings = value ?? Array.Empty<FieldReportFinding>();
    }

    /// <summary>Any further fields the API returns.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalData { get; set; }
}

/// <summary>A finding attached to a field report row.</summary>
public sealed class FieldReportFinding
{
    /// <summary>Identifier of the rule.</summary>
    public string? Rule { get; set; }

    /// <summary>Plain-language explanation of the problem.</summary>
    public string? Message { get; set; }
}
