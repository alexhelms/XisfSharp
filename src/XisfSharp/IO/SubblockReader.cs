using System.Buffers;
using System.IO.Compression;
using ZstdSharp;

namespace XisfSharp.IO;

/// <summary>
/// Reads the uncompressed contents of a block sequentially, starting at any offset.
/// </summary>
/// <remarks>
/// Positioning uses the subblock table: decoding starts at the subblock containing the requested offset
/// and discards the bytes before it, then continues across subblock boundaries.
/// </remarks>
internal sealed class SubblockReader : IDisposable
{
    private const int DiscardBufferSize = 1024 * 1024;

    private readonly PositionalSource _source;
    private readonly long _sourcePosition;
    private readonly SubblockTable _subblocks;
    private readonly CompressionCodec _codec;
    private readonly Lz4SubblockCache? _lz4Cache;

    private int _index = -1;
    private long _offsetInSubblock;
    private Stream? _decoder;
    private byte[]? _lz4Data;

    private SubblockReader(PositionalSource source, long sourcePosition, SubblockTable subblocks, CompressionCodec codec, Lz4SubblockCache? lz4Cache)
    {
        _source = source;
        _sourcePosition = sourcePosition;
        _subblocks = subblocks;
        _codec = codec;
        _lz4Cache = lz4Cache;
    }

    /// <param name="source">The source holding the block as stored.</param>
    /// <param name="sourcePosition">Position of the block within <paramref name="source"/>.</param>
    /// <param name="subblocks">The subblock layout of the block.</param>
    /// <param name="codec">The codec of compressed subblocks, or <see cref="CompressionCodec.None"/>.</param>
    /// <param name="lz4Cache">Shared decoded subblocks, required for the LZ4 codecs.</param>
    /// <param name="offset">Offset into the uncompressed block of the first byte to read.</param>
    public static async ValueTask<SubblockReader> OpenAsync(
        PositionalSource source,
        long sourcePosition,
        SubblockTable subblocks,
        CompressionCodec codec,
        Lz4SubblockCache? lz4Cache,
        long offset,
        CancellationToken cancellationToken)
    {
        if (codec is CompressionCodec.Lz4 or CompressionCodec.Lz4Hc && lz4Cache is null)
            throw new ArgumentNullException(nameof(lz4Cache));

        var reader = new SubblockReader(source, sourcePosition, subblocks, codec, lz4Cache);

        try
        {
            if (offset < subblocks.UncompressedSize)
            {
                int index = subblocks.IndexOfUncompressedOffset(offset);
                await reader.OpenSubblockAsync(index, offset - subblocks[index].UncompressedOffset, cancellationToken);
            }
        }
        catch
        {
            reader.Dispose();
            throw;
        }

        return reader;
    }

    public async ValueTask ReadExactlyAsync(Memory<byte> destination, CancellationToken cancellationToken)
    {
        while (!destination.IsEmpty)
        {
            if (_index < 0)
                throw new EndOfStreamException("Read past the end of the data block.");

            long available = _subblocks[_index].UncompressedSize - _offsetInSubblock;
            if (available == 0)
            {
                if (_index + 1 >= _subblocks.Count)
                    throw new EndOfStreamException("Read past the end of the data block.");

                await OpenSubblockAsync(_index + 1, 0, cancellationToken);
                continue;
            }

            int count = (int)Math.Min(destination.Length, available);

            if (_lz4Data is not null)
            {
                _lz4Data.AsMemory((int)_offsetInSubblock, count).CopyTo(destination);
            }
            else
            {
                await _decoder!.ReadExactlyAsync(destination[..count], cancellationToken);
            }

            _offsetInSubblock += count;
            destination = destination[count..];
        }
    }

    public void Dispose()
    {
        CloseSubblock();
    }

    private async ValueTask OpenSubblockAsync(int index, long skip, CancellationToken cancellationToken)
    {
        CloseSubblock();

        var subblock = _subblocks[index];
        _index = index;
        _offsetInSubblock = skip;

        long compressedStart = _sourcePosition + subblock.CompressedOffset;

        if (subblock.IsStored || _codec == CompressionCodec.None)
        {
            _decoder = new StreamWindow(_source, compressedStart + skip, subblock.CompressedSize - skip);
            return;
        }

        switch (_codec)
        {
            case CompressionCodec.Zlib:
                _decoder = new ZLibStream(new StreamWindow(_source, compressedStart, subblock.CompressedSize), CompressionMode.Decompress);
                await DiscardAsync(_decoder, skip, cancellationToken);
                break;

            case CompressionCodec.Zstd:
                _decoder = new DecompressionStream(new StreamWindow(_source, compressedStart, subblock.CompressedSize), leaveOpen: false);
                await DiscardAsync(_decoder, skip, cancellationToken);
                break;

            case CompressionCodec.Lz4 or CompressionCodec.Lz4Hc:
                _lz4Data = await _lz4Cache!.AcquireAsync(index, cancellationToken);
                break;

            default:
                throw new NotSupportedException($"Compression codec {_codec} is not supported");
        }
    }

    private void CloseSubblock()
    {
        _decoder?.Dispose();
        _decoder = null;

        if (_lz4Data is not null)
        {
            _lz4Cache!.Release(_index);
            _lz4Data = null;
        }
    }

    private static async ValueTask DiscardAsync(Stream stream, long count, CancellationToken cancellationToken)
    {
        if (count == 0)
            return;

        byte[] scratch = ArrayPool<byte>.Shared.Rent((int)Math.Min(count, DiscardBufferSize));

        try
        {
            while (count > 0)
            {
                int chunk = (int)Math.Min(count, scratch.Length);
                await stream.ReadExactlyAsync(scratch.AsMemory(0, chunk), cancellationToken);
                count -= chunk;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(scratch);
        }
    }
}
