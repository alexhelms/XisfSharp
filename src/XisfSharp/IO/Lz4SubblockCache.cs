namespace XisfSharp.IO;

/// <summary>
/// Decoded LZ4 subblocks shared by the lanes of one block.
/// </summary>
/// <remarks>
/// The LZ4 block format cannot be decoded incrementally, so a subblock is decoded in full the first time
/// a lane needs it and kept until every lane using it has moved on. Lanes may run concurrently; a subblock
/// requested by several lanes at once is decoded only once.
/// </remarks>
internal sealed class Lz4SubblockCache(PositionalSource source, long sourcePosition, SubblockTable subblocks, CompressionCodec codec)
{
    private readonly Dictionary<int, Entry> _entries = [];
    private readonly object _lock = new();

    public Task<byte[]> AcquireAsync(int index, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(index, out var entry))
            {
                entry.References++;
                return entry.Data;
            }

            var data = Task.Run(() => DecodeAsync(index, cancellationToken), cancellationToken);
            _entries[index] = new Entry(data);
            return data;
        }
    }

    public void Release(int index)
    {
        lock (_lock)
        {
            if (_entries.TryGetValue(index, out var entry) && --entry.References == 0)
            {
                _entries.Remove(index);
            }
        }
    }

    private async Task<byte[]> DecodeAsync(int index, CancellationToken cancellationToken)
    {
        var subblock = subblocks[index];
        byte[] compressed = GC.AllocateUninitializedArray<byte>((int)subblock.CompressedSize);
        await source.ReadExactlyAtAsync(sourcePosition + subblock.CompressedOffset, compressed, cancellationToken);

        byte[] data = GC.AllocateUninitializedArray<byte>((int)subblock.UncompressedSize);
        XisfUtil.DecompressSubblock(compressed, data, codec);
        return data;
    }

    private sealed class Entry(Task<byte[]> data)
    {
        public Task<byte[]> Data { get; } = data;

        public int References { get; set; } = 1;
    }
}
