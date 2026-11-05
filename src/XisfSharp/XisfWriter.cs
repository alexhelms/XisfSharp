using System.Buffers.Binary;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using XisfSharp.FITS;
using XisfSharp.IO;
using XisfSharp.Properties;

namespace XisfSharp;

public record XisfWriterOptions
{
    /// <summary>
    /// Get or set the data encoding used for embedded or inlined data.
    /// Default is <see cref="DataEncoding.Base64"/>.
    /// </summary>
    public DataEncoding DataEncoding { get; set; } = DataEncoding.Base64;

    /// <summary>
    /// Get or set the compression codec.
    /// Default is <see cref="CompressionCodec.None"/>.
    /// </summary>
    public CompressionCodec CompressionCodec { get; set; } = CompressionCodec.None;

    /// <summary>
    /// Get or set shuffling bytes.
    /// This option does nothing is <see cref="CompressionCodec"/> is <see cref="CompressionCodec.None"/>.
    /// When enabled, data is shuffled before compression to improve compression ratio.
    /// </summary>
    public bool ShuffleBytes { get; set; }

    /// <summary>
    /// Gets or sets the size in bytes of the uncompressed subblocks that compressed blocks are divided into,
    /// or null to divide only blocks too large for the codec.
    /// </summary>
    /// <remarks>
    /// Subblocks are compressed in parallel, so smaller subblocks speed up compression at a small cost in ratio.
    /// Divided blocks are written with a <c>subblocks</c> attribute, which the reader must support.
    /// This option does nothing if <see cref="CompressionCodec"/> is <see cref="CompressionCodec.None"/>.
    /// </remarks>
    public int? SubblockSize
    {
        get => _subblockSize;
        set
        {
            if (value is { } size)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(size, OutputBlock.MaxSubblockSize);
            }

            _subblockSize = value;
        }
    }

    private int? _subblockSize;

    /// <summary>
    /// Gets or sets the directory for temporary files, or null to use the system temporary directory.
    /// </summary>
    /// <remarks>
    /// Temporary files are used when an image streamed from a non-seekable stream is written with
    /// <see cref="ShuffleBytes"/>, because shuffling reads the image data once per byte of a sample, and when
    /// streamed images are saved to a non-seekable output, because the header is completed after the data.
    /// </remarks>
    public string? TempDirectory { get; set; }

    /// <summary>
    /// Get or set the checksum algorithm used.
    /// Default is <see cref="ChecksumAlgorithm.None"/>.
    /// </summary>
    public ChecksumAlgorithm ChecksumAlgorithm { get; set; } = ChecksumAlgorithm.None;

    /// <summary>
    /// Gets or sets the inclusive lower bound for valid floating-point values, as required by the XISF spec.
    /// </summary>
    public double FloatingPointLowerBound { get; set; } = 0;

    /// <summary>
    /// Gets or sets the inclusive upper bound for valid floating-point values, as required by the XISF spec.
    /// </summary>
    public double FloatingPointUpperBound { get; set; } = 1;
}

/// <summary>
/// Provides functionality to write XISF files.
/// </summary>
public sealed class XisfWriter : IDisposable, IAsyncDisposable
{
    private const int AlignedBlockSize = 4096;
    private const int MaxInlineBlockSize = 3072;

    private readonly XNamespace _ns = "http://www.pixinsight.com/xisf";
    private readonly Stream _stream;
    private readonly XisfWriterOptions _options;
    private readonly bool _leaveOpen;

    private readonly List<XisfImage> _images = [];
    private readonly List<IOutputAttachment> _blocks = [];
    private readonly Dictionary<XisfImage, ImageDataSource> _imageSources = new(ReferenceEqualityComparer.Instance);
    private readonly XisfPropertyCollection _properties = [];

    private bool _disposed;
    private byte[] _header = [];

    internal string XmlText { get; private set; } = string.Empty;

    /// <summary>
    /// Create a new <see cref="XisfWriter"/> that writes to the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the XISF file to write.</param>
    public XisfWriter(string filePath)
        : this(File.Create(filePath))
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfWriter"/> that writes to the specified file path with custom options.
    /// </summary>
    /// <param name="filePath">The path to the XISF file to write.</param>
    /// <param name="options">The options to use when writing the XISF file.</param>
    public XisfWriter(string filePath, XisfWriterOptions options)
        : this(File.Create(filePath), options)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfWriter"/> that writes to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write XISF data to. Must be writable.</param>
    /// <param name="leaveOpen">If true, the stream is left open after the writer is disposed; otherwise, the stream is disposed.</param>
    public XisfWriter(Stream stream, bool leaveOpen = false)
        : this(stream, new XisfWriterOptions(), leaveOpen)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfWriter"/> that writes to the specified stream with custom options.
    /// </summary>
    /// <param name="stream">The stream to write XISF data to. Must be writable.</param>
    /// <param name="options">The options to use when writing the XISF file.</param>
    /// <param name="leaveOpen">If true, the stream is left open after the writer is disposed; otherwise, the stream is disposed.</param>
    public XisfWriter(Stream stream, XisfWriterOptions options, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanWrite)
            throw new ArgumentException("Stream must be writable.", nameof(stream));

        _stream = stream;
        _options = options;
        _leaveOpen = leaveOpen;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (!_leaveOpen)
        {
            _stream.Dispose();
        }

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (!_leaveOpen)
        {
            await _stream.DisposeAsync();
        }

        _disposed = true;
    }

    /// <summary>
    /// Asynchronously writes a single image to the specified file path.
    /// </summary>
    /// <param name="path">The path where the XISF file will be saved.</param>
    /// <param name="image">The image to write.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static Task WriteAsync(string path, XisfImage image, CancellationToken cancellationToken = default)
    {
        return WriteAsync(path, [image], new XisfWriterOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes multiple images to the specified file path.
    /// </summary>
    /// <param name="path">The path where the XISF file will be saved.</param>
    /// <param name="images">The collection of images to write.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static Task WriteAsync(string path, IEnumerable<XisfImage> images, CancellationToken cancellationToken = default)
    {
        return WriteAsync(path, images, new XisfWriterOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously writes multiple images to the specified file path with custom options.
    /// </summary>
    /// <param name="path">The path where the XISF file will be saved.</param>
    /// <param name="images">The collection of images to write.</param>
    /// <param name="options">The options to use when writing the XISF file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    public static async Task WriteAsync(string path, IEnumerable<XisfImage> images, XisfWriterOptions options, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(options);

        await using var writer = new XisfWriter(path, options);
        writer.AddImages(images);
        await writer.SaveAsync(cancellationToken);
    }

    /// <summary>
    /// Adds an image to be written to the XISF file.
    /// </summary>
    /// <param name="image">The image to add.</param>
    public void AddImage(XisfImage image)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        _images.Add(image);
    }

    /// <summary>
    /// Adds an image whose pixel data is supplied row by row while the file is saved, without holding it in memory.
    /// </summary>
    /// <remarks>
    /// The image supplies the geometry, pixel storage and metadata; its <see cref="XisfImage.Data"/> is ignored and
    /// its <see cref="XisfImage.SampleFormat"/> is set from <typeparamref name="T"/>. Rows are requested in storage
    /// order, and once per byte of a sample when <see cref="XisfWriterOptions.ShuffleBytes"/> is enabled with compression.
    /// </remarks>
    /// <typeparam name="T">The sample type of the image.</typeparam>
    /// <param name="image">The image to add. Create it with the image dimensions and no data.</param>
    /// <param name="source">Supplies each row of the image.</param>
    public void AddImage<T>(XisfImage image, XisfRowSource<T> source)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(source);
        AddStreamedImage<T>(image, img => new RowImageDataSource<T>(source, img));
    }

    /// <summary>
    /// Adds an image whose pixel data is read from a stream while the file is saved, without holding it in memory.
    /// </summary>
    /// <remarks>
    /// The stream must supply <see cref="XisfImage.DataSize"/> bytes from its current position, in storage order and
    /// native byte order. The image supplies the geometry, pixel storage and metadata; its <see cref="XisfImage.Data"/>
    /// is ignored and its <see cref="XisfImage.SampleFormat"/> is set from <typeparamref name="T"/>. When
    /// <see cref="XisfWriterOptions.ShuffleBytes"/> is enabled with compression, a non-seekable stream is first copied
    /// to a temporary file. The stream is not disposed by the writer.
    /// </remarks>
    /// <typeparam name="T">The sample type of the image.</typeparam>
    /// <param name="image">The image to add. Create it with the image dimensions and no data.</param>
    /// <param name="source">The stream supplying the pixel data.</param>
    public void AddImage<T>(XisfImage image, Stream source)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(source);

        if (!source.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(source));

        AddStreamedImage<T>(image, img =>
        {
            if (source.CanSeek && source.Length - source.Position < img.DataSize)
                throw new ArgumentException($"Stream has fewer than the {img.DataSize} bytes the image requires.", nameof(source));

            return new StreamImageDataSource(source, img.DataSize, ownsStream: false);
        });
    }

    private void AddStreamedImage<T>(XisfImage image, Func<XisfImage, ImageDataSource> createSource)
        where T : unmanaged
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(image);

        if (image.Width < 1 || image.Height < 1 || image.Channels < 1)
            throw new ArgumentException("Image must have a width, height and channel count of at least 1.", nameof(image));

        image.SampleFormat = XisfUtil.ToSampleFormat<T>();
        _imageSources[image] = createSource(image);
        _images.Add(image);
    }

    /// <summary>
    /// Adds multiple images to be written to the XISF file.
    /// </summary>
    /// <param name="images">The collection of images to add.</param>
    public void AddImages(params IEnumerable<XisfImage> images)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _images.AddRange(images);
    }

    /// <summary>
    /// Adds a global property to the XISF file metadata.
    /// </summary>
    /// <param name="property">The property to add.</param>
    public void AddProperty(XisfProperty property)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _properties.Add(property);
    }

    /// <summary>
    /// Adds multiple global properties to the XISF file metadata.
    /// </summary>
    /// <param name="properties">The collection of properties to add.</param>
    public void AddProperties(params IEnumerable<XisfProperty> properties)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        foreach (var property in properties)
        {
            AddProperty(property);
        }
    }

    /// <summary>
    /// Adds an image to be written to the XISF file and returns the writer for method chaining.
    /// </summary>
    /// <param name="image">The image to add.</param>
    /// <returns>This <see cref="XisfWriter"/> instance for fluent syntax.</returns>
    public XisfWriter WithImage(XisfImage image)
    {
        AddImage(image);
        return this;
    }

    /// <summary>
    /// Adds the image and associated properties to the writer and returns the writer for method chaining.
    /// </summary>
    /// <param name="image">The image to add to the writer. Cannot be null.</param>
    /// <param name="imageProperties">A collection of properties to associate with the image. Properties with duplicate identifiers are ignored.</param>
    /// <returns>This <see cref="XisfWriter"/> instance for fluent syntax.</returns>
    public XisfWriter WithImage(XisfImage image, params IEnumerable<XisfProperty> imageProperties)
    {
        foreach (var property in imageProperties)
        {
            if (image.Properties.ContainsId(property.Id))
                continue;

            image.Properties.Add(property);
        }

        AddImage(image);
        return this;
    }

    /// <summary>
    /// Adds multiple images to be written to the XISF file and returns the writer for method chaining.
    /// </summary>
    /// <param name="images">The collection of images to add.</param>
    /// <returns>This <see cref="XisfWriter"/> instance for fluent syntax.</returns>
    public XisfWriter WithImages(params IEnumerable<XisfImage> images)
    {
        AddImages(images);
        return this;
    }

    /// <summary>
    /// Adds a global property to the XISF file metadata and returns the writer for method chaining.
    /// </summary>
    /// <param name="property">The property to add.</param>
    /// <returns>This <see cref="XisfWriter"/> instance for fluent syntax.</returns>
    public XisfWriter WithProperty(XisfProperty property)
    {
        AddProperty(property);
        return this;
    }

    /// <summary>
    /// Adds multiple global properties to the XISF file metadata and returns the writer for method chaining.
    /// </summary>
    /// <param name="properties">The collection of properties to add.</param>
    /// <returns>This <see cref="XisfWriter"/> instance for fluent syntax.</returns>
    public XisfWriter WithProperties(params IEnumerable<XisfProperty> properties)
    {
        AddProperties(properties);
        return this;
    }

    /// <summary>
    /// Asynchronously writes all added images and properties to the XISF file.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_imageSources.Count > 0)
        {
            await SaveWithStreamedImagesAsync(cancellationToken);
            return;
        }

        await WriteHeader(cancellationToken);
        await WriteImageBlocks(cancellationToken);
    }

    /// <summary>
    /// Writes the file when some image data is streamed and its encoded size is unknown until it has been written.
    /// </summary>
    /// <remarks>
    /// Header space is reserved for the largest values the unknown attributes can take, the blocks are written after it,
    /// and the header is then rewritten with the real values. The unused part of the reserved space is left as zeros,
    /// which XISF allows between the header and the first attachment.
    /// </remarks>
    private async Task SaveWithStreamedImagesAsync(CancellationToken cancellationToken)
    {
        if (_stream.CanSeek)
        {
            await WriteWithReservedHeaderAsync(_stream, cancellationToken);
            return;
        }

        // The header is rewritten after the data, so a non-seekable output receives a copy of a complete temporary file.
        string path = Path.Combine(_options.TempDirectory ?? Path.GetTempPath(), $"xisfsharp-{Guid.NewGuid():N}.tmp");
        await using var tempFile = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 1024 * 1024,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        await WriteWithReservedHeaderAsync(tempFile, cancellationToken);

        tempFile.Position = 0;
        await tempFile.CopyToAsync(_stream, cancellationToken);
    }

    private async Task WriteWithReservedHeaderAsync(Stream output, CancellationToken cancellationToken)
    {
        string xml = await BuildHeaderXmlAsync(cancellationToken);
        var streamedBlocks = _blocks.OfType<StreamedOutputBlock>().ToList();

        string worstCaseXml = xml;
        foreach (var block in streamedBlocks)
        {
            worstCaseXml = worstCaseXml
                .Replace(block.SizeToken, block.MaxBlockSize.ToString(CultureInfo.InvariantCulture))
                .Replace(block.SubblocksToken, block.WorstCaseSubblocksAttribute)
                .Replace(block.ChecksumToken, block.WorstCaseChecksumAttribute);
        }

        var (reservedSize, _, _) = LayoutHeader(worstCaseXml, index => _blocks[index].MaxBlockSize, minimumHeaderSize: 0);

        long headerStart = output.Position;
        await WriteZerosAsync(output, reservedSize, cancellationToken);

        var blockSizes = new long[_blocks.Count];
        for (int i = 0; i < _blocks.Count; i++)
        {
            await WriteZerosAsync(output, AlignedPosition(output.Position) - output.Position, cancellationToken);

            long start = output.Position;
            await _blocks[i].WriteDataAsync(output, cancellationToken);
            blockSizes[i] = output.Position - start;
        }

        long end = output.Position;

        foreach (var block in streamedBlocks)
        {
            xml = xml
                .Replace(block.SizeToken, block.StoredSize.ToString(CultureInfo.InvariantCulture))
                .Replace(block.SubblocksToken, block.SubblocksAttribute)
                .Replace(block.ChecksumToken, block.ChecksumAttribute);
        }

        // Real values never need more digits than the worst case, so the layout keeps the reserved header size.
        var (headerSize, finalXml, _) = LayoutHeader(xml, index => blockSizes[index], minimumHeaderSize: reservedSize);
        if (headerSize != reservedSize)
            throw new XisfException("Header does not fit in the space reserved for it.");

        output.Position = headerStart;
        await output.WriteAsync(CreateHeader(finalXml, headerSize: 16 + System.Text.Encoding.UTF8.GetByteCount(finalXml)), cancellationToken);
        output.Position = end;

        XmlText = finalXml;
    }

    /// <summary>
    /// Places the attachments after an aligned header and substitutes their positions into the header.
    /// </summary>
    /// <returns>The aligned header size and the header with positions filled in.</returns>
    private (long HeaderSize, string Xml, long[] Positions) LayoutHeader(string xml, Func<int, long> blockSize, long minimumHeaderSize)
    {
        long headerSize = AlignedPosition(Math.Max(16 + System.Text.Encoding.UTF8.GetByteCount(xml), minimumHeaderSize));

        // Positions grow with the header, so the header size only increases and settles within a few iterations.
        for (int iteration = 0; iteration < 10; iteration++)
        {
            var positions = new long[_blocks.Count];
            long offset = headerSize;
            string positioned = xml;

            for (int i = 0; i < _blocks.Count; i++)
            {
                positions[i] = offset;
                positioned = positioned.Replace(_blocks[i].AttachmentPosition, $"attachment:{offset}");
                offset = AlignedPosition(offset + blockSize(i));
            }

            long required = AlignedPosition(Math.Max(16 + System.Text.Encoding.UTF8.GetByteCount(positioned), minimumHeaderSize));
            if (required == headerSize)
                return (headerSize, positioned, positions);

            headerSize = required;
        }

        throw new XisfException("Failed to stabilize header size after maximum iterations.");
    }

    private static byte[] CreateHeader(string xml, long headerSize)
    {
        byte[] xmlBytes = System.Text.Encoding.UTF8.GetBytes(xml);
        byte[] header = new byte[headerSize];
        "XISF0100"u8.CopyTo(header);
        BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan(8, 4), (uint)xmlBytes.Length);
        xmlBytes.CopyTo(header, 16);
        return header;
    }

    private static async Task WriteZerosAsync(Stream output, long count, CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> zeros = new byte[(int)Math.Min(Math.Max(count, 0), 1024 * 1024)];

        while (count > 0)
        {
            int chunk = (int)Math.Min(count, zeros.Length);
            await output.WriteAsync(zeros[..chunk], cancellationToken);
            count -= chunk;
        }
    }

    private static long AlignedPosition(long unalignedPosition)
    {
        return (unalignedPosition + AlignedBlockSize - 1) / AlignedBlockSize * AlignedBlockSize;
    }

    private async Task<string> BuildHeaderXmlAsync(CancellationToken cancellationToken)
    {
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

        var root = new XElement(_ns + "xisf",
            new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
            new XAttribute(xsi + "schemaLocation", "http://www.pixinsight.com/xisf http://pixinsight.com/xisf/xisf-1.0.xsd"),
            new XAttribute("version", "1.0")
        );

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XComment("\nExtensible Image Serialization Format - XISF version 1.0\nCreated with XisfSharp\n"),
            root
        );

        AppendMetadata(root);

        foreach (var image in _images)
        {
            AppendImage(root, image);
        }

        using var stringWriter = new StringWriter();
        var xmlSettings = new XmlWriterSettings
        {
            Async = true,
            Encoding = System.Text.Encoding.UTF8,
            OmitXmlDeclaration = false,
            Indent = false,
            NewLineChars = "\n",
            NewLineHandling = NewLineHandling.Entitize,
        };

        await using (var xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
        {
            await doc.WriteToAsync(xmlWriter, cancellationToken);
        }

        await stringWriter.FlushAsync(cancellationToken);
        return stringWriter.ToString();
    }

    private async Task WriteHeader(CancellationToken cancellationToken)
    {
        byte[] signature = "XISF0100\0\0\0\0\0\0\0\0"u8.ToArray();

        long prevAlignedHeaderSize = 0;
        int maxIterations = 10;
        int iteration = 0;

        string xml = await BuildHeaderXmlAsync(cancellationToken);

        while (iteration++ < maxIterations)
        {
            long currentHeaderSize = signature.Length + System.Text.Encoding.UTF8.GetByteCount(xml);
            long alignedHeaderSize = AlignedPosition(currentHeaderSize);

            // Check if stabilized
            if (alignedHeaderSize == prevAlignedHeaderSize)
            {
                // Finalize the header
                byte[] header = new byte[alignedHeaderSize];
                byte[] xmlBytes = System.Text.Encoding.UTF8.GetBytes(xml);
                signature.CopyTo(header);
                BinaryPrimitives.WriteUInt32LittleEndian(header.AsSpan()[8..12], (uint)xmlBytes.Length);
                xmlBytes.CopyTo(header, signature.Length);
                _header = header;
                XmlText = xml;
                break;
            }

            // Update attachment positions based on new padded size
            prevAlignedHeaderSize = alignedHeaderSize;
            long attachmentOffset = alignedHeaderSize;

            foreach (var block in _blocks)
            {
                string attachmentPosition = $"attachment:{attachmentOffset}";
                xml = xml.Replace(block.AttachmentPosition, attachmentPosition);
                attachmentOffset = AlignedPosition(attachmentOffset + block.MaxBlockSize);
                block.AttachmentPosition = attachmentPosition;
            }
        }

        if (iteration >= maxIterations)
        {
            throw new XisfException("Failed to stabilize header size after maximum iterations.");
        }

        await _stream.WriteAsync(_header, cancellationToken);
    }

    private async Task WriteImageBlocks(CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> zero = new byte[AlignedBlockSize];

        foreach (var block in _blocks)
        {
            long currentPosition = _stream.Position;
            long attachmentPosition = long.Parse(block.AttachmentPosition.Split(':')[1]);
            int padding = (int)(attachmentPosition - currentPosition);
            if (padding > 0)
            {
                byte[] paddingBytes = new byte[padding];
                await _stream.WriteAsync(zero[..padding], cancellationToken);
            }

            await block.WriteDataAsync(_stream, cancellationToken);
        }
    }

    private void AppendMetadata(XElement root)
    {
        var metadataElement = new XElement(_ns + "Metadata");

        var creationTimeElement = SerializeProperty(
            new XisfTimePointProperty("XISF:CreationTime", DateTimeOffset.UtcNow));
        var creatorElement = SerializeProperty(
            new XisfStringProperty("XISF:CreatorModule", $"XisfSharp {GitVersionInformation.MajorMinorPatch}"));

        metadataElement.Add(creationTimeElement);
        metadataElement.Add(creatorElement);

        foreach (var property in _properties)
        {
            var propertyElement = SerializeProperty(property);
            metadataElement.Add(propertyElement);
        }

        root.Add(metadataElement);
    }

    private void AppendImage(XElement root, XisfImage image)
    {
        var imageElement = new XElement(_ns + "Image",
            new XAttribute("geometry", $"{image.Width}:{image.Height}:{image.Channels}"),
            new XAttribute("sampleFormat", image.SampleFormat.ToString()),
            new XAttribute("colorSpace", image.ColorSpace.ToString()),
            new XAttribute("imageType", image.ImageType.ToString()),
            new XAttribute("pixelStorage", image.PixelStorage.ToString())
        );

        AppendBounds(imageElement, image);
        AppendColorFilterArray(imageElement, image);
        AppendRGBWorkingSpace(imageElement, image);
        AppendDisplayFunction(imageElement, image);
        AppendResolution(imageElement, image);
        AppendOrientation(imageElement, image);
        AppendId(imageElement, image);
        AppendUuid(imageElement, image);
        AppendOffset(imageElement, image);

        foreach (var property in image.Properties)
        {
            var propertyElement = SerializeProperty(property);
            imageElement.Add(propertyElement);
        }

        foreach (var keyword in image.FITSKeywords)
        {
            var keywordElement = SerializeFITSKeyword(keyword);
            imageElement.Add(keywordElement);
        }

        if (_imageSources.TryGetValue(image, out var source))
        {
            NewStreamedBlock(imageElement, source, XisfUtil.SampleFormatSize(image.SampleFormat));
        }
        else
        {
            NewBlock(imageElement, image.Data, XisfUtil.SampleFormatSize(image.SampleFormat), canInline: false);
        }

        root.Add(imageElement);

        void AppendBounds(XElement imageElement, XisfImage image)
        {
            var sampleFormat = image.SampleFormat;
            var bounds = image.Bounds;

            if (bounds is null)
            {
                if (sampleFormat is SampleFormat.Float32 or SampleFormat.Float64)
                {
                    var lower = _options.FloatingPointLowerBound;
                    var upper = _options.FloatingPointUpperBound;
                    if (lower > upper)
                    {
                        lower = _options.FloatingPointUpperBound;
                        upper = _options.FloatingPointLowerBound;
                    }
                    bounds = new SampleBounds(lower, upper);
                }
            }

            if (bounds is not null)
            {
                imageElement.Add(new XAttribute("bounds", $"{bounds.Value.Lower}:{bounds.Value.Upper}"));
            }
        }

        void AppendColorFilterArray(XElement imageElement, XisfImage image)
        {
            if (image.ColorFilterArray is null)
                return;

            var cfa = image.ColorFilterArray;
            var pattern = string.Concat(cfa.Pattern.Select(p => p.ToString()));

            var cfaElement = new XElement(_ns + "ColorFilterArray",
                new XAttribute("pattern", pattern),
                new XAttribute("width", cfa.Width),
                new XAttribute("height", cfa.Height)
            );

            if (cfa.Name is not null)
                cfaElement.Add(new XAttribute("name", cfa.Name));

            imageElement.Add(cfaElement);
        }

        void AppendRGBWorkingSpace(XElement imageElement, XisfImage image)
        {
            if (image.RGBWorkingSpace is null)
                return;

            var rgbws = image.RGBWorkingSpace;
            var chromaticityX = string.Join(':', rgbws.ChromaticityX.Select(x => x.ToString("F6")));
            var chromaticityY = string.Join(':', rgbws.ChromaticityY.Select(x => x.ToString("F6")));
            var luminance = string.Join(':', rgbws.Luminance.Select(x => x.ToString("F6")));

            var rgbwsElement = new XElement(_ns + "RGBWorkingSpace",
                new XAttribute("x", chromaticityX),
                new XAttribute("y", chromaticityY),
                new XAttribute("Y", luminance),
                new XAttribute("gamma", rgbws.Gamma)
            );

            if (rgbws.Name is not null)
                rgbwsElement.Add(new XAttribute("name", rgbws.Name));

            imageElement.Add(rgbwsElement);
        }

        void AppendDisplayFunction(XElement imageElement, XisfImage image)
        {
            if (image.DisplayFunction is null)
                return;

            var df = image.DisplayFunction;
            var m = string.Join(':', df.Midtones.Select(x => x.ToString("F6")));
            var s = string.Join(':', df.Shadows.Select(x => x.ToString("F6")));
            var h = string.Join(':', df.Highlights.Select(x => x.ToString("F6")));
            var l = string.Join(':', df.ShadowDynamicRange.Select(x => x.ToString("F6")));
            var r = string.Join(':', df.HighlightDynamicRange.Select(x => x.ToString("F6")));

            var dfElement = new XElement(_ns + "DisplayFunction",
                new XAttribute("m", m),
                new XAttribute("s", s),
                new XAttribute("h", h),
                new XAttribute("l", l),
                new XAttribute("r", r)
            );

            if (df.Name is not null)
                dfElement.Add(new XAttribute("name", df.Name));

            imageElement.Add(dfElement);
        }

        void AppendResolution(XElement imageElement, XisfImage image)
        {
            if (image.Resolution is null)
                return;

            var resolutionElement = new XElement(_ns + "Resolution",
                new XAttribute("horizontal", image.Resolution.Horizontal.ToString()),
                new XAttribute("vertical", image.Resolution.Vertical.ToString())
            );

            if (image.Resolution.Unit is not null)
                resolutionElement.Add(new XAttribute("unit", image.Resolution.Unit.ToString()!.ToLower()));

            imageElement.Add(resolutionElement);
        }

        static void AppendOrientation(XElement imageElement, XisfImage image)
        {
            if (image.Orientation is null)
                return;

            var orientation = image.Orientation.Value switch
            {
                ImageOrientation.Default => "0",
                ImageOrientation.Flip => "flip",
                ImageOrientation.Rotate90 => "90",
                ImageOrientation.Rotate90Flip => "90;flip",
                ImageOrientation.RotateMinus90 => "-90",
                ImageOrientation.RotateMinus90Flip => "-90;flip",
                ImageOrientation.Rotate180 => "180",
                ImageOrientation.Rotate180Flip => "180;flip",
                _ => "0",
            };

            var orientationAttr = new XAttribute("orientation", orientation);

            imageElement.Add(orientationAttr);
        }

        static void AppendId(XElement imageElement, XisfImage image)
        {
            if (image.Id is null)
                return;

            var idAttr = new XAttribute("id", image.Id);

            imageElement.Add(idAttr);
        }

        static void AppendUuid(XElement imageElement, XisfImage image)
        {
            if (image.Uuid is null)
                return;

            var uuidAttr = new XAttribute("uuid", image.Uuid.Value.ToString("D"));

            imageElement.Add(uuidAttr);
        }

        static void AppendOffset(XElement imageElement, XisfImage image)
        {
            if (image.Offset is null)
                return;

            var offsetAttr = new XAttribute("offset", image.Offset.Value);

            imageElement.Add(offsetAttr);
        }
    }

    /// <summary>
    /// Adds the attributes of a streamed image block, with placeholders for the values known only once it is written.
    /// </summary>
    private void NewStreamedBlock(XElement element, ImageDataSource source, int itemSize)
    {
        var block = new StreamedOutputBlock(
            source,
            _options.CompressionCodec,
            itemSize,
            _options.ShuffleBytes,
            _options.SubblockSize ?? StreamedOutputBlock.DefaultSubblockSize,
            _options.ChecksumAlgorithm,
            _options.TempDirectory);

        if (block.IsCompressed)
        {
            // Streamed blocks always declare their subblocks, which lets a lone subblock that did not shrink be stored verbatim.
            element.Add(new XAttribute("compression", block.CompressionAttribute));
            element.Add(new XAttribute("subblocks", block.SubblocksToken));
        }

        if (block.HasChecksum)
        {
            element.Add(new XAttribute("checksum", block.ChecksumToken));
        }

        element.Add(new XAttribute("location", $"{block.AttachmentPosition}:{block.SizeToken}"));
        _blocks.Add(block);
    }

    internal void NewBlock(XElement element, ReadOnlyMemory<byte> blockData, int itemSize = 1, bool canInline = true)
    {
        OutputBlock block = new(blockData);

        if (_options.CompressionCodec != CompressionCodec.None)
        {
            // Check for minimum size to benefit from compression
            var uncompressedSize = blockData.Length;
            if (uncompressedSize >= 64)
            {
                block.CompressData(_options.CompressionCodec, itemSize, _options.ShuffleBytes, _options.SubblockSize ?? OutputBlock.MaxSubblockSize);
            }
        }

        block.ComputeChecksum(_options.ChecksumAlgorithm);

        var blockSize = block.BlockSize;
        if (blockSize <= MaxInlineBlockSize)
        {
            var dataEncoding = _options.DataEncoding;
            if (dataEncoding == DataEncoding.None)
                dataEncoding = DataEncoding.Base64;

            if (canInline)
            {
                WriteCompressionAttribute(element, block);
                WriteChecksumAttribute(element, block);
                element.Add(new XAttribute("location", $"inline:{dataEncoding.ToString().ToLowerInvariant()}"));
                element.Add(block.GetEncodedData(dataEncoding));
            }
            else
            {
                element.Add(new XAttribute("location", "embedded"));
                var dataElement = new XElement(_ns + "Data");
                WriteCompressionAttribute(dataElement, block);
                WriteChecksumAttribute(dataElement, block);
                dataElement.Add(new XAttribute("encoding", dataEncoding.ToString().ToLowerInvariant()));
                dataElement.Add(block.GetEncodedData(dataEncoding));
                element.Add(dataElement);
            }
        }
        else
        {
            WriteCompressionAttribute(element, block);
            WriteChecksumAttribute(element, block);
            element.Add(new XAttribute("location", $"{block.AttachmentPosition}:{blockSize}"));
            _blocks.Add(block);
        }

        static void WriteCompressionAttribute(XElement element, OutputBlock block)
        {
            var compressionAttr = block.GetCompressionAttribute();
            if (!string.IsNullOrEmpty(compressionAttr))
            {
                element.Add(new XAttribute("compression", compressionAttr));
            }

            var subblocksAttr = block.GetSubblocksAttribute();
            if (!string.IsNullOrEmpty(subblocksAttr))
            {
                element.Add(new XAttribute("subblocks", subblocksAttr));
            }
        }

        static void WriteChecksumAttribute(XElement element, OutputBlock block)
        {
            var checksumAttr = block.GetChecksumAttribute();
            if (!string.IsNullOrEmpty(checksumAttr))
            {
                element.Add(new XAttribute("checksum", checksumAttr));
            }
        }
    }

    private XElement SerializeFITSKeyword(FITSKeyword keyword)
    {
        var element = new XElement(_ns + "FITSKeyword",
            new XAttribute("name", keyword.Name),
            new XAttribute("value", keyword.Value ?? string.Empty)
        );

        if (!string.IsNullOrEmpty(keyword.Comment))
            element.Add(new XAttribute("comment", keyword.Comment));

        return element;
    }

    private XElement SerializeProperty(XisfProperty property)
    {
        return property switch
        {
            XisfScalarProperty scalar => SerializeScalar(scalar),
            XisfStringProperty @string => SerializeString(@string),
            XisfTimePointProperty timePoint => SerializeTimePoint(timePoint),
            XisfVectorProperty vector => SerializeVector(vector),
            XisfMatrixProperty matrix => SerializeMatrix(matrix),
            _ => throw new XisfException($"Unsupported property type: {property.GetType().Name}"),
        };
    }

    private XElement CreateBaseXElement(XisfProperty property)
    {
        var element = new XElement(_ns + "Property",
            new XAttribute("id", property.Id),
            new XAttribute("type", property.Type.ToString())
        );

        if (property.Format is not null)
            element.Add(new XAttribute("format", property.Format));

        if (property.Comment is not null)
            element.Add(new XAttribute("comment", property.Comment));

        return element;
    }

    private static string FormatScalarValue(object value, XisfPropertyType type)
    {
        return type switch
        {
            XisfPropertyType.Boolean => ((bool)value) ? "1" : "0",
            XisfPropertyType.Float32 or XisfPropertyType.Float64 => value?.ToString() ?? "0",
            _ => value?.ToString() ?? string.Empty
        };
    }

    private XElement SerializeScalar(XisfScalarProperty property)
    {
        var element = CreateBaseXElement(property);
        element.Add(new XAttribute("value", FormatScalarValue(property.Value, property.Type)));
        return element;
    }

    private XElement SerializeString(XisfStringProperty property)
    {
        var element = CreateBaseXElement(property);

        int valueSize = System.Text.Encoding.UTF8.GetByteCount(property.StringValue);
        if (_options.CompressionCodec == CompressionCodec.None || valueSize < 80)
        {
            element.Add(property.Value);
        }
        else
        {
            var valueBytes = System.Text.Encoding.UTF8.GetBytes(property.StringValue);
            NewBlock(element, valueBytes);
        }

        return element;
    }

    private XElement SerializeTimePoint(XisfTimePointProperty property)
    {
        var element = CreateBaseXElement(property);
        element.Add(new XAttribute("value", property.TimePointValue.ToString("O", CultureInfo.InvariantCulture)));
        return element;
    }

    private XElement SerializeVector(XisfVectorProperty property)
    {
        var element = CreateBaseXElement(property);
        element.Add(new XAttribute("length", property.Length));
        NewBlock(element, property.GetBytes(), property.GetBytesPerElement());
        return element;
    }

    private XElement SerializeMatrix(XisfMatrixProperty property)
    {
        var element = CreateBaseXElement(property);
        element.Add(new XAttribute("rows", property.Rows));
        element.Add(new XAttribute("columns", property.Columns));
        NewBlock(element, property.GetBytes(), property.GetBytesPerElement());
        return element;
    }
}
