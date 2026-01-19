using XisfSharp.IO;

namespace XisfSharp;

public class XisfFile : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream;
    private readonly bool _leaveOpen;
    private readonly XisfReader _reader;
    
    private bool _disposed;

    public int ImageCount => _reader.Images.Count;

    public XisfPropertyCollection Properties => _reader.Properties;

    public XisfFile(Stream stream)
        : this(stream, false)
    {
    }

    public XisfFile(Stream stream, bool leaveOpen)
    {
        ArgumentNullException.ThrowIfNull(stream);

        _stream = stream;
        _leaveOpen = leaveOpen;

        _reader = new XisfReader(stream);
    }

    public static Task<XisfImage> ReadImageAsync(string filename, CancellationToken cancellationToken = default)
        => ReadImageAsync(filename, 0, cancellationToken);

    public static async Task<XisfImage> ReadImageAsync(string filename, int index, CancellationToken cancellationToken = default)
    {
        using var fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
        using var xisfFile = new XisfFile(fileStream);
        await xisfFile.OpenAsync(cancellationToken);

        if (xisfFile.ImageCount == 0)
            throw new XisfException("XISF file contains no images.");

        return await xisfFile.GetImageAsync(index, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (!_leaveOpen)
            _stream?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (!_leaveOpen)
            await _stream.DisposeAsync();

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    public Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return _reader.ReadAsync(cancellationToken);
    }

    public Task<XisfImage> GetImageAsync(int index, CancellationToken cancellationToken = default)
    {
        return _reader.ReadImageAsync(index, cancellationToken);
    }

    public Task<XisfImage?> GetThumbnailAsync(int index, CancellationToken cancellationToken = default)
    {
        if (index < 0 || index >= _reader.Images.Count)
            throw new IndexOutOfRangeException();

        var image = _reader.Images[index];

        return _reader.ReadThumbnailAsync(image, cancellationToken);
    }

    public Task<XisfImage?> GetThumbnailAsync(XisfImage image, CancellationToken cancellationToken = default)
    {
        return _reader.ReadThumbnailAsync(image, cancellationToken);
    }
}
