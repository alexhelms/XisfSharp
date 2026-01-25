using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Xml;
using System.Xml.Linq;
using XisfSharp.FITS;
using XisfSharp.IO;
using XisfSharp.Properties;

namespace XisfSharp;

/// <summary>
/// Provides functionality to read and parse XISF files.
/// </summary>
public sealed class XisfReader : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;

    private XNamespace _ns = XNamespace.None;
    private bool _disposed;
    private bool _headerRead;
    private long _minBlockPosition;
    private List<XisfImage> _images = [];
    private XisfPropertyCollection _properties = [];

    /// <summary>
    /// Gets the read-only list of images in the XISF file.
    /// </summary>
    public IReadOnlyList<XisfImage> Images => _images;

    /// <summary>
    /// Gets the read-only collection of global properties in the XISF file.
    /// </summary>
    public ReadOnlyXisfPropertyCollection Properties => new(_properties);

    /// <summary>
    /// Gets or sets a value indicating whether the parser should throw exceptions on parsing failures.
    /// </summary>
    /// <remarks>If this property is set to <see langword="true"/>, the parser will throw an exception when it
    /// encounters invalid input. If set to <see langword="false"/>, the parser will handle failed parses without
    /// throwing and may return a default value.</remarks>
    public bool ThrowOnParsingFailure { get; set; }

    /// <summary>
    /// Gets or sets a log handler to invoke when a warning or error occurs.
    /// </summary>
    public Action<string, Exception?>? LogHandler { get; set; }

    internal string XmlText { get; private set; } = string.Empty;

    /// <summary>
    /// Create a new <see cref="XisfReader"/> from the specified file path.
    /// </summary>
    /// <param name="filePath">The path to the XISF file to read.</param>
    public XisfReader(string filePath)
        : this(File.OpenRead(filePath))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XisfReader"/> class from a stream.
    /// </summary>
    /// <param name="stream">The stream containing XISF data. Must be readable and seekable.</param>
    /// <param name="leaveOpen">If true, the stream is left open after the reader is disposed; otherwise, the stream is disposed.</param>
    public XisfReader(Stream stream, bool leaveOpen = false)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!stream.CanRead)
            throw new ArgumentException("Stream must be readable.", nameof(stream));

        if (!stream.CanSeek)
            throw new ArgumentException("Stream must support seeking.", nameof(stream));

        _stream = stream;
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

    private void LogError(string message, Exception e)
    {
        LogHandler?.Invoke($"{message} : {e.Message}", e);
    }

    private void Log(string message)
    {
        LogHandler?.Invoke(message, null);
    }

    /// <summary>
    /// Asynchronously reads the first image from the specified XISF file.
    /// </summary>
    /// <param name="path">The path to the XISF file to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the first <see cref="XisfImage"/> from the file.</returns>
    public static Task<XisfImage> ReadAsync(string path, CancellationToken cancellationToken = default)
    {
        return ReadAsync(File.OpenRead(path), false, cancellationToken);
    }

    /// <summary>
    /// Asynchronously reads the first image from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing XISF data. Must be readable and seekable.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the first <see cref="XisfImage"/> from the stream.</returns>
    public static Task<XisfImage> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return ReadAsync(stream, false, cancellationToken);
    }

    /// <summary>
    /// Asynchronously reads the first image from the specified stream with an option to leave the stream open.
    /// </summary>
    /// <param name="stream">The stream containing XISF data. Must be readable and seekable.</param>
    /// <param name="leaveOpen">If true, the stream is left open after reading; otherwise, the stream is disposed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the first <see cref="XisfImage"/> from the stream.</returns>
    public static async Task<XisfImage> ReadAsync(Stream stream, bool leaveOpen, CancellationToken cancellationToken = default)
    {
        await using var reader = new XisfReader(stream, leaveOpen);
        await reader.ReadHeaderAsync(cancellationToken);
        return await reader.ReadImageAsync(0, cancellationToken);
    }

    /// <summary>
    /// Asynchronously reads and parses the XISF file header.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation.</returns>
    public async Task ReadHeaderAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_headerRead)
            return;

        _minBlockPosition = 0;
        _images = [];
        _properties = [];

        await ReadSignature(cancellationToken);
        await ReadXisfHeader(cancellationToken);

        _headerRead = true;
    }

    /// <summary>
    /// Asynchronously reads the first image from the XISF file.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the first <see cref="XisfImage"/>.</returns>
    public Task<XisfImage> ReadImageAsync(CancellationToken cancellationToken = default)
    {
        return ReadImageAsync(0, cancellationToken);
    }

    /// <summary>
    /// Asynchronously reads the image at the specified index from the XISF file.
    /// </summary>
    /// <param name="index">The zero-based index of the image to read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the <see cref="XisfImage"/> at the specified index.</returns>
    public async Task<XisfImage> ReadImageAsync(int index, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        await ReadHeaderAsync(cancellationToken);

        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _images.Count);

        var image = _images[index];
        await image.DataBlock.LoadData(_stream, cancellationToken);
        return image;
    }

    /// <summary>
    /// Asynchronously reads all images from the XISF file.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains a list of all <see cref="XisfImage"/> instances.</returns>
    public async Task<List<XisfImage>> ReadAllImagesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await ReadHeaderAsync(cancellationToken);

        List<XisfImage> images = [];
        for (int i = 0; i < _images.Count; i++)
        {
            var image = await ReadImageAsync(i, cancellationToken);
            images.Add(image);
        }

        return images;
    }

    /// <summary>
    /// Asynchronously enumerates all images in the XISF file.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An async enumerable sequence of <see cref="XisfImage"/> instances.</returns>
    public async IAsyncEnumerable<XisfImage> ReadImagesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        
        await ReadHeaderAsync(cancellationToken);
        
        for (int i = 0; i < _images.Count; i++)
        {
            var image = await ReadImageAsync(i, cancellationToken);
            yield return image;
        }
    }

    /// <summary>
    /// Asynchronously reads the thumbnail associated with the specified image.
    /// </summary>
    /// <param name="image">The image whose thumbnail should be read.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous read operation. The task result contains the thumbnail <see cref="XisfImage"/>, or null if no thumbnail exists.</returns>
    public async Task<XisfImage?> ReadThumbnailAsync(XisfImage image, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await ReadHeaderAsync(cancellationToken);

        if (image.Thumbnail is null)
            return null;

        await image.Thumbnail.DataBlock.LoadData(_stream, cancellationToken);
        return image.Thumbnail;
    }

    private async Task ReadSignature(CancellationToken cancellationToken)
    {
        _stream.Seek(0, SeekOrigin.Begin);

        var signature = new byte[8];
        await _stream.ReadExactlyAsync(signature, cancellationToken);

        if (!"XISF0100"u8.ToArray().SequenceEqual(signature))
            throw new XisfException("Not a valid XISF 1.0 file");
    }

    private async Task ReadXisfHeader(CancellationToken cancellation)
    {
        var headerLengthBytes = new byte[8];
        await _stream.ReadExactlyAsync(headerLengthBytes, cancellation);

        int headerLength = BitConverter.ToInt32(headerLengthBytes);

        var xmlBytes = new byte[headerLength];
        await _stream.ReadExactlyAsync(xmlBytes, cancellation);
        _minBlockPosition = _stream.Position;

        XmlText = System.Text.Encoding.UTF8.GetString(xmlBytes);

        XDocument doc;

        try
        {
            doc = XDocument.Parse(XmlText);
        }
        catch (XmlException xmlEx)
        {
            throw new XisfException("Error parsing XISF header.", xmlEx);
        }

        var root = doc.Root;
        if (root is null || root.Name.LocalName != "xisf")
        {
            throw new XisfException($"Unknown root XML element");
        }

        var xisfVersion = root.Attribute("version")?.Value;
        if (xisfVersion != "1.0")
        {
            throw new XisfException($"Unsupported xisf version {xisfVersion}");
        }

        _ns = root.GetDefaultNamespace();

        foreach (var imageElement in root.Descendants(_ns + "Image"))
        {
            try
            {
                var image = await ParseImage(imageElement, cancellation);
                _images.Add(image);
            }
            catch (Exception e)
            {
                LogError($"Failed to parse image'{imageElement}'", e);
                if (ThrowOnParsingFailure)
                    throw;
            }
        }

        if (root.Element(_ns + "Metadata") is XElement metadataElement)
        {
            foreach (var propertyElement in metadataElement.Descendants(_ns + "Property"))
            {
                try
                {
                    var property = await ParseProperty(propertyElement, cancellation);
                    if (_properties.TryGetProperty(property.Id, out var existingProperty))
                    {
                        _properties.Remove(existingProperty!);
                        // TODO: Log warning about existing property being replaced.
                    }
                    _properties.Add(property);
                }
                catch (Exception e)
                {
                    LogError($"Failed to parse metadata property '{propertyElement}'", e);
                    if (ThrowOnParsingFailure)
                        throw;
                }
            }
        }
    }

    private async Task<XisfImage> ParseImage(XElement element, CancellationToken cancellationToken)
    {
        var geometryAttr = element.Attribute("geometry")?.Value 
            ?? throw new XisfException("Image element missing 'geometry' attribute");
        
        var geometryParts = geometryAttr.Split(':');
        if (geometryParts.Length != 3)
            throw new XisfException("Only 2D images are supported");
        
        int width = int.Parse(geometryParts[0]);
        int height = int.Parse(geometryParts[1]);
        int channels = int.Parse(geometryParts[2]);
        
        if (width < 1 || height < 1 || channels < 1)
            throw new XisfException("Invalid image geometry");

        var image = new XisfImage(width, height, channels);
        image.Id = element.Attribute("id")?.Value;
        image.Uuid = ParseUuid(element);
        image.Bounds = ParseImageBounds(element);
        image.ImageType = ParseImageType(element);
        image.PixelStorage = ParsePixelStorage(element);
        image.SampleFormat = ParseSampleFormat(element);
        image.ColorSpace = ParseColorSpace(element);
        image.Orientation = ParseOrientation(element);
        image.DataBlock = ParseDataBlock(element);

        if (element.Attribute("offset")?.Value is { } offsetStr &&
            float.TryParse(offsetStr, out float offset))
        {
            image.Offset = offset;
        }

        foreach (var propertyElement in element.Descendants(_ns + "Property"))
        {
            try
            {
                var property = await ParseProperty(propertyElement, cancellationToken);
                if (image.Properties.TryGetProperty(property.Id, out var existingProperty))
                {
                    image.Properties.Remove(existingProperty!);
                    // TODO: Log warning about existing property being replaced.
                }
                image.Properties.Add(property);
            }
            catch (Exception e)
            {
                LogError($"Failed to parse image property '{propertyElement}'", e);
                if (ThrowOnParsingFailure)
                    throw;
            }
        }

        foreach (var fitsKeywordElement in element.Descendants(_ns + "FITSKeyword"))
        {
            try
            {
                var fitsKeyword = ParseFITSKeyword(fitsKeywordElement);
                image.FITSKeywords.Add(fitsKeyword);
            }
            catch (Exception e)
            {
                LogError($"Failed to parse image FITSKeyword '{fitsKeywordElement}'", e);
                if (ThrowOnParsingFailure)
                    throw;
            }
        }

        if (element.Descendants(_ns + "ColorFilterArray").FirstOrDefault() is { } cfaElement)
        {
            try
            {
                image.ColorFilterArray = ParseColorFilterArray(cfaElement);
            }
            catch (Exception e)
            {
                LogError($"Failed to parse image ColorFilterArray '{cfaElement}'", e);
                if (ThrowOnParsingFailure)
                    throw;
            }
        }

        if (element.Descendants(_ns + "RGBWorkingSpace").FirstOrDefault() is { } rgbwsElement)
        {
            try
            {
                image.RGBWorkingSpace = ParseRGBWorkingSpace(rgbwsElement);
            }
            catch (Exception e)
            {
                LogError($"Failed to parse image RGBWorkingSpace '{rgbwsElement}'", e);
                if (ThrowOnParsingFailure)
                    throw;
            }
        }

        if (element.Descendants(_ns + "DisplayFunction").FirstOrDefault() is { } displayFunctionElement)
        {
            try
            {
                image.DisplayFunction = ParseDisplayFunction(displayFunctionElement);
            }
            catch (Exception e)
            {
                LogError($"Failed to parse image DisplayFunction '{displayFunctionElement}'", e);
                if (ThrowOnParsingFailure)
                    throw;
            }
        }

        if (element.Descendants(_ns + "Resolution").FirstOrDefault() is { } resolutionElement)
        {
            try
            {
                image.Resolution = ParseResolutionElement(resolutionElement);
            }
            catch (Exception e)
            {
                LogError($"Failed to parse image Resolution '{resolutionElement}'", e);
                if (ThrowOnParsingFailure)
                    throw;
            }
        }
        
        if (element.Descendants(_ns + "Thumbnail").FirstOrDefault() is { } thumbnailElement)
        {
            try
            {
                image.Thumbnail = await ParseImage(thumbnailElement, cancellationToken);
            }
            catch (Exception e)
            {
                LogError($"Failed to parse image Thumbnail '{thumbnailElement}'", e);
                if (ThrowOnParsingFailure)
                    throw;
            }
        }

        return image;
    }

    private async Task<XisfProperty> ParseProperty(XElement element, CancellationToken cancellationToken)
    {
        ReadOnlyMemory<byte> data = default;
        var location = element.Attribute("location")?.Value;

        if (!string.IsNullOrWhiteSpace(location))
        {
            var dataBlock = ParseDataBlock(element);
            await dataBlock.LoadData(_stream, cancellationToken);
            data = dataBlock.Data;
        }

        var property = ParsePropertyElement(element, data);
        return property;
    }

    private static Guid? ParseUuid(XElement element)
    {
        var uuidStr = element.Attribute("uuid")?.Value;

        if (Guid.TryParse(uuidStr, out var result))
        {
            return result;
        }

        return null;
    }

    private static SampleBounds? ParseImageBounds(XElement element)
    {
        var boundsStr = element.Attribute("bounds")?.Value;
        if (boundsStr is null)
            return null;

        var parts = boundsStr.Split(":");
        if (parts.Length != 2)
            return null;

        if (double.TryParse(parts[0], out var lower) &&
            double.TryParse(parts[1], out var upper))
        {
            return new SampleBounds(lower, upper);
        }

        return null;
    }

    private static ImageType ParseImageType(XElement element)
    {
        var imageTypeStr = element.Attribute("imageType")?.Value ?? "Light";
        if (Enum.TryParse<ImageType>(imageTypeStr, true, out var result))
        {
            return result;
        }

        throw new XisfException($"Unsupported image type: {imageTypeStr}");
    }

    private static PixelStorage ParsePixelStorage(XElement element)
    {
        var pixelStorageStr = element.Attribute("pixelStorage")?.Value ?? "Planar";
        if (Enum.TryParse<PixelStorage>(pixelStorageStr, true, out var result))
        {
            return result;
        }

        throw new XisfException($"Unsupported pixel storage: {pixelStorageStr}");
    }

    private static SampleFormat ParseSampleFormat(XElement element)
    {
        var sampleFormatStr = element.Attribute("sampleFormat")?.Value ?? "Planar";
        if (Enum.TryParse<SampleFormat>(sampleFormatStr, true, out var result))
        {
            return result;
        }

        throw new XisfException($"Unsupported sample format: {sampleFormatStr}");
    }

    private static ColorSpace ParseColorSpace(XElement element)
    {
        var colorSpaceStr = element.Attribute("colorSpace")?.Value ?? "Gray";
        if (Enum.TryParse<ColorSpace>(colorSpaceStr, true, out var result))
        {
            return result;
        }

        throw new XisfException($"Unsupported color space: {colorSpaceStr}");
    }

    private InputBlock ParseDataBlock(XElement element)
    {
        InputBlock block = new();

        var location = element.Attribute("location")?.Value;
        if (string.IsNullOrWhiteSpace(location))
            throw new XisfException("Block missing location attribute.");

        var tokens = location.Split(':');
        if (tokens.Length < 1)
            throw new XisfException("Block has empty location attriute");

        if (tokens[0] == "attachment")
        {
            if (tokens.Length != 3)
                throw new XisfException($"Invalid block location attribute: {location}");

            block.AttachmentPosition = long.Parse(tokens[1]);
            if (block.AttachmentPosition < _minBlockPosition)
                throw new XisfException($"Invalid block position: {block.AttachmentPosition}");

            block.AttachmentSize = int.Parse(tokens[2]);
            if (block.AttachmentSize == 0)
                throw new XisfException($"Invalid block size: {block.AttachmentSize}");

            GetBlockCompression(block, element);
            GetBlockChecksum(block, element);
            // TODO: Byte order
        }
        else if (tokens[0] == "inline")
        {
            if (tokens.Length != 2)
                throw new XisfException($"Invalid block location attribute: {location}");

            var encoding = ParseDataEncoding(tokens[1]);
            GetBlockEncodedData(block, element, encoding);
        }
        else if (tokens[0] == "embedded")
        {
            if (tokens.Length != 1)
                throw new XisfException($"Invalid block location attribute: {location}");

            if (element.Descendants(_ns + "Data").FirstOrDefault() is { } dataElement)
            {
                GetBlockEmbeddedData(block, dataElement);
            }
        }
        else
        {
            throw new XisfException($"Unsupported data block location attribute: {location}");
        }

        return block;
    }

    private static void GetBlockCompression(InputBlock block, XElement element)
    {
        string? compression = element.Attribute("compression")?.Value;
        if (string.IsNullOrWhiteSpace(compression))
            return;

        block.CompressedSize = block.AttachmentSize;

        var tokens = compression.Split(':');
        if (tokens.Length is not (2 or 3))
            throw new XisfException($"Invalid compression attribute: {compression}");

        var codec = tokens[0] switch
        {
            "zlib" or "zlib+sh" => CompressionCodec.Zlib,
            "lz4" or "lz4+sh" => CompressionCodec.Lz4,
            "lz4hc" or "lz4hc+sh" => CompressionCodec.Lz4Hc,
            "zstd" or "zstd+sh" => CompressionCodec.Zstd,
            _ => throw new XisfException($"Unsupported compression codec: {tokens[0]}")
        };

        var uncompressedSize = int.Parse(tokens[1]);

        int itemSize = 1;
        bool byteShuffled = tokens[0].EndsWith("+sh");
        if (byteShuffled)
        {
            if (tokens.Length == 3)
            {
                itemSize = int.Parse(tokens[2]);
            }
            else
            {
                throw new XisfException("Missing byte shuffling size");
            }
        }

        if (itemSize < 1 || itemSize > 8)
            throw new XisfException($"Invalid item size: {itemSize}");

        block.CompressionCodec = codec;
        block.UncompressedSize = uncompressedSize;
        block.ItemSize = itemSize;
        block.ByteShuffling = byteShuffled;
    }

    private static void GetBlockChecksum(InputBlock block, XElement element)
    {
        var checksumStr = element.Attribute("checksum")?.Value;
        if (checksumStr is null)
            return;

        var tokens = checksumStr.Split(':');
        if (tokens.Length != 2)
            throw new XisfException($"Invalid checksum attribute: {checksumStr}");

        ChecksumAlgorithm algorithm = tokens[0].ToLowerInvariant() switch
        {
            "sha1" or "sha-1" => ChecksumAlgorithm.SHA1,
            "sha256" or "sha-256" => ChecksumAlgorithm.SHA256,
            "sha512" or "sha-512" => ChecksumAlgorithm.SHA512,
            "sha3-256" => ChecksumAlgorithm.SHA3_256,
            "sha3-512" => ChecksumAlgorithm.SHA3_512,
            _ => throw new XisfException($"Unsupported checksum type: {tokens[0]}"),
        };

        block.ChecksumAlgorithm = algorithm;
        block.Checksum = tokens[1];
    }

    private static void GetBlockEncodedData(InputBlock block, XElement element, DataEncoding encoding)
    {
        GetBlockCompression(block, element);
        GetBlockChecksum(block, element);
        // TODO: Byte order

        var data = XisfUtil.Decode(element.Value, encoding);
        block.AttachmentSize = data.Length;

        if (block.IsCompressed)
        {
            block.SetCompressedData(data);
        }
        else
        {
            block.SetData(data);
            block.VerifyChecksum();
            // TODO: Byte order
        }
    }

    private static void GetBlockEmbeddedData(InputBlock block, XElement element)
    {
        DataEncoding encoding = DataEncoding.Base64;
        if (element.Attribute("encoding") is not null)
        {
            encoding = ParseDataEncoding(element.Attribute("encoding")!.Value);

            // Embedded data cannot have "None" encoding.
            if (encoding is DataEncoding.None)
                encoding = DataEncoding.Base64;
        }

        GetBlockEncodedData(block, element, encoding);
    }

    private static DataEncoding ParseDataEncoding(string? text)
    {
        return text switch
        {
            "base64" => DataEncoding.Base64,
            "base16" => DataEncoding.Base16,
            _ => DataEncoding.None,
        };
    }

    private static FITSKeyword ParseFITSKeyword(XElement element)
    {
        var name = element.Attribute("name")?.Value
            ?? throw new XisfException("FITSKeyword element missing 'name' attribute");
        var value = element.Attribute("value")?.Value;
        var comment = element.Attribute("comment")?.Value;

        return new FITSKeyword(name, value, comment);
    }

    private static ColorFilterArray ParseColorFilterArray(XElement element)
    {
        var patternStr = element.Attribute("pattern")?.Value
            ?? throw new XisfException("ColorFilterArray element missing 'pattern' attribute");
        var widthStr = element.Attribute("width")?.Value
            ?? throw new XisfException("ColorFilterArray element missing 'width' attribute");
        var heightStr = element.Attribute("height")?.Value
            ?? throw new XisfException("ColorFilterArray element missing 'height' attribute");
        var name = element.Attribute("name")?.Value;

        if (!int.TryParse(widthStr, out int width))
        {
            throw new XisfException($"ColorFilterArray width must be an integer: {widthStr}");
        }

        if (!int.TryParse(heightStr, out int height))
        {
            throw new XisfException($"ColorFilterArray height must be an integer: {widthStr}");
        }

        List<CFAElement> cfaElements = [];

        for (int i = 0; i < patternStr.Length; i++)
        {
            var patternCharacter = patternStr[i];
            CFAElement cfaElement = patternCharacter switch
            {
                '0' => CFAElement.Undefined,
                'r' or 'R' => CFAElement.R,
                'g' or 'G' => CFAElement.G,
                'b' or 'B' => CFAElement.B,
                'w' or 'W' => CFAElement.W,
                'c' or 'C' => CFAElement.C,
                'm' or 'M' => CFAElement.M,
                'y' or 'Y' => CFAElement.Y,
                _ => throw new XisfException($"Unknown ColorFilterArray pattern element: {patternCharacter}"),
            };
            cfaElements.Add(cfaElement);
        }

        return new ColorFilterArray(cfaElements, width, height, name);
    }

    private static RGBWorkingSpace ParseRGBWorkingSpace(XElement element)
    {
        var chromaticityXStr = element.Attribute("x")?.Value
            ?? throw new XisfException("RGBWorkingSpace element missing 'x' attribute");
        var chromaticityYStr = element.Attribute("y")?.Value
            ?? throw new XisfException("RGBWorkingSpace element missing 'y' attribute");
        var luminanceStr = element.Attribute("Y")?.Value
            ?? throw new XisfException("RGBWorkingSpace element missing 'Y' attribute");
        var gamma = element.Attribute("gamma")?.Value
            ?? throw new XisfException("RGBWorkingSpace element missing 'gamma' attribute");
        var name = element.Attribute("name")?.Value;

        var chromaticityX = GetThreeElementArray("x", chromaticityXStr);
        var chromaticityY = GetThreeElementArray("y", chromaticityYStr);
        var luminance = GetThreeElementArray("Y", luminanceStr);

        return new RGBWorkingSpace(gamma, chromaticityX, chromaticityY, luminance, name);

        static double[] GetThreeElementArray(string name, string value)
        {
            var parts = value.Split(':');
            if (parts.Length != 3)
                throw new XisfException($"RGBWorkingSpace attribute {name} must contain three numbers separated by colons.");

            if (!(double.TryParse(parts[0], out double d0)
                && double.TryParse(parts[1], out double d1)
                && double.TryParse(parts[2], out double d2)))
            {
                throw new XisfException($"RGBWorkingSpace attribute {name} must contain three numbers separated by colons.");
            }

            return [d0, d1, d2];
        }
    }

    private static DisplayFunction ParseDisplayFunction(XElement element)
    {
        var midtonesStr = element.Attribute("m")?.Value
            ?? throw new XisfException("DisplayFunction element missing 'm' attribute");
        var shadowsStr = element.Attribute("s")?.Value
            ?? throw new XisfException("DisplayFunction element missing 's' attribute");
        var highlightsStr = element.Attribute("h")?.Value
            ?? throw new XisfException("DisplayFunction element missing 'h' attribute");
        var shadowDynamicRangeStr = element.Attribute("l")?.Value
            ?? throw new XisfException("DisplayFunction element missing 'l' attribute");
        var highlightDynamicRangeStr = element.Attribute("r")?.Value
            ?? throw new XisfException("DisplayFunction element missing 'r' attribute");
        var name = element.Attribute("name")?.Value;

        var midtones = GetFourElementArray("m", midtonesStr);
        var shadows = GetFourElementArray("s", shadowsStr);
        var highlights = GetFourElementArray("h", highlightsStr);
        var shadowDynamicRange = GetFourElementArray("l", shadowDynamicRangeStr);
        var highlightDynamicRange = GetFourElementArray("r", highlightDynamicRangeStr);

        return new DisplayFunction(midtones, shadows, highlights, shadowDynamicRange, highlightDynamicRange, name);

        static double[] GetFourElementArray(string name, string value)
        {
            var parts = value.Split(':');
            if (parts.Length != 4)
                throw new XisfException($"DisplayFunction attribute {name} must contain four numbers separated by colons.");

            if (!(double.TryParse(parts[0], out double d0)
                && double.TryParse(parts[1], out double d1)
                && double.TryParse(parts[2], out double d2)
                && double.TryParse(parts[3], out double d3)))
            {
                throw new XisfException($"DisplayFunction attribute {name} must contain four numbers separated by colons.");
            }

            return [d0, d1, d2, d3];
        }
    }

    private static Resolution ParseResolutionElement(XElement element)
    {
        var horizontal = element.Attribute("horizontal")?.Value
            ?? throw new XisfException("Resolution element missing 'horizontal' attribute");
        var vertical = element.Attribute("vertical")?.Value
            ?? throw new XisfException("Resolution element missing 'vertical' attribute");
        var unit = element.Attribute("unit")?.Value;

        return new Resolution(
            double.Parse(horizontal, CultureInfo.InvariantCulture),
            double.Parse(vertical, CultureInfo.InvariantCulture),
            unit?.ToLowerInvariant() switch
            {
                "inch" => ResolutionUnit.Inch,
                "cm" => ResolutionUnit.Centimeter,
                _ => null,
            }
        );
    }

    private static ImageOrientation? ParseOrientation(XElement element)
    {
        var orientationStr = element.Attribute("orientation")?.Value;

        if (orientationStr is null)
            return null;

        return orientationStr.ToLowerInvariant() switch
        {
            "0" => ImageOrientation.Default,
            "flip" => ImageOrientation.Flip,
            "90" => ImageOrientation.Rotate90,
            "90;flip" => ImageOrientation.Rotate90Flip,
            "-90" => ImageOrientation.RotateMinus90,
            "-90;flip" => ImageOrientation.RotateMinus90Flip,
            "180" => ImageOrientation.Rotate180,
            "180;flip" => ImageOrientation.Rotate180Flip,
            _ => throw new XisfException($"Unsupported image orientation: {orientationStr}")
        };
    }

    private static XisfProperty ParsePropertyElement(XElement propertyElement, ReadOnlyMemory<byte> data = default)
    {
        var id = propertyElement.Attribute("id")?.Value
            ?? throw new XisfException("Property missing 'id' attribute");
        var typeStr = propertyElement.Attribute("type")?.Value
            ?? throw new XisfException($"Property '{id}' missing 'type' attribute");
        var type = ParsePropertyType(typeStr);
        var format = propertyElement.Attribute("format")?.Value;
        var comment = propertyElement.Attribute("comment")?.Value;
        var value = propertyElement.Attribute("value")?.Value ?? propertyElement.Value;

        XisfProperty property;

        if (typeStr.EndsWith("Vector"))
        {
            if (propertyElement.Attribute("length") is null)
                throw new XisfException($"Vector property is missing length attribute");

            int numberOfElements = int.Parse(propertyElement.Attribute("length")!.Value);
            int elementSize = XisfUtil.GetBytesPerElement(type);

            if (numberOfElements * elementSize != data.Length)
                throw new XisfException("Data length does not match expected vector size");

            property = CreateVectorProperty(id, type, data, format, comment);
        }
        else if (typeStr.EndsWith("Matrix"))
        {
            if (propertyElement.Attribute("rows") is null)
                throw new XisfException($"Matrix property is missing rows attribute");

            if (propertyElement.Attribute("columns") is null)
                throw new XisfException($"Matrix property is missing columns attribute");

            int rows = int.Parse(propertyElement.Attribute("rows")!.Value);
            int columns = int.Parse(propertyElement.Attribute("columns")!.Value);

            int numberOfElements = rows * columns;
            int elementSize = XisfUtil.GetBytesPerElement(type);

            if (numberOfElements * elementSize != data.Length)
                throw new XisfException("Data length does not match expected matrix size");

            property = CreateMatrixProperty(id, type, data, rows, columns, format, comment);
        }
        else
        {
            property = CreateProperty(id, type, value, format, comment);
        }

        return property;

        static XisfPropertyType ParsePropertyType(string typeStr)
        {
            if (Enum.TryParse<XisfPropertyType>(typeStr, true, out var result))
            {
                return result;
            }

            throw new XisfException($"Unknown property type: {typeStr}");
        }
    }

    private static T[] BytesToArray<T>(ReadOnlyMemory<byte> bytes)
        where T : unmanaged
    {
        // TODO: Byte order
        var span = MemoryMarshal.Cast<byte, T>(bytes.Span);
        return span.ToArray();
    }

    private static XisfProperty CreateProperty(string id, XisfPropertyType type, string value, string? format, string? comment)
    {
        if (type == XisfPropertyType.String)
        {
            return new XisfStringProperty(id, value, comment, format);
        }
        else if (type == XisfPropertyType.TimePoint)
        {
            var timePointValue = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
            return new XisfTimePointProperty(id, timePointValue, comment, format);
        }
        else
        {
            object propertyValue = type switch
            {
                XisfPropertyType.Boolean => ParseBoolean(value),
                XisfPropertyType.Int8 => sbyte.Parse(value, CultureInfo.InvariantCulture),
                XisfPropertyType.UInt8 => byte.Parse(value, CultureInfo.InvariantCulture),
                XisfPropertyType.Int16 => short.Parse(value, CultureInfo.InvariantCulture),
                XisfPropertyType.UInt16 => ushort.Parse(value, CultureInfo.InvariantCulture),
                XisfPropertyType.Int32 => int.Parse(value, CultureInfo.InvariantCulture),
                XisfPropertyType.UInt32 => uint.Parse(value, CultureInfo.InvariantCulture),
                XisfPropertyType.Int64 => long.Parse(value, CultureInfo.InvariantCulture),
                XisfPropertyType.UInt64 => ulong.Parse(value, CultureInfo.InvariantCulture),
                XisfPropertyType.Float32 => float.Parse(value, CultureInfo.InvariantCulture),
                XisfPropertyType.Float64 => double.Parse(value, CultureInfo.InvariantCulture),
                _ => throw new XisfException($"Unsupported property type: {type}")
            };

            return new XisfScalarProperty(id, type, propertyValue, comment, format);
        }

        static bool ParseBoolean(string value)
        {
            // XISF spec allows "0"/"1" or "true"/"false"
            return value switch
            {
                "0" or "false" or "False" => false,
                "1" or "true" or "True" => true,
                _ => throw new XisfException($"Invalid boolean value: {value}")
            };
        }
    }

    private static XisfVectorProperty CreateVectorProperty(string id, XisfPropertyType type, ReadOnlyMemory<byte> data, string? format, string? comment)
    {
        return type switch
        {
            XisfPropertyType.I8Vector => XisfVectorProperty.Create(id, BytesToArray<sbyte>(data), comment, format),
            XisfPropertyType.UI8Vector => XisfVectorProperty.Create(id, BytesToArray<byte>(data), comment, format),
            XisfPropertyType.I16Vector => XisfVectorProperty.Create(id, BytesToArray<short>(data), comment, format),
            XisfPropertyType.UI16Vector => XisfVectorProperty.Create(id, BytesToArray<ushort>(data), comment, format),
            XisfPropertyType.I32Vector => XisfVectorProperty.Create(id, BytesToArray<int>(data), comment, format),
            XisfPropertyType.UI32Vector => XisfVectorProperty.Create(id, BytesToArray<uint>(data), comment, format),
            XisfPropertyType.I64Vector => XisfVectorProperty.Create(id, BytesToArray<long>(data), comment, format),
            XisfPropertyType.UI64Vector => XisfVectorProperty.Create(id, BytesToArray<ulong>(data), comment, format),
            XisfPropertyType.F32Vector => XisfVectorProperty.Create(id, BytesToArray<float>(data), comment, format),
            XisfPropertyType.F64Vector => XisfVectorProperty.Create(id, BytesToArray<double>(data), comment, format),
            _ => throw new XisfException($"Unsupported vector type: {type}"),
        };
    }

    private static XisfMatrixProperty CreateMatrixProperty(string id, XisfPropertyType type, ReadOnlyMemory<byte> data, int rows, int columns, string? format, string? comment)
    {
        return type switch
        {
            XisfPropertyType.I8Matrix => XisfMatrixProperty.Create(id, BytesToArray<sbyte>(data), rows, columns, comment, format),
            XisfPropertyType.UI8Matrix => XisfMatrixProperty.Create(id, BytesToArray<byte>(data), rows, columns, comment, format),
            XisfPropertyType.I16Matrix => XisfMatrixProperty.Create(id, BytesToArray<short>(data), rows, columns, comment, format),
            XisfPropertyType.UI16Matrix => XisfMatrixProperty.Create(id, BytesToArray<ushort>(data), rows, columns, comment, format),
            XisfPropertyType.I32Matrix => XisfMatrixProperty.Create(id, BytesToArray<int>(data), rows, columns, comment, format),
            XisfPropertyType.UI32Matrix => XisfMatrixProperty.Create(id, BytesToArray<uint>(data), rows, columns, comment, format),
            XisfPropertyType.I64Matrix => XisfMatrixProperty.Create(id, BytesToArray<long>(data), rows, columns, comment, format),
            XisfPropertyType.UI64Matrix => XisfMatrixProperty.Create(id, BytesToArray<ulong>(data), rows, columns, comment, format),
            XisfPropertyType.F32Matrix => XisfMatrixProperty.Create(id, BytesToArray<float>(data), rows, columns, comment, format),
            XisfPropertyType.F64Matrix => XisfMatrixProperty.Create(id, BytesToArray<double>(data), rows, columns, comment, format),
            _ => throw new XisfException($"Unsupported matrix type: {type}"),
        };
    }
}

