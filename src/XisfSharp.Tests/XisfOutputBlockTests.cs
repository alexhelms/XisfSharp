using System.Runtime.InteropServices;
using XisfSharp.IO;

namespace XisfSharp.Tests;

[TestClass]
public class XisfOutputBlockTests
{
    [TestMethod]
    public void Constructor_InitializesWithData()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.HasData.ShouldBeTrue();
        outputBlock.BlockSize.ShouldBe(data.Length);
    }

    [TestMethod]
    public void Constructor_GeneratesAttachmentPosition()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.AttachmentPosition.ShouldNotBeNullOrEmpty();
        Guid.TryParse(outputBlock.AttachmentPosition, out _).ShouldBeTrue();
    }

    [TestMethod]
    public void HasData_ReturnsTrueWithData()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.HasData.ShouldBeTrue();
    }

    [TestMethod]
    public void HasData_ReturnsFalseWithEmptyData()
    {
        var data = Array.Empty<byte>();
        var outputBlock = new OutputBlock(data);

        outputBlock.HasData.ShouldBeFalse();
    }

    [TestMethod]
    public void HasCompression_ReturnsFalseByDefault()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.HasCompression.ShouldBeFalse();
    }

    [TestMethod]
    public void HasCompression_ReturnsTrueAfterCompression()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.CompressData(CompressionCodec.Zstd, 1, false);

        outputBlock.HasCompression.ShouldBeTrue();
    }

    [TestMethod]
    public void HasCompressedData_ReturnsFalseByDefault()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.HasCompressedData.ShouldBeFalse();
    }

    [TestMethod]
    public void HasCompressedData_ReturnsTrueAfterCompression()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.CompressData(CompressionCodec.Zstd, 1, false);

        outputBlock.HasCompressedData.ShouldBeTrue();
    }

    [TestMethod]
    public void HasChecksum_ReturnsFalseByDefault()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.HasChecksum.ShouldBeFalse();
    }

    [TestMethod]
    public void HasChecksum_ReturnsTrueAfterComputeChecksum()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.ComputeChecksum(ChecksumAlgorithm.SHA256);

        outputBlock.HasChecksum.ShouldBeTrue();
    }

    [TestMethod]
    public void BlockSize_ReturnsDataLength()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.BlockSize.ShouldBe(data.Length);
    }

    [TestMethod]
    public void BlockSize_ReturnsCompressedDataLength()
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(CompressionCodec.Zstd, sizeof(ushort), false);

        outputBlock.BlockSize.ShouldBeLessThan(originalBytes.Length);
        outputBlock.BlockSize.ShouldBeGreaterThan(0);
    }

    [TestMethod]
    public void BlockSize_ReturnsZeroForEmptyData()
    {
        var data = Array.Empty<byte>();
        var outputBlock = new OutputBlock(data);

        outputBlock.BlockSize.ShouldBe(0);
    }

    [TestMethod]
    public void GetEncodedData_Base64Encoding()
    {
        var data = "hello world"u8.ToArray();
        var expected = Convert.ToBase64String(data);
        var outputBlock = new OutputBlock(data);

        var encoded = outputBlock.GetEncodedData(DataEncoding.Base64);

        encoded.ShouldBe(expected);
    }

    [TestMethod]
    public void GetEncodedData_Base16Encoding()
    {
        var data = "hello world"u8.ToArray();
        var expected = Convert.ToHexString(data).ToLowerInvariant();
        var outputBlock = new OutputBlock(data);

        var encoded = outputBlock.GetEncodedData(DataEncoding.Base16);

        encoded.ShouldBe(expected);
    }

    [TestMethod]
    public void GetEncodedData_NoEncoding()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        var encoded = outputBlock.GetEncodedData(DataEncoding.None);

        encoded.ShouldBeEmpty();
    }

    [TestMethod]
    public void GetEncodedData_EmptyData()
    {
        var data = Array.Empty<byte>();
        var outputBlock = new OutputBlock(data);

        var encoded = outputBlock.GetEncodedData(DataEncoding.Base64);

        encoded.ShouldBeEmpty();
    }

    [TestMethod]
    public void CompressData_NoCompression()
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(CompressionCodec.None, 1, false);

        outputBlock.CompressionCodec.ShouldBe(CompressionCodec.None);
        outputBlock.HasCompressedData.ShouldBeTrue();
        outputBlock.BlockSize.ShouldBe(originalBytes.Length);
        outputBlock.ShuffleBytes.ShouldBeFalse();
        outputBlock.ItemSize.ShouldBe(1);
    }

    [TestMethod]
    [DataRow(CompressionCodec.Lz4)]
    [DataRow(CompressionCodec.Lz4Hc)]
    [DataRow(CompressionCodec.Zlib)]
    [DataRow(CompressionCodec.Zstd)]
    public void CompressData(CompressionCodec codec)
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(codec, sizeof(ushort), false);

        outputBlock.CompressionCodec.ShouldBe(codec);
        outputBlock.HasCompressedData.ShouldBeTrue();
        outputBlock.HasCompression.ShouldBeTrue();
        outputBlock.ShuffleBytes.ShouldBeFalse();
        outputBlock.ItemSize.ShouldBe(sizeof(ushort));
    }

    [TestMethod]
    [DataRow(CompressionCodec.Lz4)]
    [DataRow(CompressionCodec.Lz4Hc)]
    [DataRow(CompressionCodec.Zlib)]
    [DataRow(CompressionCodec.Zstd)]
    public void CompressData_WithShuffle(CompressionCodec codec)
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(codec, sizeof(ushort), true);

        outputBlock.CompressionCodec.ShouldBe(codec);
        outputBlock.HasCompressedData.ShouldBeTrue();
        outputBlock.HasCompression.ShouldBeTrue();
        outputBlock.ShuffleBytes.ShouldBeTrue();
        outputBlock.ItemSize.ShouldBe(sizeof(ushort));
    }

    [TestMethod]
    public void CompressData_EmptyData()
    {
        var data = Array.Empty<byte>();
        var outputBlock = new OutputBlock(data);

        outputBlock.CompressData(CompressionCodec.Zstd, 1, false);

        outputBlock.HasCompressedData.ShouldBeFalse();
    }

    [TestMethod]
    [DataRow(ChecksumAlgorithm.SHA1)]
    [DataRow(ChecksumAlgorithm.SHA256)]
    [DataRow(ChecksumAlgorithm.SHA512)]
    [DataRow(ChecksumAlgorithm.SHA3_256)]
    [DataRow(ChecksumAlgorithm.SHA3_512)]
    public void ComputeChecksum(ChecksumAlgorithm algorithm)
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        string expectedChecksum = XisfUtil.GetChecksumString(originalBytes, algorithm);
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.ComputeChecksum(algorithm);

        outputBlock.HasChecksum.ShouldBeTrue();
        outputBlock.ChecksumAlgorithm.ShouldBe(algorithm);
        outputBlock.Checksum.ShouldBe(expectedChecksum);
    }

    [TestMethod]
    public void ComputeChecksum_None()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.ComputeChecksum(ChecksumAlgorithm.None);

        outputBlock.HasChecksum.ShouldBeFalse();
        outputBlock.Checksum.ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow(ChecksumAlgorithm.SHA1)]
    [DataRow(ChecksumAlgorithm.SHA256)]
    [DataRow(ChecksumAlgorithm.SHA512)]
    [DataRow(ChecksumAlgorithm.SHA3_256)]
    [DataRow(ChecksumAlgorithm.SHA3_512)]
    public void ComputeChecksum_OnCompressedData(ChecksumAlgorithm algorithm)
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(CompressionCodec.Zstd, sizeof(ushort), false);
        outputBlock.ComputeChecksum(algorithm);

        outputBlock.HasChecksum.ShouldBeTrue();
        outputBlock.ChecksumAlgorithm.ShouldBe(algorithm);
        outputBlock.Checksum.ShouldNotBeNullOrEmpty();
    }

    [TestMethod]
    public void ComputeChecksum_EmptyData()
    {
        var data = Array.Empty<byte>();
        var outputBlock = new OutputBlock(data);

        outputBlock.ComputeChecksum(ChecksumAlgorithm.SHA256);

        outputBlock.HasChecksum.ShouldBeFalse();
    }

    [TestMethod]
    public void GetCompressionAttribute_NoCompression()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        var attribute = outputBlock.GetCompressionAttribute();

        attribute.ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow(CompressionCodec.Lz4, "lz4")]
    [DataRow(CompressionCodec.Lz4Hc, "lz4hc")]
    [DataRow(CompressionCodec.Zlib, "zlib")]
    [DataRow(CompressionCodec.Zstd, "zstd")]
    public void GetCompressionAttribute(CompressionCodec codec, string expectedCodecString)
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(codec, sizeof(ushort), false);
        var attribute = outputBlock.GetCompressionAttribute();

        attribute.ShouldStartWith($"{expectedCodecString}:");
        attribute.ShouldContain($":{outputBlock.UncompressedBlockSize}");
    }

    [TestMethod]
    [DataRow(CompressionCodec.Lz4, "lz4")]
    [DataRow(CompressionCodec.Lz4Hc, "lz4hc")]
    [DataRow(CompressionCodec.Zlib, "zlib")]
    [DataRow(CompressionCodec.Zstd, "zstd")]
    public void GetCompressionAttribute_WithShuffle(CompressionCodec codec, string expectedCodecString)
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(codec, sizeof(ushort), true);
        var attribute = outputBlock.GetCompressionAttribute();

        attribute.ShouldStartWith($"{expectedCodecString}+sh:");
        attribute.ShouldContain($":{outputBlock.UncompressedBlockSize}:{sizeof(ushort)}");
    }

    [TestMethod]
    public void GetCompressionAttribute_ShuffleWithItemSize1()
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(CompressionCodec.Zstd, 1, true);
        var attribute = outputBlock.GetCompressionAttribute();

        attribute.ShouldNotContain("+sh");
        attribute.ShouldNotEndWith(":1");
    }

    [TestMethod]
    public void GetChecksumAttribute_NoChecksum()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        var attribute = outputBlock.GetChecksumAttribute();

        attribute.ShouldBeEmpty();
    }

    [TestMethod]
    [DataRow(ChecksumAlgorithm.SHA1, "sha1")]
    [DataRow(ChecksumAlgorithm.SHA256, "sha256")]
    [DataRow(ChecksumAlgorithm.SHA512, "sha512")]
    [DataRow(ChecksumAlgorithm.SHA3_256, "sha3-256")]
    [DataRow(ChecksumAlgorithm.SHA3_512, "sha3-512")]
    public void GetChecksumAttribute(ChecksumAlgorithm algorithm, string expectedAlgorithmString)
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.ComputeChecksum(algorithm);
        var attribute = outputBlock.GetChecksumAttribute();

        attribute.ShouldStartWith($"{expectedAlgorithmString}:");
        attribute.ShouldContain(outputBlock.Checksum);
    }

    [TestMethod]
    public async Task WriteDataAsync_WritesData()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        using var stream = new MemoryStream();
        await outputBlock.WriteDataAsync(stream);

        stream.ToArray().ShouldBe(data);
    }

    [TestMethod]
    public async Task WriteDataAsync_WritesCompressedData()
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(CompressionCodec.Zstd, sizeof(ushort), false);

        using var stream = new MemoryStream();
        await outputBlock.WriteDataAsync(stream);

        var written = stream.ToArray();
        written.LongLength.ShouldBe(outputBlock.BlockSize);
        written.ShouldNotBe(originalBytes);
    }

    [TestMethod]
    public async Task WriteDataAsync_EmptyData()
    {
        var data = Array.Empty<byte>();
        var outputBlock = new OutputBlock(data);

        using var stream = new MemoryStream();
        await outputBlock.WriteDataAsync(stream);

        stream.ToArray().ShouldBeEmpty();
    }

    [TestMethod]
    public async Task WriteDataAsync_WithCancellationToken()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        using var cts = new CancellationTokenSource();
        using var stream = new MemoryStream();
        await outputBlock.WriteDataAsync(stream, cts.Token);

        stream.ToArray().ShouldBe(data);
    }

    [TestMethod]
    public void AttachmentPosition_CanBeSet()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);
        var newPosition = "custom-position-123";

        outputBlock.AttachmentPosition = newPosition;

        outputBlock.AttachmentPosition.ShouldBe(newPosition);
    }

    [TestMethod]
    public void UncompressedBlockSize_ReturnsOriginalDataLength()
    {
        var data = "hello world"u8.ToArray();
        var outputBlock = new OutputBlock(data);

        outputBlock.UncompressedBlockSize.ShouldBe(data.Length);
    }

    [TestMethod]
    public void UncompressedBlockSize_RemainsConstantAfterCompression()
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);
        int originalSize = originalBytes.Length;

        outputBlock.CompressData(CompressionCodec.Zstd, sizeof(ushort), false);

        outputBlock.UncompressedBlockSize.ShouldBe(originalSize);
        outputBlock.BlockSize.ShouldBeLessThan(originalSize); // Compressed size is smaller
    }

    [TestMethod]
    [DataRow(CompressionCodec.Lz4)]
    [DataRow(CompressionCodec.Lz4Hc)]
    [DataRow(CompressionCodec.Zlib)]
    [DataRow(CompressionCodec.Zstd)]
    public void CompressData_ClearsOriginalData(CompressionCodec codec)
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.HasData.ShouldBeTrue();

        outputBlock.CompressData(codec, sizeof(ushort), false);

        outputBlock.HasData.ShouldBeFalse(); // Data is cleared after compression
        outputBlock.HasCompressedData.ShouldBeTrue();
    }

    [TestMethod]
    public void CompressData_NoCompression_KeepsOriginalData()
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(CompressionCodec.None, 1, false);

        outputBlock.HasData.ShouldBeTrue(); // Original data is NOT cleared
        outputBlock.HasCompressedData.ShouldBeTrue();
    }

    [TestMethod]
    [DataRow(CompressionCodec.Lz4, "lz4")]
    [DataRow(CompressionCodec.Lz4Hc, "lz4hc")]
    [DataRow(CompressionCodec.Zlib, "zlib")]
    [DataRow(CompressionCodec.Zstd, "zstd")]
    public void GetCompressionAttribute_ContainsUncompressedSize(CompressionCodec codec, string expectedCodecString)
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);
        int uncompressedSize = originalBytes.Length;

        outputBlock.CompressData(codec, sizeof(ushort), false);
        var attribute = outputBlock.GetCompressionAttribute();

        // Verify attribute format: "<codec>:<uncompressed-size>"
        attribute.ShouldBe($"{expectedCodecString}:{uncompressedSize}");
        attribute.ShouldNotContain($":{outputBlock.BlockSize}"); // Should NOT contain compressed size
    }

    [TestMethod]
    [DataRow(CompressionCodec.Lz4, "lz4")]
    [DataRow(CompressionCodec.Lz4Hc, "lz4hc")]
    [DataRow(CompressionCodec.Zlib, "zlib")]
    [DataRow(CompressionCodec.Zstd, "zstd")]
    public void GetCompressionAttribute_WithShuffle_ContainsUncompressedSize(CompressionCodec codec, string expectedCodecString)
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);
        int uncompressedSize = originalBytes.Length;

        outputBlock.CompressData(codec, sizeof(ushort), true);
        var attribute = outputBlock.GetCompressionAttribute();

        // Verify attribute format: "<codec>+sh:<uncompressed-size>:<item-size>"
        attribute.ShouldBe($"{expectedCodecString}+sh:{uncompressedSize}:{sizeof(ushort)}");
    }

    [TestMethod]
    public void BlockSize_VsUncompressedBlockSize_AfterCompression()
    {
        ushort[] original = Enumerable.Range(0, 128).Select(x => (ushort)x).ToArray();
        byte[] originalBytes = MemoryMarshal.Cast<ushort, byte>(original).ToArray();
        var outputBlock = new OutputBlock(originalBytes);

        outputBlock.CompressData(CompressionCodec.Zstd, sizeof(ushort), false);

        // BlockSize returns compressed size, UncompressedBlockSize returns original size
        outputBlock.BlockSize.ShouldBeLessThan(outputBlock.UncompressedBlockSize);
        outputBlock.UncompressedBlockSize.ShouldBe(originalBytes.Length);
    }
}
