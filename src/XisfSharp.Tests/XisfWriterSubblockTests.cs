using System.Buffers.Binary;
using System.IO.Compression;
using System.Xml.Linq;
using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class XisfWriterSubblockTests
{
    private static float[] CreateSamples(int count) => Enumerable.Range(0, count).Select(i => MathF.Sin(i * 0.01f)).ToArray();

    private static string? ImageAttribute(string xml, string name)
    {
        XNamespace ns = XisfBlockTestData.Namespace;
        return XDocument.Parse(xml).Root!.Element(ns + "Image")!.Attribute(name)?.Value;
    }

    [TestMethod]
    [DataRow(CompressionCodec.Zlib, false)]
    [DataRow(CompressionCodec.Zlib, true)]
    [DataRow(CompressionCodec.Lz4, false)]
    [DataRow(CompressionCodec.Lz4, true)]
    [DataRow(CompressionCodec.Lz4Hc, true)]
    [DataRow(CompressionCodec.Zstd, false)]
    [DataRow(CompressionCodec.Zstd, true)]
    public async Task Write_WithSubblockSize_RoundTrips(CompressionCodec codec, bool shuffle)
    {
        float[] samples = CreateSamples(64 * 48);
        var image = new XisfImage();
        image.SetData(samples, 64, 48, 1);

        // 5000 is not a multiple of the item size, so shuffled planes span subblock boundaries.
        var options = new XisfWriterOptions { CompressionCodec = codec, ShuffleBytes = shuffle, SubblockSize = 5000 };
        var (result, xml) = await TestHelpers.WriteAndReadImageWithXml(image, options);

        result.Data.ToArray().ShouldBe(image.Data.ToArray());

        var subblocks = SubblockTable.Parse(ImageAttribute(xml, "subblocks")!);
        subblocks.Count.ShouldBe((int)Math.Ceiling(samples.Length * sizeof(float) / 5000.0));
        subblocks.UncompressedSize.ShouldBe(samples.Length * sizeof(float));
        subblocks.Subblocks.Take(subblocks.Count - 1).ShouldAllBe(s => s.UncompressedSize == 5000);
    }

    [TestMethod]
    public async Task Write_WithoutSubblockSize_OmitsSubblocksAttribute()
    {
        var image = new XisfImage();
        image.SetData(CreateSamples(64 * 48), 64, 48, 1);

        var options = new XisfWriterOptions { CompressionCodec = CompressionCodec.Zstd, ShuffleBytes = true };
        var (_, xml) = await TestHelpers.WriteAndReadImageWithXml(image, options);

        ImageAttribute(xml, "compression").ShouldNotBeNull();
        ImageAttribute(xml, "subblocks").ShouldBeNull();
    }

    [TestMethod]
    [DataRow(CompressionCodec.Zlib)]
    [DataRow(CompressionCodec.Lz4)]
    [DataRow(CompressionCodec.Zstd)]
    public async Task Write_IncompressibleSubblocks_AreStoredVerbatim(CompressionCodec codec)
    {
        byte[] noise = new byte[20000];
        new Random(42).NextBytes(noise);
        var image = new XisfImage(noise, 200, 100);

        var options = new XisfWriterOptions { CompressionCodec = codec, SubblockSize = 4096 };
        var (result, xml) = await TestHelpers.WriteAndReadImageWithXml(image, options);

        result.Data.ToArray().ShouldBe(noise);
        SubblockTable.Parse(ImageAttribute(xml, "subblocks")!).Subblocks.ShouldAllBe(s => s.IsStored);
    }

    [TestMethod]
    public void CompressData_SubblockSize_ProducesSubblocksThatAddUp()
    {
        byte[] data = XisfBlockTestData.CreateRamp(SampleFormat.UInt16, 10000);
        var block = new OutputBlock(data);

        block.CompressData(CompressionCodec.Zstd, sizeof(ushort), shuffleBytes: true, subblockSize: 3000);

        block.Subblocks.ShouldNotBeNull();
        block.Subblocks.Count.ShouldBe(7);
        block.Subblocks.Sum(s => s.Uncompressed).ShouldBe(data.Length);
        block.Subblocks.Sum(s => s.Compressed).ShouldBe(block.BlockSize);
        block.GetSubblocksAttribute().ShouldBe(string.Join(':', block.Subblocks.Select(s => $"{s.Compressed},{s.Uncompressed}")));
    }

    [TestMethod]
    public void CompressData_SingleSubblock_HasNoSubblocksAttribute()
    {
        var block = new OutputBlock(XisfBlockTestData.CreateRamp(SampleFormat.UInt16, 10000));

        block.CompressData(CompressionCodec.Zstd, sizeof(ushort), shuffleBytes: true);

        block.Subblocks.ShouldBeNull();
        block.GetSubblocksAttribute().ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(OutputBlock.MaxSubblockSize + 1)]
    public void CompressData_InvalidSubblockSize_Throws(int subblockSize)
    {
        var block = new OutputBlock(new byte[100]);

        Should.Throw<ArgumentOutOfRangeException>(() => block.CompressData(CompressionCodec.Zstd, 1, false, subblockSize));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(OutputBlock.MaxSubblockSize + 1)]
    public void WriterOptions_InvalidSubblockSize_Throws(int subblockSize)
    {
        var options = new XisfWriterOptions();

        Should.Throw<ArgumentOutOfRangeException>(() => options.SubblockSize = subblockSize);
    }

    [TestMethod]
    public void Compress_Zlib_WritesCompleteStreamWithAdler32Trailer()
    {
        byte[] data = XisfBlockTestData.CreateRamp(SampleFormat.Float32, 5000);

        byte[] compressed = XisfUtil.Compress(data, CompressionCodec.Zlib);

        uint expectedAdler = Adler32(data);
        BinaryPrimitives.ReadUInt32BigEndian(compressed.AsSpan(compressed.Length - 4)).ShouldBe(expectedAdler);

        using var zlib = new ZLibStream(new MemoryStream(compressed), CompressionMode.Decompress);
        using var decompressed = new MemoryStream();
        zlib.CopyTo(decompressed);
        decompressed.ToArray().ShouldBe(data);
    }

    private static uint Adler32(ReadOnlySpan<byte> data)
    {
        const uint Modulus = 65521;
        uint a = 1, b = 0;
        foreach (byte value in data)
        {
            a = (a + value) % Modulus;
            b = (b + a) % Modulus;
        }
        return (b << 16) | a;
    }
}
