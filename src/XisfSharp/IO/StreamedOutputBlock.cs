using System.Buffers;

namespace XisfSharp.IO;

/// <summary>
/// An image data block that is read from its source and encoded while it is written.
/// </summary>
/// <remarks>
/// The stored size, subblock layout and checksum are only known once the block has been written. Until then the
/// header refers to them through placeholder tokens, and <see cref="MaxBlockSize"/> and the worst-case attribute
/// values bound how much header space they can need.
/// </remarks>
internal sealed class StreamedOutputBlock : IOutputAttachment
{
    /// <summary>
    /// The default uncompressed subblock size for streamed blocks. Small enough that several subblocks can be
    /// compressed in parallel with modest memory, and far larger than the match windows of the supported codecs,
    /// so the effect on compression ratio is negligible.
    /// </summary>
    public const int DefaultSubblockSize = 32 * 1024 * 1024;

    private const int ChunkSize = 1024 * 1024;

    private readonly ImageDataSource _source;
    private readonly CompressionCodec _codec;
    private readonly int _itemSize;
    private readonly int _subblockSize;
    private readonly ChecksumAlgorithm _checksumAlgorithm;
    private readonly string? _tempDirectory;

    public StreamedOutputBlock(
        ImageDataSource source,
        CompressionCodec codec,
        int itemSize,
        bool shuffleBytes,
        int subblockSize,
        ChecksumAlgorithm checksumAlgorithm,
        string? tempDirectory)
    {
        _source = source;
        _codec = codec;
        _itemSize = codec != CompressionCodec.None && shuffleBytes ? itemSize : 1;
        _subblockSize = subblockSize;
        _checksumAlgorithm = checksumAlgorithm;
        _tempDirectory = tempDirectory;

        CompressionAttribute = IsCompressed
            ? OutputBlock.FormatCompressionAttribute(codec, source.Length, shuffleBytes, itemSize)
            : string.Empty;
    }

    public string AttachmentPosition { get; set; } = Guid.NewGuid().ToString();

    public string SizeToken { get; } = Guid.NewGuid().ToString("N");

    public string SubblocksToken { get; } = Guid.NewGuid().ToString("N");

    public string ChecksumToken { get; } = Guid.NewGuid().ToString("N");

    public bool IsCompressed => _codec != CompressionCodec.None;

    public bool HasChecksum => _checksumAlgorithm != ChecksumAlgorithm.None;

    public string CompressionAttribute { get; }

    /// <summary>
    /// Gets the largest size the block can have: stored subblocks never exceed their uncompressed size.
    /// </summary>
    public long MaxBlockSize => _source.Length;

    public string WorstCaseSubblocksAttribute => string.Join(':', UncompressedSubblockSizes().Select(size => $"{size},{size}"));

    public string WorstCaseChecksumAttribute => HasChecksum
        ? $"{XisfUtil.ChecksumAlgorithmName(_checksumAlgorithm)}:{new string('0', XisfUtil.ChecksumHexLength(_checksumAlgorithm))}"
        : string.Empty;

    public long StoredSize { get; private set; }

    public string SubblocksAttribute { get; private set; } = string.Empty;

    public string ChecksumAttribute { get; private set; } = string.Empty;

    public async Task WriteDataAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var hash = HasChecksum ? XisfUtil.CreateIncrementalHash(_checksumAlgorithm) : null;

        if (IsCompressed)
        {
            await WriteCompressedAsync(stream, hash, cancellationToken);
        }
        else
        {
            await WriteUncompressedAsync(stream, hash, cancellationToken);
        }

        if (hash is not null)
        {
            ChecksumAttribute = $"{XisfUtil.ChecksumAlgorithmName(_checksumAlgorithm)}:{XisfUtil.ToLowerHex(hash.GetCurrentHash())}";
        }
    }

    private IEnumerable<long> UncompressedSubblockSizes()
    {
        for (long offset = 0; offset < _source.Length; offset += _subblockSize)
        {
            yield return Math.Min(_subblockSize, _source.Length - offset);
        }
    }

    private async Task WriteUncompressedAsync(Stream stream, System.Security.Cryptography.IncrementalHash? hash, CancellationToken cancellationToken)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);

        try
        {
            long remaining = _source.Length;
            while (remaining > 0)
            {
                int count = (int)Math.Min(remaining, ChunkSize);
                await _source.ReadExactlyAsync(buffer.AsMemory(0, count), cancellationToken);
                hash?.AppendData(buffer, 0, count);
                await stream.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                remaining -= count;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        StoredSize = _source.Length;
    }

    /// <remarks>
    /// Byte shuffling places byte <c>j</c> of every item before byte <c>j + 1</c> of any item, so the shuffled block
    /// is produced one plane at a time with a full pass over the source per plane. A source that cannot be read
    /// twice is copied to a temporary file first.
    /// </remarks>
    private async Task WriteCompressedAsync(Stream stream, System.Security.Cryptography.IncrementalHash? hash, CancellationToken cancellationToken)
    {
        int k = _itemSize;
        if (_source.Length % k != 0)
            throw new XisfException($"Image data of {_source.Length} bytes is not a whole number of {k}-byte items.");

        ImageDataSource source = _source;
        ImageDataSource? tempSource = null;

        if (k > 1 && !source.CanRewind)
        {
            tempSource = await CopyToTempFileAsync(source, cancellationToken);
            source = tempSource;
        }

        int chunkItems = ChunkSize / k;
        byte[] chunk = ArrayPool<byte>.Shared.Rent(chunkItems * k);
        byte[] plane = ArrayPool<byte>.Shared.Rent(chunkItems);
        byte[]? subblock = ArrayPool<byte>.Shared.Rent(_subblockSize);
        int fill = 0;

        var pipeline = new SubblockCompressionPipeline(stream, _codec, hash, Math.Max(1, Environment.ProcessorCount));

        try
        {
            long itemCount = source.Length / k;

            for (int j = 0; j < k; j++)
            {
                if (j > 0)
                    await source.RewindAsync(cancellationToken);

                long remaining = itemCount;
                while (remaining > 0)
                {
                    int items = (int)Math.Min(remaining, chunkItems);
                    await source.ReadExactlyAsync(chunk.AsMemory(0, items * k), cancellationToken);

                    ReadOnlyMemory<byte> bytes = k == 1
                        ? chunk.AsMemory(0, items)
                        : ExtractPlane(chunk, items, k, j, plane);

                    while (!bytes.IsEmpty)
                    {
                        int count = Math.Min(bytes.Length, _subblockSize - fill);
                        bytes[..count].CopyTo(subblock!.AsMemory(fill));
                        fill += count;
                        bytes = bytes[count..];

                        if (fill == _subblockSize)
                        {
                            var full = subblock;
                            subblock = null;
                            await pipeline.SubmitAsync(full, fill, cancellationToken);
                            subblock = ArrayPool<byte>.Shared.Rent(_subblockSize);
                            fill = 0;
                        }
                    }

                    remaining -= items;
                }
            }

            if (fill > 0)
            {
                var last = subblock!;
                subblock = null;
                await pipeline.SubmitAsync(last, fill, cancellationToken);
            }

            await pipeline.CompleteAsync(cancellationToken);
        }
        finally
        {
            if (subblock is not null)
                ArrayPool<byte>.Shared.Return(subblock);
            ArrayPool<byte>.Shared.Return(chunk);
            ArrayPool<byte>.Shared.Return(plane);
            tempSource?.Dispose();
        }

        StoredSize = pipeline.StoredSize;
        SubblocksAttribute = string.Join(':', pipeline.Subblocks.Select(s => $"{s.Compressed},{s.Uncompressed}"));
    }

    private static ReadOnlyMemory<byte> ExtractPlane(byte[] chunk, int items, int itemSize, int byteIndex, byte[] plane)
    {
        ByteShuffle.ExtractPlane(chunk.AsSpan(0, items * itemSize), itemSize, byteIndex, plane);
        return plane.AsMemory(0, items);
    }

    private async Task<ImageDataSource> CopyToTempFileAsync(ImageDataSource source, CancellationToken cancellationToken)
    {
        string path = Path.Combine(_tempDirectory ?? Path.GetTempPath(), $"xisfsharp-{Guid.NewGuid():N}.tmp");
        var tempFile = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 0,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);

        try
        {
            long remaining = source.Length;
            while (remaining > 0)
            {
                int count = (int)Math.Min(remaining, ChunkSize);
                await source.ReadExactlyAsync(buffer.AsMemory(0, count), cancellationToken);
                await tempFile.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                remaining -= count;
            }

            tempFile.Position = 0;
            return new StreamImageDataSource(tempFile, source.Length, ownsStream: true);
        }
        catch
        {
            await tempFile.DisposeAsync();
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
