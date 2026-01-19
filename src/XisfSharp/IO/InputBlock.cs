using System.Security.Cryptography;

namespace XisfSharp.IO;

internal class InputBlock
{
    private Memory<byte> _data;
    private Memory<byte> _compressedData;

    public int CompressedSize { get; set; }

    public int UncompressedSize { get; set; }

    public long AttachmentPosition { get; set; }

    public int AttachmentSize { get; set; }

    public CompressionCodec CompressionCodec { get; set; }

    public bool ByteShuffling { get; set; }

    public int ItemSize { get; set; }

    public ChecksumAlgorithm ChecksumAlgorithm { get; set; }

    public string Checksum { get; set; } = string.Empty;

    public bool ChecksumVerified { get; private set; }

    public ReadOnlyMemory<byte> Data => _data;

    public ReadOnlyMemory<byte> CompressedData => _compressedData;

    public bool IsValid => AttachmentPosition > 0 || HasData || HasCompressedData;

    public bool IsAttachment => AttachmentPosition > 0;

    public bool IsCompressed => CompressionCodec != CompressionCodec.None;

    public bool HasData => !Data.IsEmpty;

    public bool HasCompressedData => !CompressedData.IsEmpty;

    public bool HasChecksum => !string.IsNullOrEmpty(Checksum);

    public bool IsEmpty => AttachmentSize <= 0 && !HasData && !HasCompressedData;

    public int DataSize
    {
        get
        {
            if (IsEmpty)
                return 0;

            if (HasData)
                return Data.Length;

            if (IsCompressed)
                return CompressedData.Length;

            return AttachmentSize;
        }
    }

    public async Task LoadData(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream must support seeking to load attachment data");

        // TODO: Handle data larger than 2GB (max num of elements in C# array)
        if (AttachmentSize >= int.MaxValue)
            throw new XisfException("Attachments larger than 2GB are not supported");

        await VerifyChecksum(stream, cancellationToken);

        if (!HasData)
        {
            if (IsCompressed)
            {
                await Uncompress(stream, cancellationToken);
            }
            else
            {
                _data = new byte[AttachmentSize];
                stream.Seek(AttachmentPosition, SeekOrigin.Begin);
                await stream.ReadExactlyAsync(_data, cancellationToken);
            }
        }

        // TODO: Byte order
    }

    public void SetData(Memory<byte> data)
    {
        _data = data;
    }

    public void SetCompressedData(Memory<byte> data)
    {
        _compressedData = data;
        CompressedSize = data.Length;
        VerifyChecksum();
    }

    private IncrementalHash CreateHash()
    {
        var hashName = ChecksumAlgorithm switch
        {
            ChecksumAlgorithm.SHA1 => HashAlgorithmName.SHA1,
            ChecksumAlgorithm.SHA256 => HashAlgorithmName.SHA256,
            ChecksumAlgorithm.SHA512 => HashAlgorithmName.SHA512,
            ChecksumAlgorithm.SHA3_256 => HashAlgorithmName.SHA3_256,
            ChecksumAlgorithm.SHA3_512 => HashAlgorithmName.SHA3_512,
            _ => throw new InvalidOperationException($"Unsupported checksum algorithm: {ChecksumAlgorithm}"),
        };

        return IncrementalHash.CreateHash(hashName);
    }

    private async Task VerifyChecksum(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream must support seeking to verify checksum");

        if (!HasChecksum)
            return;

        if (ChecksumVerified)
            return;

        using var hash = CreateHash();
        byte[] blockChecksumBytes = [];

        if (IsAttachment)
        {
            const int ChunkSize = 4096;
            byte[] buffer = new byte[ChunkSize];
            long numberOfChunks = AttachmentSize / ChunkSize;
            long lastChunkSize = AttachmentSize % ChunkSize;

            stream.Seek(AttachmentPosition, SeekOrigin.Begin);
            for (int i = 0; i <= numberOfChunks; i++)
            {
                long thisChunkSize = i < numberOfChunks ? ChunkSize : lastChunkSize;
                if (thisChunkSize > 0)
                {
                    var chunk = buffer[..(int)thisChunkSize];
                    await stream.ReadExactlyAsync(chunk, cancellationToken);
                    hash.AppendData(chunk);
                }
            }

            blockChecksumBytes = hash.GetCurrentHash();
        }
        else
        {
            VerifyChecksum();
        }

        CompareAndAssertChecksum(blockChecksumBytes);
    }

    public void VerifyChecksum()
    {
        if (!HasChecksum)
            return;

        if (ChecksumVerified)
            return;

        if (IsAttachment)
            throw new InvalidOperationException("Cannot verify attachment checksum without stream");

        using var hash = CreateHash();
        byte[] blockChecksumBytes = [];

        if (HasCompressedData)
        {
            hash.AppendData(CompressedData.Span);
            blockChecksumBytes = hash.GetCurrentHash();
        }
        else if (HasData)
        {
            hash.AppendData(Data.Span);
        }
        else
        {
            throw new InvalidOperationException();
        }

        CompareAndAssertChecksum(blockChecksumBytes);
    }

    private void CompareAndAssertChecksum(byte[] blockChecksumBytes)
    {
        ChecksumVerified = true;

        if (blockChecksumBytes.Length == 0)
            throw new XisfException("Checksum is empty");

        var checksumBytes = Convert.FromHexString(Checksum);

        if (!checksumBytes.SequenceEqual(blockChecksumBytes))
        {
            var blockChecksum = Convert.ToHexString(blockChecksumBytes).ToLowerInvariant();
            throw new XisfException(
                $"Block {ChecksumAlgorithm} checksum mismatch: Expected {Checksum}, got {blockChecksum}");
        }
    }

    private async Task Uncompress(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream must support seeking to uncompress attachment data");

        if (IsEmpty || !IsCompressed)
            throw new InvalidOperationException();

        if (!HasData)
        {
            await LoadCompressedData(stream, cancellationToken);

            byte[] uncompressedData = XisfUtil.Decompress(CompressedData.Span, UncompressedSize, CompressionCodec);

            if (ByteShuffling)
            {
                XisfUtil.InPlaceUnshuffle(uncompressedData.AsSpan(), ItemSize);
            }

            _data = uncompressedData;
            _compressedData = Array.Empty<byte>();
        }

        // TODO: Byte order
    }

    private async Task LoadCompressedData(Stream stream, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream must support seeking to load compressed attachment data");

        if (IsEmpty || !IsCompressed)
            throw new InvalidOperationException();

        await VerifyChecksum(stream, cancellationToken);

        if (!HasCompressedData)
        {
            stream.Seek(AttachmentPosition, SeekOrigin.Begin);
            byte[] compressedData = new byte[CompressedSize];
            await stream.ReadExactlyAsync(compressedData, cancellationToken);
            _compressedData = compressedData;
        }
    }
}
