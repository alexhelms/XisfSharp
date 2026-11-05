using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class XisfStreamingWriteTests
{
    private const int Width = 37;
    private const int Height = 23;
    private const int Channels = 3;

    private static float Sample(int channel, int y, int x) => MathF.Sin(channel * 10 + y * 0.3f + x * 0.07f);

    private static void FillRow(int channel, int y, Span<float> row)
    {
        for (int x = 0; x < row.Length; x++)
            row[x] = Sample(channel, y, x);
    }

    private static byte[] ExpectedPlanarData()
    {
        var samples = new float[Width * Height * Channels];
        for (int c = 0; c < Channels; c++)
            for (int y = 0; y < Height; y++)
                FillRow(c, y, samples.AsSpan((c * Height + y) * Width, Width));

        return MemoryMarshal.AsBytes(samples.AsSpan()).ToArray();
    }

    private static async Task<(byte[] File, string Xml)> Save(XisfWriterOptions options, Action<XisfWriter> addImages)
    {
        using var output = new MemoryStream();
        string xml;

        await using (var writer = new XisfWriter(output, options, leaveOpen: true))
        {
            addImages(writer);
            await writer.SaveAsync();
            xml = writer.XmlText;
        }

        return (output.ToArray(), xml);
    }

    private static async Task<XisfImage> ReadImage(byte[] file, int index = 0)
    {
        await using var reader = new XisfReader(new MemoryStream(file)) { ThrowOnParsingFailure = true };
        return await reader.ReadImageAsync(index);
    }

    private static XElement ImageElement(string xml, int index = 0)
    {
        XNamespace ns = XisfBlockTestData.Namespace;
        return XDocument.Parse(xml).Root!.Elements(ns + "Image").ElementAt(index);
    }

    [TestMethod]
    [DataRow(CompressionCodec.None, false, ChecksumAlgorithm.None)]
    [DataRow(CompressionCodec.None, false, ChecksumAlgorithm.SHA256)]
    [DataRow(CompressionCodec.Zlib, false, ChecksumAlgorithm.None)]
    [DataRow(CompressionCodec.Zlib, true, ChecksumAlgorithm.SHA1)]
    [DataRow(CompressionCodec.Lz4, true, ChecksumAlgorithm.None)]
    [DataRow(CompressionCodec.Lz4Hc, false, ChecksumAlgorithm.SHA512)]
    [DataRow(CompressionCodec.Zstd, false, ChecksumAlgorithm.SHA3_256)]
    [DataRow(CompressionCodec.Zstd, true, ChecksumAlgorithm.SHA1)]
    public async Task AddImage_RowSource_RoundTrips(CompressionCodec codec, bool shuffle, ChecksumAlgorithm checksum)
    {
        // Small subblocks spread the image over many subblocks, several of them compressed concurrently.
        var options = new XisfWriterOptions { CompressionCodec = codec, ShuffleBytes = shuffle, ChecksumAlgorithm = checksum, SubblockSize = 1000 };

        var (file, xml) = await Save(options, writer => writer.AddImage<float>(new XisfImage(Width, Height, Channels), FillRow));

        var image = await ReadImage(file);
        image.SampleFormat.ShouldBe(SampleFormat.Float32);
        image.Data.ToArray().ShouldBe(ExpectedPlanarData());

        var element = ImageElement(xml);
        (element.Attribute("checksum") is not null).ShouldBe(checksum != ChecksumAlgorithm.None);
        (element.Attribute("subblocks") is not null).ShouldBe(codec != CompressionCodec.None);
    }

    [TestMethod]
    [DataRow(false, 1)]
    [DataRow(true, sizeof(float))]
    public async Task AddImage_RowSource_RequestsEachRowOncePerShufflePlane(bool shuffle, int expectedPasses)
    {
        int calls = 0;
        var options = new XisfWriterOptions { CompressionCodec = CompressionCodec.Zstd, ShuffleBytes = shuffle };

        await Save(options, writer => writer.AddImage<float>(new XisfImage(Width, Height, Channels), (c, y, row) =>
        {
            calls++;
            FillRow(c, y, row);
        }));

        calls.ShouldBe(Height * Channels * expectedPasses);
    }

    [TestMethod]
    public async Task AddImage_RowSource_NormalStorage_RequestsInterleavedRows()
    {
        var requested = new List<(int Channel, int Y, int Length)>();
        var image = new XisfImage(4, 3, 3) { PixelStorage = PixelStorage.Normal };

        var (file, _) = await Save(new XisfWriterOptions(), writer => writer.AddImage<ushort>(image, (c, y, row) =>
        {
            requested.Add((c, y, row.Length));
            for (int i = 0; i < row.Length; i++)
                row[i] = (ushort)(y * 100 + i);
        }));

        requested.ToArray().ShouldBe(new[] { (0, 0, 12), (0, 1, 12), (0, 2, 12) });

        var result = await ReadImage(file);
        result.PixelStorage.ShouldBe(PixelStorage.Normal);
        MemoryMarshal.Cast<byte, ushort>(result.Data.Span).ToArray()
            .ShouldBe(Enumerable.Range(0, 3).SelectMany(y => Enumerable.Range(0, 12).Select(i => (ushort)(y * 100 + i))).ToArray());
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public async Task AddImage_Stream_RoundTrips(bool seekable)
    {
        byte[] expected = ExpectedPlanarData();
        string tempDirectory = Path.Combine(Path.GetTempPath(), $"xisfsharp-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var options = new XisfWriterOptions
            {
                CompressionCodec = CompressionCodec.Zstd,
                ShuffleBytes = true,
                TempDirectory = tempDirectory,
            };

            Stream source = seekable ? new MemoryStream(expected) : new NonSeekableStream(expected);
            var (file, _) = await Save(options, writer => writer.AddImage<float>(new XisfImage(Width, Height, Channels), source));

            (await ReadImage(file)).Data.ToArray().ShouldBe(expected);
            Directory.GetFiles(tempDirectory).ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [TestMethod]
    public void AddImage_StreamShorterThanImage_Throws()
    {
        using var writer = new XisfWriter(new MemoryStream());

        Should.Throw<ArgumentException>(() =>
            writer.AddImage<float>(new XisfImage(Width, Height, Channels), new MemoryStream(new byte[100])));
    }

    [TestMethod]
    [DataRow(CompressionCodec.None)]
    [DataRow(CompressionCodec.Zstd)]
    public async Task SaveAsync_StreamedImageToNonSeekableOutput_RoundTrips(CompressionCodec codec)
    {
        var output = new NonSeekableStream();
        var options = new XisfWriterOptions { CompressionCodec = codec, ShuffleBytes = true, ChecksumAlgorithm = ChecksumAlgorithm.SHA1 };

        await using (var writer = new XisfWriter(output, options, leaveOpen: true))
        {
            writer.AddImage<float>(new XisfImage(Width, Height, Channels), FillRow);
            await writer.SaveAsync();
        }

        (await ReadImage(output.ToArray())).Data.ToArray().ShouldBe(ExpectedPlanarData());
    }

    [TestMethod]
    public async Task SaveAsync_MixedInMemoryAndStreamedImages_RoundTrips()
    {
        byte[] inMemory = XisfBlockTestData.CreateRamp(SampleFormat.UInt16, 64 * 64);
        var options = new XisfWriterOptions { CompressionCodec = CompressionCodec.Lz4, ShuffleBytes = true, ChecksumAlgorithm = ChecksumAlgorithm.SHA1 };

        var (file, _) = await Save(options, writer =>
        {
            writer.AddImage(new XisfImage(inMemory, 64, 64, SampleFormat.UInt16));
            writer.AddImage<float>(new XisfImage(Width, Height, Channels), FillRow);
            writer.AddImage(new XisfImage(inMemory, 64, 64, SampleFormat.UInt16));
        });

        (await ReadImage(file, 0)).Data.ToArray().ShouldBe(inMemory);
        (await ReadImage(file, 1)).Data.ToArray().ShouldBe(ExpectedPlanarData());
        (await ReadImage(file, 2)).Data.ToArray().ShouldBe(inMemory);
    }

    [TestMethod]
    public async Task SaveAsync_StreamedImage_ReservedHeaderSpaceIsZeroFilled()
    {
        var options = new XisfWriterOptions { CompressionCodec = CompressionCodec.Zstd, ShuffleBytes = true, ChecksumAlgorithm = ChecksumAlgorithm.SHA256 };

        var (file, xml) = await Save(options, writer => writer.AddImage<float>(new XisfImage(Width, Height, Channels), FillRow));

        int xmlLength = (int)BinaryPrimitives.ReadUInt32LittleEndian(file.AsSpan(8, 4));
        System.Text.Encoding.UTF8.GetString(file, 16, xmlLength).ShouldBe(xml);

        string location = ImageElement(xml).Attribute("location")!.Value;
        long attachmentPosition = long.Parse(location.Split(':')[1]);

        (attachmentPosition % 4096).ShouldBe(0);
        file.AsSpan(16 + xmlLength, (int)attachmentPosition - 16 - xmlLength).IndexOfAnyExcept((byte)0).ShouldBe(-1);
    }

    [TestMethod]
    public async Task SaveAsync_IncompressibleStreamedImage_StoresSubblocksVerbatim()
    {
        byte[] noise = new byte[50 * 40];
        new Random(7).NextBytes(noise);
        var options = new XisfWriterOptions { CompressionCodec = CompressionCodec.Zstd, SubblockSize = 512 };

        var (file, xml) = await Save(options, writer => writer.AddImage<byte>(new XisfImage(50, 40, 1), new MemoryStream(noise)));

        (await ReadImage(file)).Data.ToArray().ShouldBe(noise);
        SubblockTable.Parse(ImageElement(xml).Attribute("subblocks")!.Value).Subblocks.ShouldAllBe(s => s.IsStored);
    }

    [TestMethod]
    public void XisfImage_WithoutData_AllowsGeometryLargerThanArrayMaxLength()
    {
        var image = new XisfImage(61424, 33914, 1);

        using var writer = new XisfWriter(new MemoryStream());
        writer.AddImage<float>(image, (_, _, _) => { });

        image.SampleFormat.ShouldBe(SampleFormat.Float32);
        image.DataSize.ShouldBe(61424L * 33914 * sizeof(float));
        image.RequiresStreaming.ShouldBeTrue();
    }

    [TestMethod]
    public async Task SaveAsync_InMemoryImageWithChecksum_WritesVerifiableChecksum()
    {
        byte[] data = XisfBlockTestData.CreateRamp(SampleFormat.UInt16, 64 * 64);
        var options = new XisfWriterOptions { CompressionCodec = CompressionCodec.Zlib, ChecksumAlgorithm = ChecksumAlgorithm.SHA256 };

        var (file, xml) = await Save(options, writer => writer.AddImage(new XisfImage(data, 64, 64, SampleFormat.UInt16)));

        ImageElement(xml).Attribute("checksum")!.Value.ShouldStartWith("sha256:");
        (await ReadImage(file)).Data.ToArray().ShouldBe(data);
    }

    /// <summary>
    /// A readable and writable stream that does not support seeking.
    /// </summary>
    private sealed class NonSeekableStream(byte[]? data = null) : Stream
    {
        private readonly MemoryStream _inner = data is null ? new MemoryStream() : new MemoryStream(data, writable: false);

        public byte[] ToArray() => _inner.ToArray();

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => _inner.Write(buffer, offset, count);
    }
}
