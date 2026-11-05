using XisfSharp.IO;
using static XisfSharp.Tests.XisfBlockTestData;

namespace XisfSharp.Tests;

[TestClass]
public class DecodedBlockStreamTests
{
    private static InputBlock AttachedBlock(
        byte[] stored,
        long uncompressedSize,
        CompressionCodec codec = CompressionCodec.None,
        int itemSize = 1,
        string? subblocks = null)
    {
        return new InputBlock
        {
            AttachmentPosition = AttachmentPosition,
            AttachmentSize = stored.Length,
            CompressedSize = codec == CompressionCodec.None ? 0 : stored.Length,
            UncompressedSize = codec == CompressionCodec.None ? 0 : uncompressedSize,
            CompressionCodec = codec,
            ByteShuffling = itemSize > 1,
            ItemSize = itemSize,
            DeclaredSubblocks = subblocks is null ? null : SubblockTable.Parse(subblocks),
        };
    }

    private static MemoryStream SourceWith(byte[] stored)
    {
        var bytes = new byte[AttachmentPosition + stored.Length];
        stored.CopyTo(bytes, AttachmentPosition);
        return new MemoryStream(bytes);
    }

    private static async Task<byte[]> DecodeAll(InputBlock block, MemoryStream source, BlockDecodeOptions options, int readSize = 4096)
    {
        await using var decoded = await DecodedBlockStream.OpenAsync(block, source, AttachmentPosition, options, CancellationToken.None);

        using var output = new MemoryStream();
        byte[] buffer = new byte[readSize];
        int read;
        while ((read = await decoded.ReadAsync(buffer)) > 0)
        {
            output.Write(buffer, 0, read);
        }

        decoded.Position.ShouldBe(decoded.Length);
        return output.ToArray();
    }

    [TestMethod]
    [DataRow(CompressionCodec.None, 1, 64)]
    [DataRow(CompressionCodec.None, 1, 1000)]
    [DataRow(CompressionCodec.Zlib, 1, 64)]
    [DataRow(CompressionCodec.Zlib, 4, 64)]
    [DataRow(CompressionCodec.Zlib, 4, 200)]
    [DataRow(CompressionCodec.Zlib, 8, 64)]
    [DataRow(CompressionCodec.Zstd, 2, 64)]
    [DataRow(CompressionCodec.Zstd, 4, 96)]
    [DataRow(CompressionCodec.Zstd, 8, 4096)]
    [DataRow(CompressionCodec.Lz4, 4, 64)]
    [DataRow(CompressionCodec.Lz4Hc, 8, 256)]
    public async Task Read_SmallChunksAcrossSubblocks_MatchesOriginal(CompressionCodec codec, int itemSize, int chunkSize)
    {
        byte[] expected = CreateRamp(SampleFormat.Float64, 1500);
        byte[] serialized = itemSize > 1 ? Shuffle(expected, itemSize) : expected;

        byte[] stored;
        string? subblocks = null;
        if (codec == CompressionCodec.None)
        {
            stored = serialized;
        }
        else
        {
            // Uneven subblocks so lanes start part way through a subblock and cross several boundaries.
            int[] lengths = [777, 3001, 1, 4000, serialized.Length - 7779];
            (stored, subblocks) = CompressSubblocks(serialized, codec, lengths, storedIndices: 2);
        }

        var block = AttachedBlock(stored, expected.Length, codec, itemSize, subblocks);

        // Read sizes unrelated to the chunk size exercise partial reads of the staging buffer.
        byte[] actual = await DecodeAll(block, SourceWith(stored), new BlockDecodeOptions { ChunkSize = chunkSize }, readSize: 37);

        actual.ShouldBe(expected);
    }

    [TestMethod]
    [DataRow(CompressionCodec.None)]
    [DataRow(CompressionCodec.Zstd)]
    public async Task Read_LengthNotMultipleOfItemSize_AppendsUnshuffledTail(CompressionCodec codec)
    {
        byte[] expected = CreateRamp(SampleFormat.UInt8, 8195);
        byte[] shuffled = Shuffle(expected, 4);
        byte[] stored = codec == CompressionCodec.None ? shuffled : Compress(shuffled, codec);

        // XISF only shuffles compressed blocks; the uncompressed case exercises the lane layout on its own.
        var block = AttachedBlock(stored, expected.Length, codec, itemSize: 4);

        byte[] actual = await DecodeAll(block, SourceWith(stored), new BlockDecodeOptions { ChunkSize = 64 });

        actual.ShouldBe(expected);
    }

    [TestMethod]
    public async Task Open_ShuffledLz4AboveLaneMemoryLimit_DecodesThroughTempFile()
    {
        byte[] expected = CreateRamp(SampleFormat.Float32, 4096);
        byte[] shuffled = Shuffle(expected, 4);
        int[] lengths = [4096, 4096, 4096, 4096];
        var (stored, subblocks) = CompressSubblocks(shuffled, CompressionCodec.Lz4, lengths);

        var block = AttachedBlock(stored, expected.Length, CompressionCodec.Lz4, itemSize: 4, subblocks);
        var options = new BlockDecodeOptions { ChunkSize = 256, LaneMemoryLimit = 0 };

        await using var decoded = await DecodedBlockStream.OpenAsync(block, SourceWith(stored), AttachmentPosition, options, CancellationToken.None);
        decoded.UsesTempFile.ShouldBeTrue();

        byte[] actual = new byte[decoded.Length];
        await decoded.ReadExactlyAsync(actual);

        actual.ShouldBe(expected);
    }

    [TestMethod]
    public async Task Open_ShuffledLz4WithinLaneMemoryLimit_DecodesWithLanes()
    {
        byte[] expected = CreateRamp(SampleFormat.Float32, 4096);
        var (stored, subblocks) = CompressSubblocks(Shuffle(expected, 4), CompressionCodec.Lz4, [8192, 8192]);

        var block = AttachedBlock(stored, expected.Length, CompressionCodec.Lz4, itemSize: 4, subblocks);

        await using var decoded = await DecodedBlockStream.OpenAsync(block, SourceWith(stored), AttachmentPosition, BlockDecodeOptions.Default, CancellationToken.None);
        decoded.UsesTempFile.ShouldBeFalse();

        byte[] actual = new byte[decoded.Length];
        await decoded.ReadExactlyAsync(actual);

        actual.ShouldBe(expected);
    }

    [TestMethod]
    public async Task Open_TempFileIsDeletedOnDispose()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"xisfsharp-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            byte[] expected = CreateRamp(SampleFormat.Float32, 4096);
            var (stored, subblocks) = CompressSubblocks(Shuffle(expected, 4), CompressionCodec.Lz4, [4096, 4096, 4096, 4096]);
            var block = AttachedBlock(stored, expected.Length, CompressionCodec.Lz4, itemSize: 4, subblocks);
            var options = new BlockDecodeOptions { LaneMemoryLimit = 0, TempDirectory = directory };

            var decoded = await DecodedBlockStream.OpenAsync(block, SourceWith(stored), AttachmentPosition, options, CancellationToken.None);
            Directory.GetFiles(directory).Length.ShouldBe(1);

            await decoded.DisposeAsync();
            Directory.GetFiles(directory).ShouldBeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Open_BigEndianWithPartialElement_Throws()
    {
        byte[] stored = new byte[10];
        var block = AttachedBlock(stored, stored.Length);
        block.IsBigEndian = true;
        block.ElementSize = 4;

        await Should.ThrowAsync<XisfException>(() =>
            DecodedBlockStream.OpenAsync(block, SourceWith(stored), AttachmentPosition, BlockDecodeOptions.Default, CancellationToken.None));
    }

    [TestMethod]
    public async Task Open_ChunkSizeBelowMinimum_Throws()
    {
        byte[] stored = new byte[16];
        var block = AttachedBlock(stored, stored.Length);
        var options = new BlockDecodeOptions { ChunkSize = BlockDecodeOptions.MinimumChunkSize - 1 };

        await Should.ThrowAsync<ArgumentOutOfRangeException>(() =>
            DecodedBlockStream.OpenAsync(block, SourceWith(stored), AttachmentPosition, options, CancellationToken.None));
    }

    [TestMethod]
    [DataRow(CompressionCodec.Zstd)]
    [DataRow(CompressionCodec.Lz4)]
    public async Task ReadImageAsync_FromFile_DecodesLanesConcurrentlyThroughFileHandle(CompressionCodec codec)
    {
        string path = Path.Combine(Path.GetTempPath(), $"xisfsharp-test-{Guid.NewGuid():N}.xisf");

        try
        {
            double[] samples = Enumerable.Range(0, 300 * 200).Select(i => Math.Cos(i * 0.001)).ToArray();
            var image = new XisfImage();
            image.SetData(samples, 300, 200, 1);

            var options = new XisfWriterOptions { CompressionCodec = codec, ShuffleBytes = true, SubblockSize = 10000 };
            await image.SaveAsync(path, options);

            await using var reader = new XisfReader(path) { DecodeOptions = new BlockDecodeOptions { ChunkSize = 4096 } };
            var result = await reader.ReadImageAsync(0);

            result.Data.ToArray().ShouldBe(image.Data.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task Dispose_WhileReadingAhead_ReleasesCleanly()
    {
        byte[] expected = CreateRamp(SampleFormat.Float32, 64 * 1024);
        byte[] stored = Compress(Shuffle(expected, 4), CompressionCodec.Zstd);
        var block = AttachedBlock(stored, expected.Length, CompressionCodec.Zstd, itemSize: 4);

        var decoded = await DecodedBlockStream.OpenAsync(block, SourceWith(stored), AttachmentPosition, new BlockDecodeOptions { ChunkSize = 1024 }, CancellationToken.None);

        // The first read leaves a decode of the next chunk running in the background.
        byte[] buffer = new byte[16];
        await decoded.ReadExactlyAsync(buffer);
        buffer.ShouldBe(expected[..16]);

        await decoded.DisposeAsync();
        decoded.CanRead.ShouldBeFalse();
    }

    [TestMethod]
    public async Task Read_AfterEnd_ReturnsZero()
    {
        byte[] stored = CreateRamp(SampleFormat.UInt16, 100);
        var block = AttachedBlock(stored, stored.Length);

        await using var decoded = await DecodedBlockStream.OpenAsync(block, SourceWith(stored), AttachmentPosition, BlockDecodeOptions.Default, CancellationToken.None);

        byte[] buffer = new byte[stored.Length];
        await decoded.ReadExactlyAsync(buffer);

        (await decoded.ReadAsync(buffer)).ShouldBe(0);
        buffer.ShouldBe(stored);
    }
}
