using System.Buffers;

namespace XisfSharp.IO;

/// <summary>
/// A read-only stream of the decoded contents of a data block: decompressed, unshuffled and in native byte order.
/// </summary>
/// <remarks>
/// Byte shuffling is applied to the whole block before it is divided into subblocks, so byte <c>j</c> of item
/// <c>i</c> is stored at offset <c>j * itemCount + i</c> of the uncompressed block. The stream reads the block
/// through one lane per byte of an item, each starting at its own shuffled plane, and interleaves the lanes to
/// rebuild the items. Unshuffled blocks use a single lane. Lanes decode concurrently, and the next chunk is
/// decoded in the background while the caller consumes the current one.
/// </remarks>
internal sealed class DecodedBlockStream : Stream
{
    private readonly SubblockReader[] _lanes;
    private readonly SubblockReader? _tailReader;
    private readonly byte[][] _laneBuffers;
    private readonly CancellationTokenSource _readAheadCancellation = new();
    private byte[] _staging;
    private byte[] _nextStaging;
    private Task<int>? _readAhead;
    private readonly int _itemSize;
    private readonly long _itemCount;
    private readonly int _itemsPerChunk;
    private readonly int _tailLength;
    private readonly int _elementSize;
    private readonly bool _reverseByteOrder;
    private readonly Stream? _tempFile;
    private readonly long _length;

    private long _itemsDecoded;
    private bool _tailDecoded;
    private int _stagingOffset;
    private int _stagingCount;
    private long _position;
    private bool _disposed;

    private DecodedBlockStream(
        SubblockReader[] lanes,
        SubblockReader? tailReader,
        int itemSize,
        long length,
        int chunkSize,
        int elementSize,
        bool reverseByteOrder,
        Stream? tempFile)
    {
        _lanes = lanes;
        _tailReader = tailReader;
        _itemSize = itemSize;
        _length = length;
        _itemCount = length / itemSize;
        _tailLength = (int)(length % itemSize);
        _elementSize = elementSize;
        _reverseByteOrder = reverseByteOrder;
        _tempFile = tempFile;

        // A whole number of 8-byte groups per chunk keeps every chunk aligned to any element size.
        _itemsPerChunk = chunkSize / (itemSize * 8) * 8;
        _staging = ArrayPool<byte>.Shared.Rent(_itemsPerChunk * itemSize);
        _nextStaging = ArrayPool<byte>.Shared.Rent(_itemsPerChunk * itemSize);
        _laneBuffers = itemSize == 1
            ? []
            : Enumerable.Range(0, itemSize).Select(_ => ArrayPool<byte>.Shared.Rent(_itemsPerChunk)).ToArray();
    }

    /// <summary>
    /// Gets a value indicating whether the block was decompressed to a temporary file before being unshuffled.
    /// </summary>
    public bool UsesTempFile => _tempFile is not null;

    public override bool CanRead => !_disposed;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Opens a decoded view of <paramref name="block"/> after verifying its checksum.
    /// </summary>
    /// <param name="block">The block to decode.</param>
    /// <param name="source">The stream holding the block as stored.</param>
    /// <param name="sourcePosition">Position of the block within <paramref name="source"/>.</param>
    /// <param name="options">Decoding parameters.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public static async Task<DecodedBlockStream> OpenAsync(
        InputBlock block,
        Stream source,
        long sourcePosition,
        BlockDecodeOptions options,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.ChunkSize, BlockDecodeOptions.MinimumChunkSize);

        // The checksum covers the stored bytes and must be verified before anything is decompressed.
        await block.VerifyChecksumAsync(source, cancellationToken);

        var codec = block.IsCompressed ? block.CompressionCodec : CompressionCodec.None;
        var subblocks = block.IsCompressed
            ? block.GetSubblocks()
            : SubblockTable.Single(block.StoredSize, block.StoredSize);

        long length = subblocks.UncompressedSize;
        int itemSize = block.ByteShuffling ? block.ItemSize : 1;
        int elementSize = block.ElementSize;
        bool reverseByteOrder = elementSize > 1 && block.IsBigEndian == BitConverter.IsLittleEndian;

        if (reverseByteOrder && (length % elementSize != 0 || length % itemSize != 0))
            throw new XisfException($"Data block of {length} bytes cannot be converted from big-endian {elementSize}-byte elements.");

        Stream? tempFile = null;
        Task<SubblockReader>[] laneOpens = [];
        SubblockReader? tailReader = null;

        try
        {
            var positional = PositionalSource.Create(source);

            if (NeedsTempFile(subblocks, codec, itemSize, options))
            {
                tempFile = await DecompressToTempFileAsync(positional, sourcePosition, subblocks, codec, options, cancellationToken);
                positional = PositionalSource.Create(tempFile);
                sourcePosition = 0;
                subblocks = SubblockTable.Single(length, length);
                codec = CompressionCodec.None;
            }

            var lz4Cache = codec is CompressionCodec.Lz4 or CompressionCodec.Lz4Hc
                ? new Lz4SubblockCache(positional, sourcePosition, subblocks, codec)
                : null;

            // Opening a lane may decode and discard everything before its plane, so lanes open concurrently.
            long itemCount = length / itemSize;
            laneOpens = Enumerable.Range(0, itemSize)
                .Select(j => Task.Run(() => SubblockReader.OpenAsync(positional, sourcePosition, subblocks, codec, lz4Cache, j * itemCount, cancellationToken).AsTask(), cancellationToken))
                .ToArray();

            var lanes = await Task.WhenAll(laneOpens);

            // Bytes that do not form a complete item follow the shuffled planes unshuffled.
            if (length % itemSize != 0)
            {
                tailReader = await SubblockReader.OpenAsync(positional, sourcePosition, subblocks, codec, lz4Cache, itemCount * itemSize, cancellationToken);
            }

            return new DecodedBlockStream(lanes, tailReader, itemSize, length, options.ChunkSize, elementSize, reverseByteOrder, tempFile);
        }
        catch
        {
            foreach (var open in laneOpens)
            {
                if (open.IsCompletedSuccessfully)
                    open.Result.Dispose();
            }

            tailReader?.Dispose();
            tempFile?.Dispose();
            throw;
        }
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        throw new NotSupportedException("Decoded data blocks can only be read asynchronously.");

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (buffer.IsEmpty)
            return 0;

        if (_stagingOffset == _stagingCount)
        {
            int produced;

            if (_readAhead is not null)
            {
                produced = await _readAhead.WaitAsync(cancellationToken);
                _readAhead = null;
                (_staging, _nextStaging) = (_nextStaging, _staging);
            }
            else
            {
                produced = await DecodeChunkAsync(_staging, cancellationToken);
            }

            if (produced == 0)
                return 0;

            _stagingOffset = 0;
            _stagingCount = produced;

            // Decoding the next chunk while the caller consumes this one overlaps decompression with the caller's work.
            if (HasMoreChunks)
            {
                var target = _nextStaging;
                var token = _readAheadCancellation.Token;
                _readAhead = Task.Run(() => DecodeChunkAsync(target, token).AsTask(), token);
            }
        }

        int count = Math.Min(buffer.Length, _stagingCount - _stagingOffset);
        _staging.AsMemory(_stagingOffset, count).CopyTo(buffer);
        _stagingOffset += count;
        _position += count;
        return count;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override async ValueTask DisposeAsync()
    {
        await StopReadAheadAsync();
        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            // Lanes and buffers are released only once no background decode is using them.
            StopReadAheadAsync().AsTask().GetAwaiter().GetResult();
            _disposed = true;

            foreach (var lane in _lanes)
                lane.Dispose();
            _tailReader?.Dispose();
            _tempFile?.Dispose();

            ArrayPool<byte>.Shared.Return(_staging);
            ArrayPool<byte>.Shared.Return(_nextStaging);
            _readAheadCancellation.Dispose();
            foreach (var laneBuffer in _laneBuffers)
                ArrayPool<byte>.Shared.Return(laneBuffer);
        }

        base.Dispose(disposing);
    }

    private bool HasMoreChunks => _itemsDecoded < _itemCount || (_tailLength > 0 && !_tailDecoded);

    private async ValueTask StopReadAheadAsync()
    {
        if (_readAhead is null)
            return;

        _readAheadCancellation.Cancel();

        try
        {
            await _readAhead;
        }
        catch
        {
            // The stream is being disposed, so a failed or cancelled read-ahead has no reader left to report to.
        }

        _readAhead = null;
    }

    /// <summary>
    /// Decodes the next chunk into <paramref name="target"/>.
    /// </summary>
    /// <returns>The number of bytes produced, or 0 once the block is exhausted.</returns>
    private async ValueTask<int> DecodeChunkAsync(byte[] target, CancellationToken cancellationToken)
    {
        int produced;
        long remainingItems = _itemCount - _itemsDecoded;

        if (remainingItems > 0)
        {
            int items = (int)Math.Min(remainingItems, _itemsPerChunk);
            produced = items * _itemSize;

            if (_itemSize == 1)
            {
                await _lanes[0].ReadExactlyAsync(target.AsMemory(0, items), cancellationToken);
            }
            else
            {
                var reads = new Task[_itemSize];
                for (int j = 0; j < _itemSize; j++)
                {
                    var lane = _lanes[j];
                    var laneBuffer = _laneBuffers[j];
                    reads[j] = Task.Run(() => lane.ReadExactlyAsync(laneBuffer.AsMemory(0, items), cancellationToken).AsTask(), cancellationToken);
                }

                await Task.WhenAll(reads);
                ByteShuffle.Interleave(_laneBuffers, items, target);
            }

            if (_reverseByteOrder)
            {
                EndianConverter.ReverseEndiannessInPlace(target.AsSpan(0, produced), _elementSize);
            }

            _itemsDecoded += items;
        }
        else if (_tailLength > 0 && !_tailDecoded)
        {
            await _tailReader!.ReadExactlyAsync(target.AsMemory(0, _tailLength), cancellationToken);
            _tailDecoded = true;
            produced = _tailLength;
        }
        else
        {
            produced = 0;
        }

        return produced;
    }

    private static bool NeedsTempFile(SubblockTable subblocks, CompressionCodec codec, int itemSize, BlockDecodeOptions options)
    {
        if (itemSize == 1 || codec is not (CompressionCodec.Lz4 or CompressionCodec.Lz4Hc))
            return false;

        // Each lane may hold a different decoded subblock; sequential decoding needs only one.
        long laneMemory = Math.Min(itemSize, subblocks.Count) * subblocks.MaxUncompressedSubblockSize;
        return laneMemory > Math.Max(options.LaneMemoryLimit, subblocks.MaxUncompressedSubblockSize);
    }

    private static async Task<Stream> DecompressToTempFileAsync(
        PositionalSource source,
        long sourcePosition,
        SubblockTable subblocks,
        CompressionCodec codec,
        BlockDecodeOptions options,
        CancellationToken cancellationToken)
    {
        string path = Path.Combine(options.TempDirectory ?? Path.GetTempPath(), $"xisfsharp-{Guid.NewGuid():N}.tmp");
        var tempFile = new FileStream(
            path,
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 0,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        byte[] buffer = ArrayPool<byte>.Shared.Rent(options.ChunkSize);

        try
        {
            var lz4Cache = new Lz4SubblockCache(source, sourcePosition, subblocks, codec);
            using var reader = await SubblockReader.OpenAsync(source, sourcePosition, subblocks, codec, lz4Cache, 0, cancellationToken);

            long remaining = subblocks.UncompressedSize;
            while (remaining > 0)
            {
                int count = (int)Math.Min(remaining, options.ChunkSize);
                await reader.ReadExactlyAsync(buffer.AsMemory(0, count), cancellationToken);
                await tempFile.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
                remaining -= count;
            }

            await tempFile.FlushAsync(cancellationToken);
            return tempFile;
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
