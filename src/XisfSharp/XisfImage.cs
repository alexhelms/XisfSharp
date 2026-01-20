using System.Runtime.InteropServices;
using XisfSharp.FITS;
using XisfSharp.IO;

namespace XisfSharp;

public delegate void SpanAction<T>(Span<T> span);

public class XisfImage
{
    /// <summary>
    /// Gets the width of the image in pixels.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the height of the image in pixels.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets the number of channels in the image.
    /// </summary>
    public int Channels { get; private set; } = 1;

    internal InputBlock DataBlock { get; set; } = new();

    /// <summary>
    /// Gets or sets the optional thumbnail image associated with this image.
    /// </summary>
    /// <remarks>
    /// If the thumbnail is an attachment in the XISF file, it is necessary to call
    /// <see cref="XisfReader.ReadThumbnailAsync(XisfImage, CancellationToken)"/> to
    /// load the thumbnail data after reading the main image.
    /// </remarks>
    public XisfImage? Thumbnail { get; set; }

    /// <summary>
    /// Get the raw image data as a read-only memory buffer.
    /// </summary>
    public ReadOnlyMemory<byte> Data => DataBlock.Data;

    /// <summary>
    /// Gets or sets the type of image (for example, Light, Dark, Flat, Bias).
    /// </summary>
    public ImageType ImageType { get; set; } = ImageType.Light;

    /// <summary>
    /// Gets or sets the pixel storage format indicating how pixel data is arranged in memory.
    /// </summary>
    public PixelStorage PixelStorage { get; set; } = PixelStorage.Planar;

    /// <summary>
    /// Gets or sets the sample format defining the data type of each pixel component.
    /// </summary>
    public SampleFormat SampleFormat { get; internal set; } = SampleFormat.UInt16;

    /// <summary>
    /// Gets or sets the color space of the image (for example, Gray, RGB).
    /// </summary>
    public ColorSpace ColorSpace { get; set; } = ColorSpace.Gray;

    /// <summary>
    /// Gets or sets the optional sample bounds defining the minimum and maximum sample values.
    /// </summary>
    /// <remarks>
    /// <see cref="Bounds"/> is required for <see cref="SampleFormat.Float32"/> or <see cref="SampleFormat.Float64"/> images.
    /// <see cref="XisfWriter"/> will automatically apply defaults, or you can supply values with <see cref="XisfWriterOptions"/>.
    /// </remarks>
    public SampleBounds? Bounds { get; set; }

    /// <summary>
    /// Gets or sets the optional color filter array (CFA) pattern for Bayer matrix sensors.
    /// </summary>
    public ColorFilterArray? ColorFilterArray { get; set; }

    /// <summary>
    /// Gets or sets the optional RGB working space defining color primaries and white point.
    /// </summary>
    public RGBWorkingSpace? RGBWorkingSpace { get; set; }

    /// <summary>
    /// Gets or sets the optional display function for the screen transfer function (stretch).
    /// </summary>
    public DisplayFunction? DisplayFunction { get; set; }

    /// <summary>
    /// Gets or sets the optional image resolution in pixels per inch or pixels per centimeter.
    /// </summary>
    public Resolution? Resolution { get; set; }

    /// <summary>
    /// Gets or sets the optional orientation of the image indicating rotation or mirroring.
    /// </summary>
    public ImageOrientation? Orientation { get; set; }

    /// <summary>
    /// Gets or sets an optional identifier string for the image.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets an optional universally unique identifier (UUID) for the image.
    /// </summary>
    public Guid? Uuid { get; set; }

    /// <summary>
    /// Gets or sets an optional offset/pedestal value applied to the image data.
    /// </summary>
    public float? Offset { get; set; }

    /// <summary>
    /// Gets or sets the collection of custom XISF properties associated with the image.
    /// </summary>
    public XisfPropertyCollection Properties { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of FITS keywords embedded in the image metadata.
    /// </summary>
    public List<FITSKeyword> FITSKeywords { get; set; } = [];

    /// <summary>
    /// Create a new and empty <see cref="XisfImage"/>.
    /// </summary>
    public XisfImage()
        : this(0, 0)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with the specified dimensions.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    public XisfImage(int width, int height)
        : this([], width, height, 1, SampleFormat.UInt8)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with the specified dimensions and channel count.
    /// </summary>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="channels">The number of channels per pixel.</param>
    public XisfImage(int width, int height, int channels)
        : this([], width, height, channels, SampleFormat.UInt8)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with byte data and specified dimensions.
    /// </summary>
    /// <param name="data">The byte array containing the raw image data.</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="sampleFormat">The format of each sample in the image data. Defaults to <see cref="SampleFormat.UInt8"/>.</param>
    public XisfImage(byte[] data, int width, int height, SampleFormat sampleFormat = SampleFormat.UInt8)
        : this(data, width, height, 1, sampleFormat)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with byte data, dimensions, and channel count.
    /// </summary>
    /// <param name="data">The byte array containing the raw image data.</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="channels">The number of channels per pixel.</param>
    /// <param name="sampleFormat">The format of each sample in the image data. Defaults to <see cref="SampleFormat.UInt8"/>.</param>
    public XisfImage(byte[] data, int width, int height, int channels, SampleFormat sampleFormat = SampleFormat.UInt8)
    {
        SetData(data, width, height, channels, sampleFormat);
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with unsigned 16-bit integer data and specified dimensions.
    /// </summary>
    /// <param name="data">The array containing 16-bit unsigned integer pixel data.</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    public XisfImage(ushort[] data, int width, int height)
        : this(data, width, height, 1)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with unsigned 16-bit integer data, dimensions, and channel count.
    /// </summary>
    /// <param name="data">The array containing 16-bit unsigned integer pixel data.</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="channels">The number of channels per pixel.</param>
    public XisfImage(ushort[] data, int width, int height, int channels)
    {
        SetData(data, width, height, channels);
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with 32-bit floating-point data and specified dimensions.
    /// </summary>
    /// <param name="data">The array containing 32-bit floating-point pixel data.</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    public XisfImage(float[] data, int width, int height)
        : this(data, width, height, 1)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with 32-bit floating-point data, dimensions, and channel count.
    /// </summary>
    /// <param name="data">The array containing 32-bit floating-point pixel data.</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="channels">The number of channels per pixel.</param>
    public XisfImage(float[] data, int width, int height, int channels)
    {
        SetData(data, width, height, channels);
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with 64-bit floating-point data and specified dimensions.
    /// </summary>
    /// <param name="data">The array containing 64-bit floating-point pixel data.</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    public XisfImage(double[] data, int width, int height)
        : this(data, width, height, 1)
    {
    }

    /// <summary>
    /// Create a new <see cref="XisfImage"/> with 64-bit floating-point data, dimensions, and channel count.
    /// </summary>
    /// <param name="data">The array containing 64-bit floating-point pixel data.</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="channels">The number of channels per pixel.</param>
    public XisfImage(double[] data, int width, int height, int channels)
    {
        SetData(data, width, height, channels);
    }

    /// <summary>
    /// Asynchronously loads an XISF image from the specified file path.
    /// </summary>
    /// <param name="path">The path to the XISF file to load.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded <see cref="XisfImage"/>.</returns>
    public static async Task<XisfImage> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        await using var reader = new XisfReader(path);
        await reader.ReadHeaderAsync(cancellationToken);
        return await reader.ReadImageAsync(0, cancellationToken);
    }

    /// <summary>
    /// Asynchronously loads an XISF image from the specified stream.
    /// </summary>
    /// <param name="stream">The stream containing the XISF data to load. The stream must be seekable.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded <see cref="XisfImage"/>.</returns>
    public static Task<XisfImage> LoadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return LoadAsync(stream, false, cancellationToken);
    }

    /// <summary>
    /// Asynchronously loads an XISF image from the specified stream with an option to leave the stream open.
    /// </summary>
    /// <param name="stream">The stream containing the XISF data to load. The stream must be seekable.</param>
    /// <param name="leaveOpen">If true, the stream is left open after loading; otherwise, the stream is disposed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded <see cref="XisfImage"/>.</returns>
    public static async Task<XisfImage> LoadAsync(Stream stream, bool leaveOpen, CancellationToken cancellationToken = default)
    {
        await using var reader = new XisfReader(stream, leaveOpen);
        await reader.ReadHeaderAsync(cancellationToken);
        return await reader.ReadImageAsync(0, cancellationToken);
    }

    /// <summary>
    /// Asynchronously saves the image to the specified file path.
    /// </summary>
    /// <param name="path">The path where the XISF file will be saved.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public Task SaveAsync(string path, CancellationToken cancellationToken = default)
    {
        return SaveAsync(path, new XisfWriterOptions(), cancellationToken);
    }

    /// <summary>
    /// Asynchronously saves the image to the specified file path with custom writer options.
    /// </summary>
    /// <param name="path">The path where the XISF file will be saved.</param>
    /// <param name="options">The options to use when writing the XISF file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveAsync(string path, XisfWriterOptions options, CancellationToken cancellationToken = default)
    {
        await using var writer = new XisfWriter(path, options);
        writer.AddImage(this);
        await writer.SaveAsync(cancellationToken);
    }

    /// <summary>
    /// Asynchronously saves the image to the specified stream.
    /// </summary>
    /// <param name="stream">The stream to write the XISF data to.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public Task SaveAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        return SaveAsync(stream, new XisfWriterOptions(), false, cancellationToken);
    }

    /// <summary>
    /// Asynchronously saves the image to the specified stream with custom writer options.
    /// </summary>
    /// <param name="stream">The stream to write the XISF data to.</param>
    /// <param name="options">The options to use when writing the XISF file.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public Task SaveAsync(Stream stream, XisfWriterOptions options, CancellationToken cancellationToken = default)
    {
        return SaveAsync(stream, options, false, cancellationToken);
    }

    /// <summary>
    /// Asynchronously saves the image to the specified stream with custom writer options and an option to leave the stream open.
    /// </summary>
    /// <param name="stream">The stream to write the XISF data to.</param>
    /// <param name="options">The options to use when writing the XISF file.</param>
    /// <param name="leaveOpen">If true, the stream is left open after saving; otherwise, the stream is disposed.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    public async Task SaveAsync(Stream stream, XisfWriterOptions options, bool leaveOpen, CancellationToken cancellationToken = default)
    {
        await using var writer = new XisfWriter(stream, options, leaveOpen);
        writer.AddImage(this);
        await writer.SaveAsync(cancellationToken);
    }

    /// <summary>
    /// Sets the image data and associated metadata.
    /// </summary>
    /// <remarks>
    /// The method updates the image dimensions, channel count, sample format, and underlying data block.
    /// The expected data length is determined by width * height * channels * sample format size.
    /// </remarks>
    /// <param name="data">The byte array containing the raw image data. The length must match the expected size calculated from the
    /// specified width, height, channels, and sample format. Cannot be null.</param>
    /// <param name="width">The width of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="height">The height of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="channels">The number of channels per pixel (for example, 1 for grayscale, 3 for RGB). Must be greater than or equal to 1.</param>
    /// <param name="sampleFormat">The format of each sample in the image data, which determines the size of each pixel component.</param>
    public void SetData(byte[] data, int width, int height, int channels, SampleFormat sampleFormat)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(channels, 1);

        int sampleFormatSize = XisfUtil.SampleFormatSize(sampleFormat);
        long expectedSize = (long)width * height * channels * sampleFormatSize;

        if (expectedSize > int.MaxValue)
            throw new ArgumentException($"Image size exceeds maximum supported size of {int.MaxValue} bytes.");

        if (data.Length > 0 && data.Length != expectedSize)
            throw new ArgumentException($"Data length {data.Length} does not match expected size {expectedSize}.");

        Width = width;
        Height = height;
        Channels = channels;
        SampleFormat = sampleFormat;

        DataBlock.SetData(data);
    }

    /// <summary>
    /// Sets the image data and image dimensions.
    /// </summary>
    /// <remarks>
    /// The method infers the sample format from the type parameter <typeparamref name="T"/>. The data is copied and converted to a
    /// byte array before being set. This method is suitable for use with unmanaged types such as byte, ushort, int, float, or double.
    /// </remarks>
    /// <typeparam name="T">The unmanaged type of each pixel value in the data buffer.</typeparam>
    /// <param name="data">A read-only span containing the pixel data to set. The length must match width * height * channels.</param>
    /// <param name="width">The width of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="height">The height of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="channels">The number of channels per pixel (for example, 1 for grayscale, 3 for RGB). Must be greater than or equal to 1.</param>
    public void SetData<T>(ReadOnlySpan<T> data, int width, int height, int channels)
        where T : unmanaged
    {
        SampleFormat sampleFormat = XisfUtil.ToSampleFormat<T>();
        byte[] dataAsBytes = MemoryMarshal.AsBytes(data).ToArray();
        SetData(dataAsBytes, width, height, channels, sampleFormat);
    }

    /// <summary>
    /// Sets the image data using existing image dimensions.
    /// </summary>
    /// <remarks>
    /// This method replaces the current image data with the contents of the provided span. The caller is responsible
    /// for ensuring that the data is correctly formatted and sized for the image dimensions and channel count.
    /// </remarks>
    /// <typeparam name="T">The unmanaged type of each pixel element in the data span.</typeparam>
    /// <param name="data">A read-only span containing the pixel data to set. The length of the span must match the product of the image's
    /// width, height, channel count.</param>
    public void SetData<T>(ReadOnlySpan<T> data)
        where T : unmanaged
    {
        SetData(data, Width, Height, Channels);
    }

    /// <summary>
    /// Sets the image data and image dimensions.
    /// </summary>
    /// <remarks>
    /// The method infers the sample format from the type parameter <typeparamref name="T"/>. The data is copied and converted to a
    /// byte array before being set. This method is suitable for use with unmanaged types such as byte, ushort, int, float, or double.
    /// </remarks>
    /// <typeparam name="T">The unmanaged type of each pixel value in the data buffer.</typeparam>
    /// <param name="data">An array containing the pixel data to set. The length must match width * height * channels.</param>
    /// <param name="width">The width of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="height">The height of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="channels">The number of channels per pixel (for example, 1 for grayscale, 3 for RGB). Must be greater than or equal to 1.</param>
    public void SetData<T>(T[] data, int width, int height, int channels)
        where T : unmanaged
    {
        SetData(data.AsSpan(), width, height, channels);
    }

    /// <summary>
    /// Sets the image data using existing image dimensions.
    /// </summary>
    /// <remarks>
    /// This method replaces the current image data with the contents of the provided span. The caller is responsible
    /// for ensuring that the data is correctly formatted and sized for the image dimensions and channel count.
    /// </remarks>
    /// <typeparam name="T">The unmanaged type of each pixel element in the data span.</typeparam>
    /// <param name="data">An array containing the pixel data to set. The length of the span must match the product of the image's
    /// width, height, channel count.</param>
    public void SetData<T>(T[] data)
        where T : unmanaged
    {
        SetData(data.AsSpan());
    }

    /// <summary>
    /// Sets the image data using the specified dimensions, channel count, and a delegate that populates the data buffer.
    /// </summary>
    /// <remarks>
    /// The delegate specified by setDataAction is called with a span large enough to hold all image
    /// data for the given dimensions and channel count. The type parameter T determines the sample format used for the
    /// image data.
    /// </remarks>
    /// <typeparam name="T">The unmanaged value type of each sample in the image data.</typeparam>
    /// <param name="width">The width of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="height">The height of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="channels">The number of channels per pixel (for example, 1 for grayscale, 3 for RGB). Must be greater than or equal to 1.</param>
    /// <param name="setDataAction">A delegate that receives a span representing the image data buffer to populate with values.</param>
    public void SetData<T>(int width, int height, int channels, SpanAction<T> setDataAction)
        where T : unmanaged
    {
        ArgumentNullException.ThrowIfNull(setDataAction);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(channels, 1);

        SampleFormat sampleFormat = XisfUtil.ToSampleFormat<T>();
        int sampleFormatSize = XisfUtil.SampleFormatSize(sampleFormat);
        long expectedSize = (long)width * height * channels * sampleFormatSize;

        if (expectedSize > int.MaxValue)
            throw new ArgumentException($"Image size exceeds maximum supported size of {int.MaxValue} bytes.");

        byte[] data = new byte[expectedSize];
        Span<T> typedSpan = MemoryMarshal.Cast<byte, T>(data.AsSpan());
        setDataAction(typedSpan);

        SetData(data, width, height, channels, sampleFormat);
    }

    /// <summary>
    /// Sets the pixel data for one or more channels using the specified width, height, and channel data arrays.
    /// </summary>
    /// <remarks>
    /// All channel buffers must contain pixel data for the entire image area, with each buffer
    /// representing a separate channel. The method does not modify the input buffers.
    /// </remarks>
    /// <typeparam name="T">The unmanaged type of the pixel data elements for each channel.</typeparam>
    /// <param name="width">The width, in pixels, of the image data to set. Must be greater than zero.</param>
    /// <param name="height">The height, in pixels, of the image data to set. Must be greater than zero.</param>
    /// <param name="channelData">An array of buffers, each containing pixel data for a channel. Each buffer must have a length
    /// equal to width * height. At least one channel is required.</param>
    public void SetData<T>(int width, int height, params T[][] channelData)
        where T : unmanaged
    {
        if (channelData.Length == 0)
            throw new ArgumentException("At least one channel is required.", nameof(channelData));

        int pixelCount = width * height;
        for (int i = 0; i < channelData.Length; i++)
        {
            if (channelData[i].Length != pixelCount)
                throw new ArgumentException($"Channel {i} length {channelData[i].Length} does not match expeted {pixelCount}.");
        }

        SetData<T>(width, height, channelData.Length, span =>
        {
            for (int i = 0; i < channelData.Length; i++)
            {
                channelData[i].AsSpan().CopyTo(span.Slice(i * pixelCount, pixelCount));
            }
        });
    }

    /// <summary>
    /// Sets the image data using three channels of pixel values for the specified dimensions.
    /// </summary>
    /// <remarks>
    /// This method expects each channel to contain pixel data for the entire image. The channels are combined 
    /// in order to form a three-channel image. The pixel data type is determined by the generic type parameter.
    /// </remarks>
    /// <typeparam name="T">The unmanaged type of the pixel data elements in each channel.</typeparam>
    /// <param name="width">The width of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="height">The height of the image, in pixels. Must be greater than or equal to 0.</param>
    /// <param name="channel0">A read-only span containing pixel values for the first channel. The length must equal width * height.</param>
    /// <param name="channel1">A read-only span containing pixel values for the second channel. The length must equal width * height.</param>
    /// <param name="channel2">A read-only span containing pixel values for the third channel. The length must equal width * height.</param>
    public void SetData<T>(int width, int height, ReadOnlySpan<T> channel0, ReadOnlySpan<T> channel1, ReadOnlySpan<T> channel2)
        where T : unmanaged
    {
        static void ValidateChannelLength<U>(ReadOnlySpan<U> channel, int expected, int channelIndex)
        {
            if (channel.Length != expected)
                throw new ArgumentException($"Channel {channelIndex} length {channel.Length} does not match expected {expected}.");
        }

        int pixelCount = width * height;
        ValidateChannelLength(channel0, pixelCount, 0);
        ValidateChannelLength(channel1, pixelCount, 1);
        ValidateChannelLength(channel2, pixelCount, 2);

        SampleFormat sampleFormat = XisfUtil.ToSampleFormat<T>();
        int sampleFormatSize = XisfUtil.SampleFormatSize(sampleFormat);
        long expectedSize = (long)width * height * 3 * sampleFormatSize;

        if (expectedSize > int.MaxValue)
            throw new ArgumentException($"Image size exceeds maximum supported size of {int.MaxValue} bytes.");

        byte[] data = new byte[expectedSize];
        Span<T> span = MemoryMarshal.Cast<byte, T>(data.AsSpan());

        // I'd like to do SetData<T>(..., span => { ... }) but Span cannot be captured in a closure.
        channel0.CopyTo(span.Slice(0, pixelCount));
        channel1.CopyTo(span.Slice(1 * pixelCount, pixelCount));
        channel2.CopyTo(span.Slice(2 * pixelCount, pixelCount));

        SetData(data, width, height, 3, sampleFormat);
    }
}
