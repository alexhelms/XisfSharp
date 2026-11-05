namespace XisfSharp.IO;

/// <summary>
/// A read-only, forward-only view over a range of a <see cref="PositionalSource"/>.
/// </summary>
internal sealed class StreamWindow : Stream
{
    private readonly PositionalSource _source;
    private readonly long _start;
    private readonly long _length;
    private long _position;

    public StreamWindow(PositionalSource source, long start, long length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        _source = source;
        _start = start;
        _length = length;
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int count = (int)Math.Min(buffer.Length, _length - _position);
        if (count <= 0)
            return 0;

        int read = await _source.ReadAtAsync(_start + _position, buffer[..count], cancellationToken);
        _position += read;
        return read;
    }

    public override void Flush()
    {
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
