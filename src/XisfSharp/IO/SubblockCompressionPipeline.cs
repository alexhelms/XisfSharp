using System.Buffers;
using System.Security.Cryptography;

namespace XisfSharp.IO;

/// <summary>
/// Compresses subblocks in parallel and writes them to the output in order.
/// </summary>
/// <remarks>
/// At most <c>maxInFlight</c> subblocks are held at once, which bounds memory to roughly twice that many
/// subblock buffers. A subblock that does not shrink is stored verbatim, which readers recognize by its
/// equal compressed and uncompressed sizes.
/// </remarks>
internal sealed class SubblockCompressionPipeline(Stream output, CompressionCodec codec, IncrementalHash? hash, int maxInFlight)
{
    private readonly Queue<(Task<byte[]> Compressed, byte[] Input, int Length)> _pending = new();
    private readonly List<(long Compressed, long Uncompressed)> _subblocks = [];

    public IReadOnlyList<(long Compressed, long Uncompressed)> Subblocks => _subblocks;

    public long StoredSize { get; private set; }

    /// <summary>
    /// Queues a subblock for compression. The pipeline returns <paramref name="input"/> to <see cref="ArrayPool{T}.Shared"/> once written.
    /// </summary>
    public async ValueTask SubmitAsync(byte[] input, int length, CancellationToken cancellationToken)
    {
        var compressed = Task.Run(() => XisfUtil.Compress(input.AsSpan(0, length), codec, multithreaded: false), cancellationToken);
        _pending.Enqueue((compressed, input, length));

        if (_pending.Count >= maxInFlight)
            await WriteOldestAsync(cancellationToken);
    }

    public async ValueTask CompleteAsync(CancellationToken cancellationToken)
    {
        while (_pending.Count > 0)
            await WriteOldestAsync(cancellationToken);
    }

    private async ValueTask WriteOldestAsync(CancellationToken cancellationToken)
    {
        var (task, input, length) = _pending.Dequeue();

        try
        {
            byte[] compressed = await task;
            ReadOnlyMemory<byte> stored = compressed.Length < length ? compressed : input.AsMemory(0, length);

            hash?.AppendData(stored.Span);
            await output.WriteAsync(stored, cancellationToken);

            _subblocks.Add((stored.Length, length));
            StoredSize += stored.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(input);
        }
    }
}
