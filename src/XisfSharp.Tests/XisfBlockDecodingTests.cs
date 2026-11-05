using System.Security.Cryptography;
using static XisfSharp.Tests.XisfBlockTestData;

namespace XisfSharp.Tests;

[TestClass]
public class XisfBlockDecodingTests
{
    private const int Width = 64;
    private const int Height = 32;

    private static async Task<XisfImage> ReadSingleImage(MemoryStream unit)
    {
        await using var reader = new XisfReader(unit) { ThrowOnParsingFailure = true };
        await reader.ReadHeaderAsync();
        return await reader.ReadImageAsync(0);
    }

    private static string AttachedImage(string attributes, int blockSize, SampleFormat sampleFormat = SampleFormat.Float32)
    {
        return $"""<Image geometry="{Width}:{Height}:1" sampleFormat="{sampleFormat}" bounds="0:1" colorSpace="Gray" {attributes} location="attachment:{AttachmentPosition}:{blockSize}"/>""";
    }

    [TestMethod]
    [DataRow(CompressionCodec.Zlib, false)]
    [DataRow(CompressionCodec.Zlib, true)]
    [DataRow(CompressionCodec.Lz4, false)]
    [DataRow(CompressionCodec.Lz4, true)]
    [DataRow(CompressionCodec.Lz4Hc, false)]
    [DataRow(CompressionCodec.Lz4Hc, true)]
    [DataRow(CompressionCodec.Zstd, false)]
    [DataRow(CompressionCodec.Zstd, true)]
    public async Task ReadImageAsync_CompressedWithSubblocks_DecodesImage(CompressionCodec codec, bool shuffle)
    {
        byte[] expected = CreateRamp(SampleFormat.Float32, Width * Height);
        byte[] serialized = shuffle ? Shuffle(expected, 4) : expected;

        // Subblock boundaries deliberately do not fall on item boundaries.
        int[] lengths = [1001, 3001, serialized.Length - 4002];
        var (block, subblocks) = CompressSubblocks(serialized, codec, lengths);

        string compression = shuffle
            ? $"{CodecName(codec)}+sh:{expected.Length}:4"
            : $"{CodecName(codec)}:{expected.Length}";

        var unit = CreateUnit(
            AttachedImage($"""compression="{compression}" subblocks="{subblocks}" """, block.Length), block);

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(CompressionCodec.Zlib)]
    [DataRow(CompressionCodec.Lz4)]
    [DataRow(CompressionCodec.Zstd)]
    public async Task ReadImageAsync_SubblockWithEqualSizes_IsReadAsStored(CompressionCodec codec)
    {
        byte[] expected = CreateRamp(SampleFormat.Float32, Width * Height);
        byte[] shuffled = Shuffle(expected, 4);

        int[] lengths = [2048, 2048, shuffled.Length - 4096];
        var (block, subblocks) = CompressSubblocks(shuffled, codec, lengths, storedIndices: 1);

        var unit = CreateUnit(
            AttachedImage($"""compression="{CodecName(codec)}+sh:{expected.Length}:4" subblocks="{subblocks}" """, block.Length), block);

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    public async Task ReadImageAsync_CompressedWithoutSubblocksAttribute_DecodesSingleSubblock()
    {
        byte[] expected = CreateRamp(SampleFormat.Float32, Width * Height);
        byte[] block = Compress(Shuffle(expected, 4), CompressionCodec.Zstd);

        var unit = CreateUnit(
            AttachedImage($"""compression="zstd+sh:{expected.Length}:4" """, block.Length), block);

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    public async Task ReadImageAsync_SubblockSizesDoNotAddUp_Throws()
    {
        byte[] expected = CreateRamp(SampleFormat.Float32, Width * Height);
        var (block, _) = CompressSubblocks(expected, CompressionCodec.Zlib, [4096, expected.Length - 4096]);

        var unit = CreateUnit(
            AttachedImage($"""compression="zlib:{expected.Length}" subblocks="{block.Length},4096" """, block.Length), block);

        var exception = await Should.ThrowAsync<XisfException>(() => ReadSingleImage(unit));
        exception.Message.ShouldContain("Subblock");
    }

    [TestMethod]
    [DataRow("10")]
    [DataRow("10,x")]
    [DataRow("0,5")]
    [DataRow("10,20:")]
    public async Task ReadHeaderAsync_InvalidSubblocksAttribute_Throws(string subblocks)
    {
        var unit = CreateUnit(
            AttachedImage($"""compression="zlib:8192" subblocks="{subblocks}" """, 100), new byte[100]);

        await using var reader = new XisfReader(unit) { ThrowOnParsingFailure = true };

        await Should.ThrowAsync<XisfException>(() => reader.ReadHeaderAsync());
    }

    [TestMethod]
    [DataRow(SampleFormat.UInt16, false)]
    [DataRow(SampleFormat.UInt32, false)]
    [DataRow(SampleFormat.Float32, false)]
    [DataRow(SampleFormat.Float64, false)]
    [DataRow(SampleFormat.UInt16, true)]
    [DataRow(SampleFormat.Float64, true)]
    public async Task ReadImageAsync_BigEndian_ConvertsToNativeByteOrder(SampleFormat sampleFormat, bool compressed)
    {
        int sampleSize = SampleSize(sampleFormat);
        byte[] expected = CreateRamp(sampleFormat, Width * Height);
        byte[] bigEndian = ReverseElements(expected, sampleSize);

        // Byte order applies to the uncompressed data, so shuffling operates on the big-endian bytes.
        byte[] block = compressed ? Compress(Shuffle(bigEndian, sampleSize), CompressionCodec.Zstd) : bigEndian;
        string compression = compressed ? $"""compression="zstd+sh:{bigEndian.Length}:{sampleSize}" """ : "";

        var unit = CreateUnit(
            AttachedImage($"""byteOrder="big" {compression}""", block.Length, sampleFormat), block);

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    public async Task ReadImageAsync_ExplicitLittleEndian_IsUnchanged()
    {
        byte[] expected = CreateRamp(SampleFormat.UInt16, Width * Height);

        var unit = CreateUnit(AttachedImage("""byteOrder="little" """, expected.Length, SampleFormat.UInt16), expected);

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    public async Task ReadImageAsync_EmbeddedBigEndian_ConvertsToNativeByteOrder()
    {
        byte[] expected = CreateRamp(SampleFormat.UInt16, 16 * 8);
        string data = Convert.ToBase64String(ReverseElements(expected, 2));

        var unit = CreateUnit(
            $"""<Image geometry="16:8:1" sampleFormat="UInt16" colorSpace="Gray" byteOrder="big" location="embedded"><Data encoding="base64">{data}</Data></Image>""");

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    public async Task ReadHeaderAsync_InvalidByteOrder_Throws()
    {
        var unit = CreateUnit(AttachedImage("""byteOrder="middle" """, 100), new byte[100]);

        await using var reader = new XisfReader(unit) { ThrowOnParsingFailure = true };

        var exception = await Should.ThrowAsync<XisfException>(() => reader.ReadHeaderAsync());
        exception.Message.ShouldContain("byteOrder");
    }

    [TestMethod]
    public async Task ReadImageAsync_EmbeddedUncompressedWithChecksum_VerifiesAndLoads()
    {
        byte[] expected = CreateRamp(SampleFormat.UInt16, 16 * 8);
        string digest = Convert.ToHexString(SHA1.HashData(expected)).ToLowerInvariant();

        var unit = CreateUnit(
            $"""<Image geometry="16:8:1" sampleFormat="UInt16" colorSpace="Gray" location="embedded"><Data encoding="base64" checksum="sha1:{digest}">{Convert.ToBase64String(expected)}</Data></Image>""");

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    public async Task ReadImageAsync_EmbeddedCompressedWithChecksum_VerifiesAndLoads()
    {
        byte[] expected = CreateRamp(SampleFormat.UInt16, 16 * 8);
        byte[] compressed = Compress(expected, CompressionCodec.Zlib);
        string digest = Convert.ToHexString(SHA256.HashData(compressed)).ToLowerInvariant();

        var unit = CreateUnit(
            $"""<Image geometry="16:8:1" sampleFormat="UInt16" colorSpace="Gray" location="embedded"><Data encoding="base64" compression="zlib:{expected.Length}" checksum="sha256:{digest}">{Convert.ToBase64String(compressed)}</Data></Image>""");

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    public async Task ReadImageAsync_AttachedUncompressedWithChecksum_VerifiesAndLoads()
    {
        // Larger than one checksum read chunk so the digest is accumulated across several reads.
        byte[] expected = CreateRamp(SampleFormat.Float32, 1024 * 300);
        string digest = Convert.ToHexString(SHA512.HashData(expected)).ToLowerInvariant();

        var unit = CreateUnit(
            $"""<Image geometry="1024:300:1" sampleFormat="Float32" bounds="0:1" colorSpace="Gray" checksum="sha512:{digest}" location="attachment:{AttachmentPosition}:{expected.Length}"/>""",
            expected);

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    public async Task ReadImageAsync_CompressedChecksumCoversStoredBytes()
    {
        byte[] expected = CreateRamp(SampleFormat.Float32, Width * Height);
        var (block, subblocks) = CompressSubblocks(Shuffle(expected, 4), CompressionCodec.Zlib, [4096, expected.Length - 4096]);
        string digest = Convert.ToHexString(SHA1.HashData(block)).ToLowerInvariant();

        var unit = CreateUnit(
            AttachedImage($"""compression="zlib+sh:{expected.Length}:4" subblocks="{subblocks}" checksum="sha1:{digest}" """, block.Length), block);

        var image = await ReadSingleImage(unit);

        image.Data.ToArray().ShouldBe(expected);
    }

    [TestMethod]
    public async Task ReadImageAsync_ChecksumMismatch_ThrowsBeforeDecompressingAndOnEveryAttempt()
    {
        // The block is not valid zlib data, so any attempt to decompress it would fail with a different exception.
        byte[] block = new byte[512];
        Random.Shared.NextBytes(block);
        string wrongDigest = new('0', 40);

        var unit = CreateUnit(
            AttachedImage($"""compression="zlib:{Width * Height * 4}" checksum="sha1:{wrongDigest}" """, block.Length), block);

        await using var reader = new XisfReader(unit) { ThrowOnParsingFailure = true };
        await reader.ReadHeaderAsync();

        var first = await Should.ThrowAsync<XisfException>(() => reader.ReadImageAsync(0));
        first.Message.ShouldContain("checksum mismatch");

        var second = await Should.ThrowAsync<XisfException>(() => reader.ReadImageAsync(0));
        second.Message.ShouldContain("checksum mismatch");
    }

    [TestMethod]
    public async Task ReadHeaderAsync_ImageLargerThanArrayMaxLength_ReportsSizeAndRefusesToLoad()
    {
        const long expectedSize = 61424L * 33914L * sizeof(float);

        var unit = CreateUnit(
            $"""<Image geometry="61424:33914:1" sampleFormat="Float32" bounds="0:1" colorSpace="Gray" location="attachment:{AttachmentPosition}:{expectedSize}"/>""");

        await using var reader = new XisfReader(unit) { ThrowOnParsingFailure = true };
        await reader.ReadHeaderAsync();

        var image = reader.Images[0];
        image.DataSize.ShouldBe(expectedSize);
        image.RequiresStreaming.ShouldBeTrue();

        var exception = await Should.ThrowAsync<XisfException>(() => reader.ReadImageAsync(0));
        exception.Message.ShouldContain("maximum in-memory size");
    }

    [TestMethod]
    public async Task ReadHeaderAsync_CompressedImageLargerThanArrayMaxLength_RefusesToLoad()
    {
        const long uncompressedSize = 61424L * 33914L * sizeof(float);

        var unit = CreateUnit(
            $"""<Image geometry="61424:33914:1" sampleFormat="Float32" bounds="0:1" colorSpace="Gray" compression="zstd+sh:{uncompressedSize}:4" location="attachment:{AttachmentPosition}:3025304397"/>""");

        await using var reader = new XisfReader(unit) { ThrowOnParsingFailure = true };
        await reader.ReadHeaderAsync();

        reader.Images[0].RequiresStreaming.ShouldBeTrue();

        var exception = await Should.ThrowAsync<XisfException>(() => reader.ReadImageAsync(0));
        exception.Message.ShouldContain("maximum in-memory size");
    }
}
