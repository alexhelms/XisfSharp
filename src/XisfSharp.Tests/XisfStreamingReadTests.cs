using System.Runtime.InteropServices;
using XisfSharp.IO;
using static XisfSharp.Tests.XisfBlockTestData;

namespace XisfSharp.Tests;

[TestClass]
public class XisfStreamingReadTests
{
    private static async Task<MemoryStream> WriteImage(XisfImage image, XisfWriterOptions options)
    {
        var output = new MemoryStream();
        await using (var writer = new XisfWriter(output, options, leaveOpen: true))
        {
            writer.AddImage(image);
            await writer.SaveAsync();
        }

        output.Position = 0;
        return output;
    }

    private static float[] CreatePlanarSamples(int width, int height, int channels)
    {
        return Enumerable.Range(0, width * height * channels).Select(i => i * 0.5f).ToArray();
    }

    [TestMethod]
    [DataRow(64)]
    [DataRow(160)]
    [DataRow(1024 * 1024)]
    public async Task ReadImageRowsAsync_Planar_DeliversEveryRowOfEachChannelInOrder(int chunkSize)
    {
        const int width = 13, height = 7, channels = 3;
        float[] samples = CreatePlanarSamples(width, height, channels);

        var image = new XisfImage();
        image.SetData(samples, width, height, channels);
        var unit = await WriteImage(image, new XisfWriterOptions { CompressionCodec = CompressionCodec.Zstd, ShuffleBytes = true });

        await using var reader = new XisfReader(unit) { DecodeOptions = new BlockDecodeOptions { ChunkSize = chunkSize } };

        var rows = new List<(int Channel, int Y, float[] Samples)>();
        await reader.ReadImageRowsAsync<float>(0, (channel, y, row) => rows.Add((channel, y, row.ToArray())));

        rows.Select(r => (r.Channel, r.Y)).ShouldBe(
            Enumerable.Range(0, channels).SelectMany(c => Enumerable.Range(0, height).Select(y => (c, y))));
        rows.SelectMany(r => r.Samples).ToArray().ShouldBe(samples);
    }

    [TestMethod]
    public async Task ReadImageRowsAsync_NormalStorage_DeliversInterleavedRowsAsChannelZero()
    {
        const int width = 5, height = 4, channels = 3;
        byte[] data = CreateRamp(SampleFormat.UInt16, width * height * channels);

        var unit = CreateUnit(
            $"""<Image geometry="{width}:{height}:{channels}" sampleFormat="UInt16" colorSpace="RGB" pixelStorage="Normal" location="attachment:{AttachmentPosition}:{data.Length}"/>""",
            data);

        await using var reader = new XisfReader(unit);

        var rows = new List<(int Channel, int Y, ushort[] Samples)>();
        await reader.ReadImageRowsAsync<ushort>(0, (channel, y, row) => rows.Add((channel, y, row.ToArray())));

        rows.Count.ShouldBe(height);
        rows.ShouldAllBe(r => r.Channel == 0 && r.Samples.Length == width * channels);
        rows.Select(r => r.Y).ShouldBe(Enumerable.Range(0, height));
        rows.SelectMany(r => r.Samples).ToArray().ShouldBe(MemoryMarshal.Cast<byte, ushort>(data).ToArray());
    }

    [TestMethod]
    public async Task ReadImageRowsAsync_SampleTypeMismatch_ThrowsBeforeReadingRows()
    {
        var image = new XisfImage();
        image.SetData(CreatePlanarSamples(4, 4, 1), 4, 4, 1);
        await using var reader = new XisfReader(await WriteImage(image, new XisfWriterOptions()));

        int calls = 0;
        await Should.ThrowAsync<ArgumentException>(() => reader.ReadImageRowsAsync<ushort>(0, (_, _, _) => calls++));

        calls.ShouldBe(0);
    }

    [TestMethod]
    public async Task ReadImageRowsAsync_CallbackThrows_PropagatesException()
    {
        var image = new XisfImage();
        image.SetData(CreatePlanarSamples(4, 4, 1), 4, 4, 1);
        await using var reader = new XisfReader(await WriteImage(image, new XisfWriterOptions()));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            reader.ReadImageRowsAsync<float>(0, (_, y, _) =>
            {
                if (y == 2)
                    throw new InvalidOperationException();
            }));
    }

    [TestMethod]
    [DataRow(CompressionCodec.None)]
    [DataRow(CompressionCodec.Lz4)]
    [DataRow(CompressionCodec.Zlib)]
    public async Task OpenImageDataAsync_MatchesReadImageAsync(CompressionCodec codec)
    {
        var image = new XisfImage();
        image.SetData(CreatePlanarSamples(32, 16, 2), 32, 16, 2);
        var options = new XisfWriterOptions { CompressionCodec = codec, ShuffleBytes = codec != CompressionCodec.None };
        byte[] unit = (await WriteImage(image, options)).ToArray();

        byte[] expected;
        await using (var reader = new XisfReader(new MemoryStream(unit)))
        {
            expected = (await reader.ReadImageAsync(0)).Data.ToArray();
        }

        await using (var reader = new XisfReader(new MemoryStream(unit)))
        {
            await using var data = await reader.OpenImageDataAsync(0);
            data.Length.ShouldBe(expected.Length);

            using var copy = new MemoryStream();
            await data.CopyToAsync(copy);
            copy.ToArray().ShouldBe(expected);
        }
    }

    [TestMethod]
    public async Task OpenImageDataAsync_AfterImageLoaded_ServesLoadedData()
    {
        var image = new XisfImage();
        image.SetData(CreatePlanarSamples(8, 8, 1), 8, 8, 1);
        var unit = await WriteImage(image, new XisfWriterOptions { CompressionCodec = CompressionCodec.Zstd });

        await using var reader = new XisfReader(unit);
        var loaded = await reader.ReadImageAsync(0);

        await using var data = await reader.OpenImageDataAsync(0);
        byte[] streamed = new byte[data.Length];
        await data.ReadExactlyAsync(streamed);

        streamed.ShouldBe(loaded.Data.ToArray());
    }

    [TestMethod]
    public async Task OpenImageDataAsync_DataSizeDoesNotMatchGeometry_Throws()
    {
        byte[] data = new byte[100];
        var unit = CreateUnit(
            $"""<Image geometry="8:8:1" sampleFormat="UInt16" colorSpace="Gray" location="attachment:{AttachmentPosition}:{data.Length}"/>""",
            data);

        await using var reader = new XisfReader(unit);

        await Should.ThrowAsync<XisfException>(() => reader.OpenImageDataAsync(0));
    }

    [TestMethod]
    public async Task ReadImageAsync_ImageRequiresStreaming_PointsToStreamingApi()
    {
        const long size = 61424L * 33914L * sizeof(float);
        var unit = CreateUnit(
            $"""<Image geometry="61424:33914:1" sampleFormat="Float32" bounds="0:1" colorSpace="Gray" location="attachment:{AttachmentPosition}:{size}"/>""");

        await using var reader = new XisfReader(unit);

        var exception = await Should.ThrowAsync<XisfException>(() => reader.ReadImageAsync(0));
        exception.Message.ShouldContain(nameof(XisfReader.ReadImageRowsAsync));
    }

    [TestMethod]
    public void DecodeMemoryLimit_Negative_Throws()
    {
        using var reader = new XisfReader(new MemoryStream());

        Should.Throw<ArgumentOutOfRangeException>(() => reader.DecodeMemoryLimit = -1);
    }

    [TestMethod]
    public void DecodeSettings_AreAppliedToDecodeOptions()
    {
        using var reader = new XisfReader(new MemoryStream())
        {
            DecodeMemoryLimit = 1234,
            TempDirectory = "scratch",
        };

        reader.DecodeOptions.LaneMemoryLimit.ShouldBe(1234);
        reader.DecodeOptions.TempDirectory.ShouldBe("scratch");
    }
}
