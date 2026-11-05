namespace XisfSharp.IO;

/// <summary>
/// A data block written as an attachment after the XISF header.
/// </summary>
internal interface IOutputAttachment
{
    /// <summary>
    /// Gets or sets the placeholder that stands for the block position in the header until the layout is known.
    /// </summary>
    string AttachmentPosition { get; set; }

    /// <summary>
    /// Gets the largest number of bytes the block can occupy once written.
    /// </summary>
    long MaxBlockSize { get; }

    Task WriteDataAsync(Stream stream, CancellationToken cancellationToken);
}
