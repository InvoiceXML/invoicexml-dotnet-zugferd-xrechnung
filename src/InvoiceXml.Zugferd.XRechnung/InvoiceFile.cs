using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace InvoiceXml.Zugferd.XRechnung;

/// <summary>A file returned by the API: a PDF, an XML document or a ZIP archive.</summary>
public sealed class InvoiceFile
{
    private readonly byte[] _content;

    /// <summary>Creates a file. Useful when faking <see cref="IZugferdXRechnungClient"/> in tests.</summary>
    /// <param name="content">The file content.</param>
    /// <param name="contentType">The MIME type, for example <c>application/pdf</c>.</param>
    /// <param name="fileName">The suggested file name, if any.</param>
    public InvoiceFile(byte[] content, string contentType, string? fileName = null)
    {
        _content = content ?? throw new ArgumentNullException(nameof(content));
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        FileName = fileName;
    }

    /// <summary>The file name suggested by the API, for example <c>invoice-zugferd-RE-2026-001.pdf</c>.</summary>
    public string? FileName { get; }

    /// <summary>The MIME type: <c>application/pdf</c>, <c>application/xml</c> or <c>application/zip</c>.</summary>
    public string ContentType { get; }

    /// <summary>Size of the file in bytes.</summary>
    public int Length => _content.Length;

    /// <summary>Returns the file content.</summary>
    /// <returns>The raw bytes of the file.</returns>
    public byte[] ToArray() => _content;

    /// <summary>Opens a read-only stream over the file content.</summary>
    /// <returns>A new <see cref="MemoryStream"/> positioned at the start.</returns>
    public Stream OpenRead() => new MemoryStream(_content, writable: false);

    /// <summary>Writes the file to disk, replacing any existing file.</summary>
    /// <param name="path">Target path.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes when the file is written.</returns>
    public async Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        if (path is null) throw new ArgumentNullException(nameof(path));
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await stream.WriteAsync(_content, 0, _content.Length, cancellationToken).ConfigureAwait(false);
    }
}
