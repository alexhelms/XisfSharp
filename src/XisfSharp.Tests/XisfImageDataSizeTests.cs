namespace XisfSharp.Tests;

[TestClass]
public class XisfImageDataSizeTests
{
    [TestMethod]
    public void DataSize_ComputedFromGeometryAndSampleFormat()
    {
        var image = new XisfImage();
        image.SetData<ushort>(40, 30, 3, _ => { });

        image.DataSize.ShouldBe(40L * 30 * 3 * sizeof(ushort));
        image.RequiresStreaming.ShouldBeFalse();
    }

    [TestMethod]
    public void SetData_ChannelArrays_MismatchedLength_ReportsExpectedLength()
    {
        var image = new XisfImage();
        float[] channel0 = new float[20];
        float[] channel1 = new float[19];

        var exception = Should.Throw<ArgumentException>(() => image.SetData(5, 4, channel0, channel1));

        exception.Message.ShouldContain("Channel 1");
        exception.Message.ShouldContain("20");
    }

    [TestMethod]
    public void SetData_ChannelArrays_DimensionsOverflowingInt32_RejectsChannelLength()
    {
        var image = new XisfImage();

        // 65536 * 65537 wraps to 65536 in 32-bit arithmetic, which would match this channel's length.
        float[] channel = new float[65536];

        var exception = Should.Throw<ArgumentException>(() => image.SetData(65536, 65537, channel));
        exception.Message.ShouldContain("Channel 0");
    }

    [TestMethod]
    public void SetData_ThreeChannelSpans_DimensionsOverflowingInt32_RejectsChannelLength()
    {
        var image = new XisfImage();

        // 65536 * 65537 wraps to 65536 in 32-bit arithmetic, which would match these channels' length.
        float[] channel = new float[65536];

        var exception = Should.Throw<ArgumentException>(() =>
            image.SetData<float>(65536, 65537, channel.AsSpan(), channel.AsSpan(), channel.AsSpan()));
        exception.Message.ShouldContain("Channel 0");
    }

    [TestMethod]
    public void SetData_ChannelArrays_CopiesChannelsInPlanarOrder()
    {
        var image = new XisfImage();
        float[] channel0 = [1, 2, 3, 4, 5, 6];
        float[] channel1 = [7, 8, 9, 10, 11, 12];

        image.SetData(3, 2, channel0, channel1);

        var samples = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(image.Data.Span).ToArray();
        samples.ShouldBe(new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
    }
}
