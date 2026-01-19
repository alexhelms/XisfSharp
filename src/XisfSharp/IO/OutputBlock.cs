namespace XisfSharp.IO;

internal class OutputBlock(ReadOnlyMemory<byte> data)
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
                return Convert.ToBase64String(_data.Span);
            }
            else if (encoding == DataEncoding.Base16)
            {
#if NET10_0_OR_GREATER
                return Convert.ToHexStringLower(_data.Span);
#else
                return Convert.ToHexString(_data.Span).ToLowerInvariant();
#endif
            }
        }

        return string.Empty;
    }

    public string GetCompressionAttribute()
    {
        // compression="<codec>:<uncompressed-size>:<item-size>"
        string value = string.Empty;

        if (HasCompression)
        {
            string codec = CompressionCodec switch
            {
                CompressionCodec.Zlib => "zlib",
                CompressionCodec.Lz4 => "lz4",
                CompressionCodec.Lz4Hc => "lz4hc",
                CompressionCodec.Zstd => "zstd",
                _ => throw new NotImplementedException($"Unsupported compression: {ChecksumAlgorithm}")
            };

            if (ShuffleBytes && ItemSize > 1)
                codec += "+sh";

            value = $"{codec}:{UncompressedBlockSize}";

            if (ShuffleBytes && ItemSize > 1)
                value += $":{ItemSize}";
        }

        return value;
    }

    public void CompressData(CompressionCodec codec, int itemSize, bool shuffleBytes)
    {
        // TODO: Compression level

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
            if (shuffleBytes && itemSize > 1)
            {
                var shuffledData = XisfUtil.Shuffle(_data.Span, itemSize);
                _compressedData = XisfUtil.Compress(shuffledData, codec);
                _data = Array.Empty<byte>();
            }
            else
            {
                _compressedData = XisfUtil.Compress(_data.Span, codec);
                _data = Array.Empty<byte>();
            }
        }

        CompressionCodec = codec;
        ShuffleBytes = shuffleBytes;
        ItemSize = itemSize;
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
