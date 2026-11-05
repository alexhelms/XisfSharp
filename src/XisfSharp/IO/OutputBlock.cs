namespace XisfSharp.IO;

internal class OutputBlock(ReadOnlyMemory<byte> data) : IOutputAttachment
{
    private ReadOnlyMemory<byte> _data = data;
    private Memory<byte> _compressedData;

    public string AttachmentPosition { get; set; } = Guid.NewGuid().ToString();

    public CompressionCodec CompressionCodec { get; private set; }

    public bool ShuffleBytes { get; private set; }

    public int ItemSize { get; private set; } = 1;

    public ChecksumAlgorithm ChecksumAlgorithm { get; private set; }

    public string Checksum { get; private set; } = string.Empty;

    public bool HasCompression => CompressionCodec != CompressionCodec.None;

    public bool HasData => _data.Length > 0;

    public bool HasCompressedData => _compressedData.Length > 0;

    public bool HasChecksum => !string.IsNullOrEmpty(Checksum);

    public long BlockSize
    {
        get
        {
            if (HasData)
                return _data.Length;

            if (HasCompressedData)
                return _compressedData.Length;

            return 0;
        }
    }

    public long UncompressedBlockSize { get; } = data.Length;

    public string GetEncodedData(DataEncoding encoding = DataEncoding.Base64)
    {
        if (encoding == DataEncoding.None)
            return string.Empty;

        ReadOnlySpan<byte> data = default;

        if (HasData)
        {
            data = _data.Span;
        }

        if (HasCompressedData)
        {
            data = _compressedData.Span;
        }

        if (!data.IsEmpty)
        {
            if (encoding == DataEncoding.Base64)
            {
                return Convert.ToBase64String(data);
            }
            else if (encoding == DataEncoding.Base16)
            {
#if NET10_0_OR_GREATER
                return Convert.ToHexStringLower(data);
#else
                return Convert.ToHexString(data).ToLowerInvariant();
#endif
            }
        }

        return string.Empty;
    }

    long IOutputAttachment.MaxBlockSize => BlockSize;

    public string GetCompressionAttribute()
    {
        return HasCompression
            ? FormatCompressionAttribute(CompressionCodec, UncompressedBlockSize, ShuffleBytes, ItemSize)
            : string.Empty;
    }

    /// <summary>
    /// Formats a <c>compression</c> attribute value: <c>codec:uncompressed-size</c>, with <c>+sh</c> and the item size when shuffled.
    /// </summary>
    public static string FormatCompressionAttribute(CompressionCodec codec, long uncompressedSize, bool shuffleBytes, int itemSize)
    {
        string name = codec switch
        {
            CompressionCodec.Zlib => "zlib",
            CompressionCodec.Lz4 => "lz4",
            CompressionCodec.Lz4Hc => "lz4hc",
            CompressionCodec.Zstd => "zstd",
            _ => throw new NotImplementedException($"Unsupported compression: {codec}")
        };

        return shuffleBytes && itemSize > 1
            ? $"{name}+sh:{uncompressedSize}:{itemSize}"
            : $"{name}:{uncompressedSize}";
    }

    /// <summary>
    /// The largest uncompressed subblock written: the LZ4 input limit, which also keeps every compressed subblock
    /// within a managed array. Blocks larger than this are always divided into subblocks.
    /// </summary>
    public const int MaxSubblockSize = 0x7E000000;

    /// <summary>
    /// Gets the compressed and uncompressed size of each subblock when the block was divided, or null for a single subblock.
    /// </summary>
    public IReadOnlyList<(long Compressed, long Uncompressed)>? Subblocks { get; private set; }

    public void CompressData(CompressionCodec codec, int itemSize, bool shuffleBytes)
    {
        CompressData(codec, itemSize, shuffleBytes, MaxSubblockSize);
    }

    /// <summary>
    /// Compresses the block, dividing it into independently compressed subblocks of at most <paramref name="subblockSize"/> bytes.
    /// </summary>
    /// <remarks>
    /// When the block is divided, a subblock that does not shrink is stored verbatim; readers recognize it by its
    /// equal compressed and uncompressed sizes. A block that is not divided keeps its compressed form even when it
    /// grows, unless it would be exactly as large as the original, which would be indistinguishable from stored data.
    /// </remarks>
    public void CompressData(CompressionCodec codec, int itemSize, bool shuffleBytes, int subblockSize)
    {
        // TODO: Compression level

        ArgumentOutOfRangeException.ThrowIfLessThan(subblockSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(subblockSize, MaxSubblockSize);

        if (!HasData)
            return;

        if (codec == CompressionCodec.None)
        {
            var tmp = new byte[_data.Length];
            _data.Span.CopyTo(tmp);
            _compressedData = tmp;
        }
        else
        {
            // Byte shuffling applies to the whole block before it is divided into subblocks.
            ReadOnlyMemory<byte> source = shuffleBytes && itemSize > 1
                ? XisfUtil.Shuffle(_data.Span, itemSize)
                : _data;

            int count = (int)((source.Length + (long)subblockSize - 1) / subblockSize);

            if (count == 1)
            {
                byte[] compressed = XisfUtil.Compress(source.Span, codec);

                if (compressed.Length == source.Length)
                {
                    CompressData(CompressionCodec.None, itemSize, shuffleBytes);
                    return;
                }

                _compressedData = compressed;
                Subblocks = null;
            }
            else
            {
                _compressedData = CompressSubblocks(source, codec, subblockSize, count);
            }

            _data = Array.Empty<byte>();
        }

        CompressionCodec = codec;
        ShuffleBytes = shuffleBytes;
        ItemSize = itemSize;
    }

    private byte[] CompressSubblocks(ReadOnlyMemory<byte> source, CompressionCodec codec, int subblockSize, int count)
    {
        var subblocks = new byte[count][];

        // Subblocks are independent, so they are compressed in parallel with single-threaded codecs.
        Parallel.For(0, count, i =>
        {
            int offset = i * subblockSize;
            var input = source.Slice(offset, Math.Min(subblockSize, source.Length - offset));
            byte[] compressed = XisfUtil.Compress(input.Span, codec, multithreaded: false);
            subblocks[i] = compressed.Length < input.Length ? compressed : input.ToArray();
        });

        // Every subblock is at most its uncompressed size, so the total never exceeds the original block.
        byte[] block = GC.AllocateUninitializedArray<byte>(subblocks.Sum(s => s.Length));
        var sizes = new (long Compressed, long Uncompressed)[count];
        int position = 0;

        for (int i = 0; i < count; i++)
        {
            subblocks[i].CopyTo(block, position);
            position += subblocks[i].Length;
            sizes[i] = (subblocks[i].Length, Math.Min(subblockSize, source.Length - i * subblockSize));
        }

        Subblocks = sizes;
        return block;
    }

    public string GetSubblocksAttribute()
    {
        // subblocks="c1,u1:c2,u2:...:cN,uN"
        if (Subblocks is null)
            return string.Empty;

        return string.Join(':', Subblocks.Select(s => $"{s.Compressed},{s.Uncompressed}"));
    }

    public void ComputeChecksum(ChecksumAlgorithm algorithm)
    {
        if (algorithm == ChecksumAlgorithm.None)
            return;

        ReadOnlySpan<byte> data = default;

        if (HasData)
        {
            data = _data.Span;
        }
        else if (HasCompressedData)
        {
            data = _compressedData.Span;
        }

        if (!data.IsEmpty)
        {
            ChecksumAlgorithm = algorithm;
            Checksum = XisfUtil.GetChecksumString(data, algorithm);
        }
    }

    public string GetChecksumAttribute()
    {
        // checksum="<algorithm>:<digest>"
        string value = string.Empty;

        if (HasChecksum)
        {
            string algorithm = ChecksumAlgorithm switch
            {
                ChecksumAlgorithm.SHA1 => "sha1",
                ChecksumAlgorithm.SHA256 => "sha256",
                ChecksumAlgorithm.SHA512 => "sha512",
                ChecksumAlgorithm.SHA3_256 => "sha3-256",
                ChecksumAlgorithm.SHA3_512 => "sha3-512",
                _ => throw new NotImplementedException($"Unsupported checksum algorithm: {ChecksumAlgorithm}")
            };

            value = $"{algorithm}:{Checksum}";
        }

        return value;
    }

    public async Task WriteDataAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (HasData)
        {
            await stream.WriteAsync(_data, cancellationToken);
        }
        else if (HasCompressedData)
        {
            await stream.WriteAsync(_compressedData, cancellationToken);
        }
    }
}
