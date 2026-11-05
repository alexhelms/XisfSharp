using System.Runtime.InteropServices;

namespace XisfSharp.IO;

/// <summary>
/// Supplies the pixel data of an image being written, in storage order and native byte order.
/// </summary>
internal abstract class ImageDataSource : IDisposable
{
    /// <summary>
    /// Gets the total number of bytes the source supplies.
    /// </summary>
    public abstract long Length { get; }

    /// <summary>
    /// Gets a value indicating whether the source can be read again from the start.
    /// </summary>
    public abstract bool CanRewind { get; }

    /// <summary>
    /// Fills <paramref name="destination"/> with the next bytes of the image data.
    /// </summary>
    public abstract ValueTask ReadExactlyAsync(Memory<byte> destination, CancellationToken cancellationToken);

    /// <summary>
    /// Restarts reading from the first byte of the image data.
    /// </summary>
    public abstract ValueTask RewindAsync(CancellationToken cancellationToken);

    public virtual void Dispose()
    {
    }
}

/// <summary>
/// Image data produced row by row by a caller-supplied delegate.
/// </summary>
internal sealed class RowImageDataSource<T> : ImageDataSource
    where T : unmanaged
{
    private readonly XisfRowSource<T> _source;
    private readonly int _height;
    private readonly int _passes;
    private readonly byte[] _row;
    private readonly long _length;

    private int _channel;
    private int _y;
    private int _rowOffset;

    public RowImageDataSource(XisfRowSource<T> source, XisfImage image)
    {
        bool planar = image.PixelStorage == PixelStorage.Planar;

        _source = source;
        _height = image.Height;
        _passes = planar ? image.Channels : 1;
        _row = new byte[checked((planar ? image.Width : image.Width * image.Channels) * System.Runtime.CompilerServices.Unsafe.SizeOf<T>())];
        _length = image.DataSize;
        _rowOffset = _row.Length;
    }

    public override long Length => _length;

    public override bool CanRewind => true;

    public override ValueTask ReadExactlyAsync(Memory<byte> destination, CancellationToken cancellationToken)
    {
        var span = destination.Span;

        while (!span.IsEmpty)
        {
            if (_rowOffset == _row.Length)
                NextRow();

            int count = Math.Min(span.Length, _row.Length - _rowOffset);
            _row.AsSpan(_rowOffset, count).CopyTo(span);
            _rowOffset += count;
            span = span[count..];
        }

        return ValueTask.CompletedTask;
    }

    public override ValueTask RewindAsync(CancellationToken cancellationToken)
    {
        _channel = 0;
        _y = 0;
        _rowOffset = _row.Length;
        return ValueTask.CompletedTask;
    }

    private void NextRow()
    {
        if (_channel >= _passes)
            throw new EndOfStreamException("Read past the last row of the image.");

        // Cleared so samples the delegate does not write are zero rather than left over from the previous row.
        Array.Clear(_row);
        _source(_channel, _y, MemoryMarshal.Cast<byte, T>(_row.AsSpan()));
        _rowOffset = 0;

        if (++_y == _height)
        {
            _y = 0;
            _channel++;
        }
    }
}

/// <summary>
/// Image data read from a caller-supplied stream, starting at its position when the source was created.
/// </summary>
internal sealed class StreamImageDataSource(Stream stream, long length, bool ownsStream) : ImageDataSource
{
    private readonly long _start = stream.CanSeek ? stream.Position : 0;
    private long _position;

    public override long Length => length;

    public override bool CanRewind => stream.CanSeek;

    public override async ValueTask ReadExactlyAsync(Memory<byte> destination, CancellationToken cancellationToken)
    {
        if (_position + destination.Length > length)
            throw new EndOfStreamException("Read past the end of the image data.");

        await stream.ReadExactlyAsync(destination, cancellationToken);
        _position += destination.Length;
    }

    public override ValueTask RewindAsync(CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
            throw new NotSupportedException("The image data stream cannot be read more than once.");

        stream.Position = _start;
        _position = 0;
        return ValueTask.CompletedTask;
    }

    public override void Dispose()
    {
        if (ownsStream)
            stream.Dispose();
    }
}
