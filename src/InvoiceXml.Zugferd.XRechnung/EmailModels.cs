using System.Collections.Generic;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>
/// The email that carries an invoice to its recipient. It is sent from your own address: the
/// sender must be on a domain added and verified on the Verified email domains page of your
/// dashboard.
/// </summary>
public sealed class EmailMessage
{
    /// <summary>Sender address, for example <c>invoices@yourcompany.com</c>. Required.</summary>
    public string? From { get; set; }

    /// <summary>Display name of the sender, for example <c>Your Company Billing</c>.</summary>
    public string? FromName { get; set; }

    /// <summary>Recipient address. Required.</summary>
    public string? To { get; set; }

#pragma warning disable CA2227 // Settable so the message reads naturally in an object initializer.
    /// <summary>
    /// Addresses that receive a visible copy. <see cref="Cc"/> and <see cref="Bcc"/> hold at most
    /// 2 addresses together.
    /// </summary>
    public IList<string> Cc { get; set; } = new List<string>();

    /// <summary>
    /// Addresses that receive a hidden copy. <see cref="Cc"/> and <see cref="Bcc"/> hold at most
    /// 2 addresses together.
    /// </summary>
    public IList<string> Bcc { get; set; } = new List<string>();
#pragma warning restore CA2227

    /// <summary>Address that receives the replies to the email.</summary>
    public string? ReplyTo { get; set; }

    /// <summary>Subject of the email. Left empty, a short English subject naming the invoice number is used.</summary>
    public string? Subject { get; set; }

    /// <summary>Plain-text message of the email. Left empty, a short English message naming the invoice number is used.</summary>
    public string? Message { get; set; }
}

/// <summary>The answer of the email methods when the email was accepted for delivery.</summary>
public sealed class EmailSendResult
{
    /// <summary>
    /// Always <c>accepted</c>: the email was handed over for delivery. It does not mean the
    /// recipient's server has received it yet.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>Identifier of the sent email. Keep it to ask about a delivery later.</summary>
    public string? MessageId { get; set; }
}
