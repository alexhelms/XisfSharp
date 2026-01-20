namespace XisfSharp.Tests;

[TestClass]
public class XisfImageTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_WithWidthHeight_CreatesEmptyImage()
    {
        var image = new XisfImage(100, 200);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(1);
        image.SampleFormat.ShouldBe(SampleFormat.UInt8);
        image.Data.Length.ShouldBe(0);
    }

    [TestMethod]
    public void Constructor_WithWidthHeightChannels_CreatesEmptyImage()
    {
        var image = new XisfImage(100, 200, 3);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.UInt8);
        image.Data.Length.ShouldBe(0);
    }

    [TestMethod]
    public void Constructor_WithByteArray_SingleChannel_StoresDataCorrectly()
    {
        byte[] data = new byte[100 * 200];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);

        var image = new XisfImage(data, 100, 200);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(1);
        image.SampleFormat.ShouldBe(SampleFormat.UInt8);
        image.Data.Length.ShouldBe(data.Length);
        image.Data.ToArray().ShouldBe(data);
    }

    [TestMethod]
    public void Constructor_WithByteArray_MultiChannel_StoresDataCorrectly()
    {
        byte[] data = new byte[100 * 200 * 3];
        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)(i % 256);

        var image = new XisfImage(data, 100, 200, 3, SampleFormat.UInt8);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.UInt8);
        image.Data.ToArray().ShouldBe(data);
    }

    [TestMethod]
    public void Constructor_WithUShortArray_SingleChannel_StoresDataCorrectly()
    {
        ushort[] data = new ushort[100 * 200];
        for (int i = 0; i < data.Length; i++)
            data[i] = (ushort)(i % 65536);

        var image = new XisfImage(data, 100, 200);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(1);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);
        image.Data.Length.ShouldBe(data.Length * sizeof(ushort));
    }

    [TestMethod]
    public void Constructor_WithUShortArray_MultiChannel_StoresDataCorrectly()
    {
        ushort[] data = new ushort[100 * 200 * 3];
        for (int i = 0; i < data.Length; i++)
            data[i] = (ushort)(i % 65536);

        var image = new XisfImage(data, 100, 200, 3);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);
        image.Data.Length.ShouldBe(data.Length * sizeof(ushort));
    }

    [TestMethod]
    public void Constructor_WithFloatArray_SingleChannel_StoresDataCorrectly()
    {
        float[] data = new float[100 * 200];
        for (int i = 0; i < data.Length; i++)
            data[i] = i * 0.001f;

        var image = new XisfImage(data, 100, 200);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(1);
        image.SampleFormat.ShouldBe(SampleFormat.Float32);
        image.Data.Length.ShouldBe(data.Length * sizeof(float));
    }

    [TestMethod]
    public void Constructor_WithFloatArray_MultiChannel_StoresDataCorrectly()
    {
        float[] data = new float[100 * 200 * 3];
        for (int i = 0; i < data.Length; i++)
            data[i] = i * 0.001f;

        var image = new XisfImage(data, 100, 200, 3);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.Float32);
        image.Data.Length.ShouldBe(data.Length * sizeof(float));
    }

    [TestMethod]
    public void Constructor_EmptyDimensions_CreatesZeroSizeImage()
    {
        var image = new XisfImage();
        image.Width.ShouldBe(0);
        image.Height.ShouldBe(0);
        image.Channels.ShouldBe(1);
        image.SampleFormat.ShouldBe(SampleFormat.UInt8);
        image.Data.Length.ShouldBe(0);
    }

    #endregion

    #region SetData - Basic Byte Array Tests

    [TestMethod]
    public void SetData_WithByteArray_EmptyImage_UpdatesImageProperties()
    {
        var image = new XisfImage();  // Empty
        byte[] data = new byte[100 * 200 * 3];

        image.SetData(data, 100, 200, 3, SampleFormat.UInt8);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.UInt8);
        image.Data.Length.ShouldBe(data.Length);
    }

    [TestMethod]
    public void SetData_WithByteArray_UpdatesImageProperties()
    {
        var image = new XisfImage(10, 10);
        byte[] data = new byte[100 * 200 * 3];

        image.SetData(data, 100, 200, 3, SampleFormat.UInt8);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.UInt8);
        image.Data.Length.ShouldBe(data.Length);
    }

    [TestMethod]
    public void SetData_WithEmptyByteArray_ZeroDimensions_Succeeds()
    {
        var image = new XisfImage(10, 10);
        byte[] data = [];

        image.SetData(data, 0, 0, 1, SampleFormat.UInt8);

        image.Width.ShouldBe(0);
        image.Height.ShouldBe(0);
        image.Data.Length.ShouldBe(0);
    }

    [TestMethod]
    public void SetData_WithNullByteArray_ThrowsArgumentNullException()
    {
        var image = new XisfImage(10, 10);

        Should.Throw<ArgumentNullException>(() =>
            image.SetData(null!, 100, 200, 1, SampleFormat.UInt8));
    }

    [TestMethod]
    public void SetData_WithNegativeWidth_ThrowsArgumentOutOfRangeException()
    {
        var image = new XisfImage(10, 10);
        byte[] data = new byte[100];

        Should.Throw<ArgumentOutOfRangeException>(() =>
            image.SetData(data, -1, 100, 1, SampleFormat.UInt8));
    }

    [TestMethod]
    public void SetData_WithNegativeHeight_ThrowsArgumentOutOfRangeException()
    {
        var image = new XisfImage(10, 10);
        byte[] data = new byte[100];

        Should.Throw<ArgumentOutOfRangeException>(() =>
            image.SetData(data, 100, -1, 1, SampleFormat.UInt8));
    }

    [TestMethod]
    public void SetData_WithZeroChannels_ThrowsArgumentOutOfRangeException()
    {
        var image = new XisfImage(10, 10);
        byte[] data = new byte[100];

        Should.Throw<ArgumentOutOfRangeException>(() =>
            image.SetData(data, 10, 10, 0, SampleFormat.UInt8));
    }

    [TestMethod]
    public void SetData_WithNegativeChannels_ThrowsArgumentOutOfRangeException()
    {
        var image = new XisfImage(10, 10);
        byte[] data = new byte[100];

        Should.Throw<ArgumentOutOfRangeException>(() =>
            image.SetData(data, 10, 10, -1, SampleFormat.UInt8));
    }

    [TestMethod]
    public void SetData_WithMismatchedDataLength_ThrowsArgumentException()
    {
        var image = new XisfImage(10, 10);
        byte[] data = new byte[100];

        Should.Throw<ArgumentException>(() =>
            image.SetData(data, 100, 200, 1, SampleFormat.UInt8));
    }

    [TestMethod]
    public void SetData_WithDifferentSampleFormats_UpdatesCorrectly()
    {
        var image = new XisfImage(10, 10);

        // UInt16
        byte[] data16 = new byte[100 * 200 * sizeof(ushort)];
        image.SetData(data16, 100, 200, 1, SampleFormat.UInt16);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);

        // Float32
        byte[] data32 = new byte[100 * 200 * sizeof(float)];
        image.SetData(data32, 100, 200, 1, SampleFormat.Float32);
        image.SampleFormat.ShouldBe(SampleFormat.Float32);

        // Float64
        byte[] data64 = new byte[100 * 200 * sizeof(double)];
        image.SetData(data64, 100, 200, 1, SampleFormat.Float64);
        image.SampleFormat.ShouldBe(SampleFormat.Float64);
    }

    #endregion

    #region SetData - Generic Span/Array Tests

    [TestMethod]
    public void SetData_WithGenericSpan_UShort_StoresCorrectly()
    {
        var image = new XisfImage(10, 10);
        ushort[] data = new ushort[100 * 200 * 3];
        for (int i = 0; i < data.Length; i++)
            data[i] = (ushort)i;

        image.SetData(data.AsSpan(), 100, 200, 3);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);
        image.Data.Length.ShouldBe(data.Length * sizeof(ushort));
    }

    [TestMethod]
    public void SetData_WithGenericSpan_Float_StoresCorrectly()
    {
        var image = new XisfImage(10, 10);
        float[] data = new float[100 * 200];
        for (int i = 0; i < data.Length; i++)
            data[i] = i * 0.5f;

        image.SetData(data.AsSpan(), 100, 200, 1);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(1);
        image.SampleFormat.ShouldBe(SampleFormat.Float32);
        image.Data.Length.ShouldBe(data.Length * sizeof(float));
    }

    [TestMethod]
    public void SetData_WithGenericSpan_ExistingDimensions_UpdatesData()
    {
        var image = new XisfImage(10, 10, 2);
        ushort[] data = new ushort[10 * 10 * 2];
        for (int i = 0; i < data.Length; i++)
            data[i] = (ushort)(i * 100);

        image.SetData(data.AsSpan());

        image.Width.ShouldBe(10);
        image.Height.ShouldBe(10);
        image.Channels.ShouldBe(2);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);
    }

    [TestMethod]
    public void SetData_WithGenericArray_StoresCorrectly()
    {
        var image = new XisfImage(10, 10);
        ushort[] data = new ushort[50 * 50 * 3];

        image.SetData(data, 50, 50, 3);

        image.Width.ShouldBe(50);
        image.Height.ShouldBe(50);
        image.Channels.ShouldBe(3);
    }

    [TestMethod]
    public void SetData_WithGenericArray_ExistingDimensions_UpdatesData()
    {
        var image = new XisfImage(10, 10, 1);
        byte[] data = new byte[10 * 10];

        image.SetData(data);

        image.Width.ShouldBe(10);
        image.Height.ShouldBe(10);
        image.Channels.ShouldBe(1);
    }

    #endregion

    #region SetData - SpanAction Delegate Tests

    [TestMethod]
    public void SetData_WithSpanAction_PopulatesDataCorrectly()
    {
        var image = new XisfImage(10, 10);

        image.SetData<ushort>(100, 200, 1, span =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = (ushort)(i % 65536);
        });

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(1);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);
    }

    [TestMethod]
    public void SetData_WithSpanAction_MultiChannel_PopulatesCorrectly()
    {
        var image = new XisfImage(10, 10);

        image.SetData<float>(50, 50, 3, span =>
        {
            for (int i = 0; i < span.Length; i++)
                span[i] = i * 0.001f;
        });

        image.Width.ShouldBe(50);
        image.Height.ShouldBe(50);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.Float32);
        image.Data.Length.ShouldBe(50 * 50 * 3 * sizeof(float));
    }

    [TestMethod]
    public void SetData_WithNullSpanAction_ThrowsArgumentNullException()
    {
        var image = new XisfImage(10, 10);

        Should.Throw<ArgumentNullException>(() =>
            image.SetData<ushort>(100, 200, 1, null!));
    }

    [TestMethod]
    public void SetData_WithSpanAction_ZeroWidth_ThrowsArgumentOutOfRangeException()
    {
        var image = new XisfImage(10, 10);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            image.SetData<ushort>(0, 200, 1, span => { }));
    }

    [TestMethod]
    public void SetData_WithSpanAction_ZeroHeight_ThrowsArgumentOutOfRangeException()
    {
        var image = new XisfImage(10, 10);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            image.SetData<ushort>(100, 0, 1, span => { }));
    }

    [TestMethod]
    public void SetData_WithSpanAction_ZeroChannels_ThrowsArgumentOutOfRangeException()
    {
        var image = new XisfImage(10, 10);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            image.SetData<ushort>(100, 200, 0, span => { }));
    }

    #endregion

    #region SetData - Separate Channel Arrays Tests

    [TestMethod]
    public void SetData_WithSeparateChannelArrays_SingleChannel_StoresCorrectly()
    {
        var image = new XisfImage(10, 10);
        ushort[] channel = new ushort[100 * 200];
        for (int i = 0; i < channel.Length; i++)
            channel[i] = (ushort)i;

        image.SetData(100, 200, channel);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(1);
    }

    [TestMethod]
    public void SetData_WithSeparateChannelArrays_ThreeChannels_StoresCorrectly()
    {
        var image = new XisfImage(10, 10);
        int pixelCount = 100 * 200;

        ushort[] r = new ushort[pixelCount];
        ushort[] g = new ushort[pixelCount];
        ushort[] b = new ushort[pixelCount];

        for (int i = 0; i < pixelCount; i++)
        {
            r[i] = (ushort)(i * 1);
            g[i] = (ushort)(i * 2);
            b[i] = (ushort)(i * 3);
        }

        image.SetData(100, 200, r, g, b);

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);
    }

    [TestMethod]
    public void SetData_WithSeparateChannelArrays_NoChannels_ThrowsArgumentException()
    {
        var image = new XisfImage(10, 10);

        Should.Throw<ArgumentException>(() =>
            image.SetData<ushort>(100, 200));
    }

    [TestMethod]
    public void SetData_WithSeparateChannelArrays_MismatchedLength_ThrowsArgumentException()
    {
        var image = new XisfImage(10, 10);
        ushort[] channel1 = new ushort[100 * 200];
        ushort[] channel2 = new ushort[100 * 100]; // Wrong size

        Should.Throw<ArgumentException>(() =>
            image.SetData(100, 200, channel1, channel2));
    }

    #endregion

    #region SetData - Three Channel Span Tests

    [TestMethod]
    public void SetData_WithThreeChannelSpans_StoresCorrectly()
    {
        var image = new XisfImage(10, 10);
        int pixelCount = 100 * 200;

        float[] r = new float[pixelCount];
        float[] g = new float[pixelCount];
        float[] b = new float[pixelCount];

        for (int i = 0; i < pixelCount; i++)
        {
            r[i] = i * 0.001f;
            g[i] = i * 0.002f;
            b[i] = i * 0.003f;
        }

        image.SetData(100, 200, r.AsSpan(), g.AsSpan(), b.AsSpan());

        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.Float32);
        image.Data.Length.ShouldBe(pixelCount * 3 * sizeof(float));
    }

    [TestMethod]
    public void SetData_WithThreeChannelSpans_VerifiesPlanarLayout()
    {
        var image = new XisfImage(10, 10);
        int width = 10;
        int height = 10;
        int pixelCount = width * height;

        ushort[] r = new ushort[pixelCount];
        ushort[] g = new ushort[pixelCount];
        ushort[] b = new ushort[pixelCount];

        // Fill with distinct values
        for (int i = 0; i < pixelCount; i++)
        {
            r[i] = (ushort)(100 + i);
            g[i] = (ushort)(200 + i);
            b[i] = (ushort)(300 + i);
        }

        image.SetData(width, height, r.AsSpan(), g.AsSpan(), b.AsSpan());

        // Verify data is stored in planar format
        var dataSpan = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, ushort>(image.Data.Span);

        for (int i = 0; i < pixelCount; i++)
        {
            dataSpan[i].ShouldBe(r[i], $"R channel mismatch at {i}");
            dataSpan[pixelCount + i].ShouldBe(g[i], $"G channel mismatch at {i}");
            dataSpan[2 * pixelCount + i].ShouldBe(b[i], $"B channel mismatch at {i}");
        }
    }

    [TestMethod]
    public void SetData_WithThreeChannelSpans_Channel0WrongSize_ThrowsArgumentException()
    {
        var image = new XisfImage(10, 10);
        ushort[] r = new ushort[100];
        ushort[] g = new ushort[100 * 200];
        ushort[] b = new ushort[100 * 200];

        Should.Throw<ArgumentException>(() =>
            image.SetData(100, 200, r.AsSpan(), g.AsSpan(), b.AsSpan()));
    }

    [TestMethod]
    public void SetData_WithThreeChannelSpans_Channel1WrongSize_ThrowsArgumentException()
    {
        var image = new XisfImage(10, 10);
        ushort[] r = new ushort[100 * 200];
        ushort[] g = new ushort[100];
        ushort[] b = new ushort[100 * 200];

        Should.Throw<ArgumentException>(() =>
            image.SetData(100, 200, r.AsSpan(), g.AsSpan(), b.AsSpan()));
    }

    [TestMethod]
    public void SetData_WithThreeChannelSpans_Channel2WrongSize_ThrowsArgumentException()
    {
        var image = new XisfImage(10, 10);
        ushort[] r = new ushort[100 * 200];
        ushort[] g = new ushort[100 * 200];
        ushort[] b = new ushort[100];

        Should.Throw<ArgumentException>(() =>
            image.SetData(100, 200, r.AsSpan(), g.AsSpan(), b.AsSpan()));
    }

    #endregion

    #region Edge Cases and Special Scenarios

    [TestMethod]
    public void SetData_SinglePixelImage_StoresCorrectly()
    {
        var image = new XisfImage(10, 10);
        ushort[] data = [42];

        image.SetData(data, 1, 1, 1);

        image.Width.ShouldBe(1);
        image.Height.ShouldBe(1);
        image.Channels.ShouldBe(1);
    }

    [TestMethod]
    public void SetData_VeryLargeImage_WithinLimits_Succeeds()
    {
        var image = new XisfImage(10, 10);
        // Create a large but valid image (e.g., 8K resolution, 1 channel, UInt16)
        int width = 7680;
        int height = 4320;
        int channels = 1;
        int bytesPerSample = 2; // UInt16
        
        long expectedSize = (long)width * height * channels * bytesPerSample;

        byte[] data = new byte[expectedSize];
        image.SetData(data, width, height, channels, SampleFormat.UInt16);

        image.Width.ShouldBe(width);
        image.Height.ShouldBe(height);
    }

    [TestMethod]
    public void SetData_ImageSizeExceedsMaxValue_ThrowsArgumentException()
    {
        var image = new XisfImage(10, 10);
        // Try to create an image that would exceed int.MaxValue bytes
        int width = int.MaxValue / 2;
        int height = 3; // This would overflow

        byte[] data = new byte[100]; // Dummy data

        Should.Throw<ArgumentException>(() =>
            image.SetData(data, width, height, 1, SampleFormat.UInt8));
    }

    [TestMethod]
    public void SetData_WithSpanAction_ImageSizeExceedsMaxValue_ThrowsArgumentException()
    {
        var image = new XisfImage(10, 10);
        int width = int.MaxValue / 2;
        int height = 3;

        Should.Throw<ArgumentException>(() =>
            image.SetData<byte>(width, height, 1, span => { }));
    }

    [TestMethod]
    public void SetData_WithThreeChannels_ImageSizeExceedsMaxValue_ThrowsArgumentException()
    {
        var image = new XisfImage(10, 10);
        int width = int.MaxValue / 4;
        int height = 2;

        float[] r = new float[100];
        float[] g = new float[100];
        float[] b = new float[100];

        Should.Throw<ArgumentException>(() => image.SetData(width, height, r, g, b));
    }

    [TestMethod]
    public void SetData_MultipleUpdates_LastUpdateWins()
    {
        var image = new XisfImage(10, 10);

        // First update
        byte[] data1 = new byte[50 * 50];
        image.SetData(data1, 50, 50, 1, SampleFormat.UInt8);
        image.Width.ShouldBe(50);
        image.SampleFormat.ShouldBe(SampleFormat.UInt8);

        // Second update
        ushort[] data2 = new ushort[100 * 200 * 3];
        image.SetData(data2, 100, 200, 3);
        image.Width.ShouldBe(100);
        image.Height.ShouldBe(200);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);
    }

    [TestMethod]
    public void SetData_DifferentDataTypes_InfersCorrectSampleFormat()
    {
        var image = new XisfImage(10, 10);

        // byte -> UInt8
        byte[] byteData = new byte[100];
        image.SetData(byteData, 10, 10, 1);
        image.SampleFormat.ShouldBe(SampleFormat.UInt8);

        // ushort -> UInt16
        ushort[] ushortData = new ushort[100];
        image.SetData(ushortData, 10, 10, 1);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);

        // float -> Float32
        float[] floatData = new float[100];
        image.SetData(floatData, 10, 10, 1);
        image.SampleFormat.ShouldBe(SampleFormat.Float32);

        // double -> Float64
        double[] doubleData = new double[100];
        image.SetData(doubleData, 10, 10, 1);
        image.SampleFormat.ShouldBe(SampleFormat.Float64);
    }

    [TestMethod]
    public void SetData_CommonScenario_MonochromeUInt16()
    {
        // Typical astronomical image: 1920x1080, single channel, 16-bit
        var image = new XisfImage(10, 10);
        int width = 1920;
        int height = 1080;
        ushort[] data = new ushort[width * height];

        for (int i = 0; i < data.Length; i++)
            data[i] = (ushort)(i % 65536);

        image.SetData(data, width, height, 1);

        image.Width.ShouldBe(width);
        image.Height.ShouldBe(height);
        image.Channels.ShouldBe(1);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);
    }

    [TestMethod]
    public void SetData_CommonScenario_RGBFloat32_SeparatePlanes()
    {
        // Typical RGB image with separate planes
        var image = new XisfImage(10, 10);
        int width = 1920;
        int height = 1080;
        int pixelCount = width * height;

        float[] r = new float[pixelCount];
        float[] g = new float[pixelCount];
        float[] b = new float[pixelCount];

        for (int i = 0; i < pixelCount; i++)
        {
            r[i] = (float)i / pixelCount;
            g[i] = (float)i / pixelCount * 0.5f;
            b[i] = (float)i / pixelCount * 0.25f;
        }

        image.SetData(width, height, r.AsSpan(), g.AsSpan(), b.AsSpan());

        image.Width.ShouldBe(width);
        image.Height.ShouldBe(height);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.Float32);
        image.PixelStorage.ShouldBe(PixelStorage.Planar);
    }

    [TestMethod]
    public void SetData_CommonScenario_RGBUInt16_SingleArray()
    {
        // RGB image in a single planar array
        var image = new XisfImage(10, 10);
        int width = 1920;
        int height = 1080;
        ushort[] data = new ushort[width * height * 3];

        image.SetData(data, width, height, 3);

        image.Width.ShouldBe(width);
        image.Height.ShouldBe(height);
        image.Channels.ShouldBe(3);
        image.SampleFormat.ShouldBe(SampleFormat.UInt16);
    }

    #endregion

    #region Floating Point Bounds

    [TestMethod]
    public async Task Write_Float32Image_NoBounds_UsesOptionsDefaults()
    {
        int width = 10;
        int height = 10;
        float[] data = new float[width * height];

        byte[] fileBytes;
        {
            var image = new XisfImage(data, width, height, 1);
            // Don't set bounds

            var options = new XisfWriterOptions
            {
                FloatingPointLowerBound = 0.0,
                FloatingPointUpperBound = 1.0
            };

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream, options);
            writer.AddImage(image);
            await writer.SaveAsync();

            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);
            await reader.ReadHeaderAsync();

            var image = await reader.ReadImageAsync(0);
            image.Bounds.ShouldNotBeNull();
            image.Bounds.Value.Lower.ShouldBe(0.0);
            image.Bounds.Value.Upper.ShouldBe(1.0);
        }
    }

    [TestMethod]
    public async Task Write_Float64Image_NoBounds_UsesOptionsDefaults()
    {
        int width = 10;
        int height = 10;
        double[] data = new double[width * height];

        byte[] fileBytes;
        {
            var image = new XisfImage(data, width, height, 1);
            // Don't set bounds

            var options = new XisfWriterOptions
            {
                FloatingPointLowerBound = -1.0,
                FloatingPointUpperBound = 2.0
            };

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream, options);
            writer.AddImage(image);
            await writer.SaveAsync();

            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);
            await reader.ReadHeaderAsync();

            var image = await reader.ReadImageAsync(0);
            image.Bounds.ShouldNotBeNull();
            image.Bounds.Value.Lower.ShouldBe(-1.0);
            image.Bounds.Value.Upper.ShouldBe(2.0);
        }
    }

    [TestMethod]
    public async Task Write_Float32Image_WithBounds_UsesImageBounds()
    {
        int width = 10;
        int height = 10;
        float[] data = new float[width * height];

        byte[] fileBytes;
        {
            var image = new XisfImage(data, width, height, 1);
            image.Bounds = new SampleBounds(0.5, 0.9);

            var options = new XisfWriterOptions
            {
                FloatingPointLowerBound = 0.0,
                FloatingPointUpperBound = 1.0
            };

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream, options);
            writer.AddImage(image);
            await writer.SaveAsync();

            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);
            await reader.ReadHeaderAsync();

            var image = await reader.ReadImageAsync(0);
            image.Bounds.ShouldNotBeNull();
            image.Bounds.Value.Lower.ShouldBe(0.5);
            image.Bounds.Value.Upper.ShouldBe(0.9);
        }
    }

    [TestMethod]
    public async Task Write_Float32Image_InvertedOptionBounds_SwapsValues()
    {
        int width = 10;
        int height = 10;
        float[] data = new float[width * height];

        byte[] fileBytes;
        {
            var image = new XisfImage(data, width, height, 1);
            // Don't set bounds

            var options = new XisfWriterOptions
            {
                FloatingPointLowerBound = 1.0,  // Inverted
                FloatingPointUpperBound = 0.0
            };

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream, options);
            writer.AddImage(image);
            await writer.SaveAsync();

            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);
            await reader.ReadHeaderAsync();

            var image = await reader.ReadImageAsync(0);
            image.Bounds.ShouldNotBeNull();
            image.Bounds.Value.Lower.ShouldBe(0.0);  // Should be swapped
            image.Bounds.Value.Upper.ShouldBe(1.0);
        }
    }

    [TestMethod]
    public async Task Write_UInt8Image_NoBounds_BoundsIsNull()
    {
        int width = 10;
        int height = 10;
        byte[] data = new byte[width * height];

        byte[] fileBytes;
        {
            var image = new XisfImage(data, width, height, 1, SampleFormat.UInt8);
            // Don't set bounds

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream);
            writer.AddImage(image);
            await writer.SaveAsync();

            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);
            await reader.ReadHeaderAsync();

            var image = await reader.ReadImageAsync(0);
            image.Bounds.ShouldBeNull();
        }
    }

    [TestMethod]
    public async Task Write_UInt16Image_NoBounds_BoundsIsNull()
    {
        int width = 10;
        int height = 10;
        ushort[] data = new ushort[width * height];

        byte[] fileBytes;
        {
            var image = new XisfImage(data, width, height, 1);
            // Don't set bounds

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream);
            writer.AddImage(image);
            await writer.SaveAsync();

            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);
            await reader.ReadHeaderAsync();

            var image = await reader.ReadImageAsync(0);
            image.Bounds.ShouldBeNull();
        }
    }

    [TestMethod]
    public async Task Write_UInt8Image_WithBounds_BoundsPersists()
    {
        int width = 10;
        int height = 10;
        byte[] data = new byte[width * height];

        byte[] fileBytes;
        {
            var image = new XisfImage(data, width, height, 1, SampleFormat.UInt8);
            image.Bounds = new SampleBounds(0, 255);

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream);
            writer.AddImage(image);
            await writer.SaveAsync();

            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);
            await reader.ReadHeaderAsync();

            var image = await reader.ReadImageAsync(0);
            image.Bounds.ShouldNotBeNull();
            image.Bounds.Value.Lower.ShouldBe(0);
            image.Bounds.Value.Upper.ShouldBe(255);
        }
    }

    [TestMethod]
    public async Task Write_UInt16Image_WithBounds_BoundsPersists()
    {
        int width = 10;
        int height = 10;
        ushort[] data = new ushort[width * height];

        byte[] fileBytes;
        {
            var image = new XisfImage(data, width, height, 1);
            image.Bounds = new SampleBounds(100, 60000);

            await using var stream = new MemoryStream();
            await using var writer = new XisfWriter(stream);
            writer.AddImage(image);
            await writer.SaveAsync();

            fileBytes = stream.ToArray();
        }

        {
            await using var inputStream = new MemoryStream(fileBytes);
            await using var reader = new XisfReader(inputStream);
            await reader.ReadHeaderAsync();

            var image = await reader.ReadImageAsync(0);
            image.Bounds.ShouldNotBeNull();
            image.Bounds.Value.Lower.ShouldBe(100);
            image.Bounds.Value.Upper.ShouldBe(60000);
        }
    }

    #endregion
}
