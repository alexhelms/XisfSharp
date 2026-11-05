using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace XisfSharp.IO;

internal class InputBlock
{
    private const int ChecksumChunkSize = 1024 * 1024;

    private Memory<byte> _data;
    private Memory<byte> _storedData;

    public long CompressedSize { get; set; }

    public long UncompressedSize { get; set; }

    public long AttachmentPosition { get; set; }

    public long AttachmentSize { get; set; }

    public CompressionCodec CompressionCodec { get; set; }

    public bool ByteShuffling { get; set; }

    public int ItemSize { get; set; }

    /// <summary>
    /// Gets or sets the subblock layout declared by the <c>subblocks</c> attribute, or null when the attribute is absent.
    /// </summary>
    public SubblockTable? DeclaredSubblocks { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the uncompressed data is serialized in big-endian byte order.
    /// </summary>
    public bool IsBigEndian { get; set; }

    /// <summary>
    /// Gets or sets the size in bytes of the elements stored in the block, used to convert the byte order.
    /// </summary>
    public int ElementSize { get; set; } = 1;

    public ChecksumAlgorithm ChecksumAlgorithm { get; set; }

    public string Checksum { get; set; } = string.Empty;

    public bool ChecksumVerified { get; private set; }

    /// <summary>
    /// Gets the decoded contents of the block once loaded, or data supplied with <see cref="SetData"/>.
    /// </summary>
    public ReadOnlyMemory<byte> Data => _data;

    public bool IsAttachment => AttachmentPosition > 0;

    public bool IsCompressed => CompressionCodec != CompressionCodec.None;

    public bool HasData => !_data.IsEmpty;

    /// <summary>
    /// Gets a value indicating whether the block's serialized bytes were embedded in the XISF header.
    /// </summary>
    public bool HasStoredData => !_storedData.IsEmpty;

    public bool HasChecksum => !string.IsNullOrEmpty(Checksum);

    public bool IsEmpty => AttachmentSize <= 0 && !HasData && !HasStoredData;

    /// <summary>
    /// Gets the size in bytes of the block as serialized.
    /// </summary>
    public long StoredSize => IsAttachment ? AttachmentSize : _storedData.Length;

    /// <summary>
    /// Gets the size in bytes of the block contents after decoding.
    /// </summary>
    public long DecodedSize
    {
        get
        {
            if (HasData)
                return _data.Length;

            return IsCompressed ? UncompressedSize : StoredSize;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the decoded block fits in a single managed buffer.
    /// </summary>
    public bool CanLoadIntoMemory => DecodedSize <= Array.MaxLength;

    /// <summary>
    /// Gets the subblock layout of a compressed block, validated against the declared block sizes.
    /// </summary>
    public SubblockTable GetSubblocks()
    {
        if (!IsCompressed)
            throw new InvalidOperationException("Block is not compressed");

        var subblocks = DeclaredSubblocks ?? SubblockTable.Single(CompressedSize, UncompressedSize);
        subblocks.Validate(CompressedSize, UncompressedSize);
        return subblocks;
    }

    public Task LoadData(Stream stream, CancellationToken cancellationToken)
    {
        return LoadData(stream, BlockDecodeOptions.Default, cancellationToken);
    }

    public async Task LoadData(Stream stream, BlockDecodeOptions options, CancellationToken cancellationToken)
    {
        if (HasData || IsEmpty)
            return;

        if (!CanLoadIntoMemory)
            throw new XisfException(
                $"Data block of {DecodedSize} bytes exceeds the maximum in-memory size of {Array.MaxLength} bytes.");

        await using var decoded = await OpenDecodedStreamAsync(stream, options, cancellationToken);

        byte[] data = GC.AllocateUninitializedArray<byte>((int)decoded.Length);
        await decoded.ReadExactlyAsync(data, cancellationToken);

        _data = data;
        _storedData = default;
    }

    /// <summary>
    /// Opens a stream over the decoded contents of the block without loading it into memory.
    /// </summary>
    /// <param name="stream">The XISF unit containing the block, used for attachments.</param>
    /// <param name="options">Decoding parameters.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public Task<DecodedBlockStream> OpenDecodedStreamAsync(Stream stream, BlockDecodeOptions options, CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream must support seeking to read data blocks");

        if (IsAttachment)
            return DecodedBlockStream.OpenAsync(this, stream, AttachmentPosition, options, cancellationToken);

        if (HasStoredData)
        {
            var bytes = MemoryMarshal.TryGetArray<byte>(_storedData, out var segment)
                ? segment
                : new ArraySegment<byte>(_storedData.ToArray());

            var storedStream = new MemoryStream(bytes.Array!, bytes.Offset, bytes.Count, writable: false);
            return DecodedBlockStream.OpenAsync(this, storedStream, 0, options, cancellationToken);
        }

        throw new InvalidOperationException("Block has no stored data to decode");
    }

    /// <summary>
    /// Sets decoded data directly, bypassing the stored representation.
    /// </summary>
    public void SetData(Memory<byte> data)
    {
        _data = data;
    }

    /// <summary>
    /// Sets the serialized bytes of a block embedded in the XISF header and verifies its checksum.
    /// </summary>
    public void SetStoredData(Memory<byte> data)
    {
        _storedData = data;

        if (IsCompressed)
            CompressedSize = data.Length;

        VerifyChecksum();
    }

    /// <summary>
    /// Verifies the checksum of the block as stored, reading attachments from <paramref name="stream"/>.
    /// </summary>
    public async Task VerifyChecksumAsync(Stream stream, CancellationToken cancellationToken)
    {
        if (!HasChecksum)
            return;

        if (ChecksumVerified)
            return;

        if (!IsAttachment)
        {
            VerifyChecksum();
            return;
        }

        if (!stream.CanSeek)
            throw new InvalidOperationException("Stream must support seeking to verify checksum");

        // The digest covers the block as stored, which is the compressed data for compressed blocks.
        using var hash = XisfUtil.CreateIncrementalHash(ChecksumAlgorithm);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(ChecksumChunkSize);

        try
        {
            stream.Seek(AttachmentPosition, SeekOrigin.Begin);
            long remaining = AttachmentSize;

            while (remaining > 0)
            {
                int chunkSize = (int)Math.Min(remaining, ChecksumChunkSize);
                await stream.ReadExactlyAsync(buffer.AsMemory(0, chunkSize), cancellationToken);
                hash.AppendData(buffer, 0, chunkSize);
                remaining -= chunkSize;
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        CompareAndAssertChecksum(hash.GetCurrentHash());
    }

    public void VerifyChecksum()
    {
        if (!HasChecksum)
            return;

        if (ChecksumVerified)
            return;

        if (IsAttachment)
            throw new InvalidOperationException("Cannot verify attachment checksum without stream");

        if (!HasStoredData)
            throw new InvalidOperationException("Block has no data to verify");

        using var hash = XisfUtil.CreateIncrementalHash(ChecksumAlgorithm);
        hash.AppendData(_storedData.Span);
        CompareAndAssertChecksum(hash.GetCurrentHash());
    }

    private void CompareAndAssertChecksum(byte[] blockChecksumBytes)
    {
        if (blockChecksumBytes.Length == 0)
            throw new XisfException("Checksum is empty");

        var checksumBytes = Convert.FromHexString(Checksum);

        if (!checksumBytes.SequenceEqual(blockChecksumBytes))
        {
            var blockChecksum = Convert.ToHexString(blockChecksumBytes).ToLowerInvariant();
            throw new XisfException(
                $"Block {ChecksumAlgorithm} checksum mismatch: Expected {Checksum}, got {blockChecksum}");
        }

        ChecksumVerified = true;
    }
}
