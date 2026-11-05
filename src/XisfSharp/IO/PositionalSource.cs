using Microsoft.Win32.SafeHandles;

namespace XisfSharp.IO;

/// <summary>
/// Reads bytes at explicit positions, so several readers can share one underlying stream.
/// </summary>
internal abstract class PositionalSource
{
    /// <summary>
    /// Reads up to <paramref name="buffer"/>.Length bytes starting at <paramref name="position"/>.
    /// </summary>
    /// <returns>The number of bytes read, or 0 at the end of the source.</returns>
    public abstract ValueTask<int> ReadAtAsync(long position, Memory<byte> buffer, CancellationToken cancellationToken);

    public async ValueTask ReadExactlyAtAsync(long position, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        while (!buffer.IsEmpty)
        {
            int read = await ReadAtAsync(position, buffer, cancellationToken);
            if (read == 0)
                throw new EndOfStreamException("Read past the end of the data block.");

            position += read;
            buffer = buffer[read..];
        }
    }

    /// <summary>
    /// Creates a source over <paramref name="stream"/>. File streams are read through their handle, which allows
    /// concurrent reads; other streams are read one request at a time.
    /// </summary>
    public static PositionalSource Create(Stream stream)
    {
        return stream is FileStream fileStream
            ? new FileHandleSource(fileStream.SafeFileHandle)
            : new SerializedStreamSource(stream);
    }

    private sealed class FileHandleSource(SafeFileHandle handle) : PositionalSource
    {
        public override ValueTask<int> ReadAtAsync(long position, Memory<byte> buffer, CancellationToken cancellationToken) =>
            RandomAccess.ReadAsync(handle, buffer, position, cancellationToken);
    }

    private sealed class SerializedStreamSource(Stream stream) : PositionalSource
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        public override async ValueTask<int> ReadAtAsync(long position, Memory<byte> buffer, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);

            try
            {
                stream.Position = position;
                return await stream.ReadAsync(buffer, cancellationToken);
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
