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
    private readonly List<OutputBlock> _blocks = [];
    private readonly XisfPropertyCollection _properties = [];

    private bool _disposed;
    private byte[] _header = [];

    internal string XmlText { get; private set; } = string.Empty;

    /// <summary>
    /// Create a new <see cref="XisfWriter"/> that writes to the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the XISF file to write.</param>
    public XisfWriter(string filePath)
        : this(File.OpenWrite(filePath))
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfWriter"/> that writes to the specified file path with custom options.
    /// </summary>
    /// <param name="filePath">The path to the XISF file to write.</param>
    /// <param name="options">The options to use when writing the XISF file.</param>
    public XisfWriter(string filePath, XisfWriterOptions options)
        : this(File.OpenWrite(filePath), options)
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

        await WriteHeader(cancellationToken);
        await WriteImageBlocks(cancellationToken);
    }

    private async Task WriteHeader(CancellationToken cancellationToken)
    {
        byte[] signature = "XISF0100\0\0\0\0\0\0\0\0"u8.ToArray();

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

        long prevAlignedHeaderSize = 0;
        int maxIterations = 10;
        int iteration = 0;

        string xml;
        using (var stringWriter = new StringWriter())
        {
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

            await stringWriter.FlushAsync();
            xml = stringWriter.ToString();
        }

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
                attachmentOffset = AlignedPosition(attachmentOffset + block.BlockSize);
                block.AttachmentPosition = attachmentPosition;
            }
        }

        if (iteration >= maxIterations)
        {
            throw new XisfException("Failed to stabilize header size after maximum iterations.");
        }

        await _stream.WriteAsync(_header, cancellationToken);

        static long AlignedPosition(long unalignedPosition)
        {
            long alignedPosition = AlignedBlockSize * (unalignedPosition/ AlignedBlockSize);
            if (alignedPosition < unalignedPosition)
            {
                alignedPosition += AlignedBlockSize;
            }
            return alignedPosition;
        }
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
            new XisfStringProperty("XISF:CreatorModule", $"XisfSharp {GitVersionInformation.InformationalVersion}"));

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

        NewBlock(imageElement, image.Data, XisfUtil.SampleFormatSize(image.SampleFormat), canInline: false);

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

    internal void NewBlock(XElement element, ReadOnlyMemory<byte> blockData, int itemSize = 1, bool canInline = true)
    {
        OutputBlock block = new(blockData);

        if (_options.CompressionCodec != CompressionCodec.None)
        {
            // Check for minimum size to benefit from compression
            var uncompressedSize = blockData.Length;
            if (uncompressedSize < 64)
                return;

            block.CompressData(_options.CompressionCodec, itemSize, _options.ShuffleBytes);
        }

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
