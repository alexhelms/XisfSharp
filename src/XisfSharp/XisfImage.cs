using XisfSharp.FITS;
using XisfSharp.IO;

namespace XisfSharp;

public class XisfImage
{
    public int Width { get; private set; }

    public int Height { get; private set; }

    public int Channels { get; private set; } = 1;

    internal InputBlock DataBlock { get; set; } = new();

    public XisfImage? Thumbnail { get; set; }

    public ReadOnlyMemory<byte> Data => DataBlock.Data;

    public ImageType ImageType { get; set; } = ImageType.Light;

    public PixelStorage PixelStorage { get; set; } = PixelStorage.Planar;

    public SampleFormat SampleFormat { get; set; } = SampleFormat.UInt16;

    public ColorSpace ColorSpace { get; set; } = ColorSpace.Gray;

    public SampleBounds? Bounds { get; set; }

    public ColorFilterArray? ColorFilterArray { get; set; }

    public RGBWorkingSpace? RGBWorkingSpace { get; set; }

    public DisplayFunction? DisplayFunction { get; set; }

    public Resolution? Resolution { get; set; }

    public ImageOrientation? Orientation { get; set; }

    public string? Id { get; set; }

    public Guid? Uuid { get; set; }

    public float? Offset { get; set; }

    public XisfPropertyCollection Properties { get; set; } = [];

    public List<FITSKeyword> FITSKeywords { get; set; } = [];

    public XisfImage(int width, int height)
        : this([], width, height, 1, SampleFormat.UInt8)
    {
    }

    public XisfImage(int width, int height, int channels)
        : this([], width, height, channels, SampleFormat.UInt8)
    {
    }

    public XisfImage(byte[] data, int width, int height, SampleFormat sampleFormat = SampleFormat.UInt8)
        : this(data, width, height, 1, sampleFormat)
    {
    }

    public XisfImage(byte[] data, int width, int height, int channels, SampleFormat sampleFormat = SampleFormat.UInt8)
    {
        SetData(data, width, height, channels, sampleFormat);
    }

    public void SetData(byte[] data, SampleFormat sampleFormat = SampleFormat.UInt8)
        => SetData(data, Width, Height, Channels, sampleFormat);
    
    public void SetData(byte[] data, int width, int height, int channels, SampleFormat sampleFormat)
    {
        ArgumentNullException.ThrowIfNull(data);
        int sampleFormatSize = XisfUtil.SampleFormatSize(sampleFormat);
        long expectedSize = (long)width * height * channels * sampleFormatSize;
        if (expectedSize > int.MaxValue)
            throw new ArgumentException($"Image size exceeds maximum supported size of {int.MaxValue}.");

        // Allow creation of an XisfImage with no data, presuming it would be set later.
        if (data.Length > 0)
        {
            if (data.Length != expectedSize)
                throw new ArgumentException($"Data length {data.Length} does not match expected size {expectedSize}.");
        }

        Width = width;
        Height = height;
        Channels = channels;
        SampleFormat = sampleFormat;

        DataBlock.SetData(data);
    }
}
