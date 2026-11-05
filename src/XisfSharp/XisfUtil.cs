using K4os.Compression.LZ4;
using System.IO.Compression;
using System.Security.Cryptography;
using XisfSharp.IO;
using XisfSharp.Properties;
using ZstdSharp;

namespace XisfSharp;

internal static class XisfUtil
{
    internal static SampleFormat ToSampleFormat<T>()
        where T : unmanaged
    {
        return typeof(T) switch
        {
            var t when t == typeof(byte) => SampleFormat.UInt8,
            var t when t == typeof(ushort) => SampleFormat.UInt16,
            var t when t == typeof(uint) => SampleFormat.UInt32,
            var t when t == typeof(ulong) => SampleFormat.UInt64,
            var t when t == typeof(float) => SampleFormat.Float32,
            var t when t == typeof(double) => SampleFormat.Float64,
            _ => throw new XisfException($"Unsupported type: {typeof(T).FullName}")
        };
    }

    internal static int SampleFormatSize(SampleFormat sampleFormat)
    {
        return sampleFormat switch
        {
            SampleFormat.UInt16 => sizeof(ushort),
            SampleFormat.UInt32 => sizeof(int),
            SampleFormat.UInt64 => sizeof(long),
            SampleFormat.Float32 => sizeof(float),
            SampleFormat.Float64 => sizeof(double),
            _ => 1,
        };
    }

    internal static int GetBytesPerElement(XisfPropertyType type)
    {
        return type switch
        {
            XisfPropertyType.Boolean => sizeof(bool),
            XisfPropertyType.Int8 => sizeof(sbyte),
            XisfPropertyType.UInt8 => sizeof(byte),
            XisfPropertyType.Int16 => sizeof(short),
            XisfPropertyType.UInt16 => sizeof(ushort),
            XisfPropertyType.Int32 => sizeof(int),
            XisfPropertyType.UInt32 => sizeof(uint),
            XisfPropertyType.Int64 => sizeof(long),
            XisfPropertyType.UInt64 => sizeof(ulong),
            XisfPropertyType.Float32 => sizeof(float),
            XisfPropertyType.Float64 => sizeof(double),

            XisfPropertyType.I8Vector => sizeof(sbyte),
            XisfPropertyType.UI8Vector => sizeof(byte),
            XisfPropertyType.I16Vector => sizeof(short),
            XisfPropertyType.UI16Vector => sizeof(ushort),
            XisfPropertyType.I32Vector => sizeof(int),
            XisfPropertyType.UI32Vector => sizeof(uint),
            XisfPropertyType.I64Vector => sizeof(long),
            XisfPropertyType.UI64Vector => sizeof(ulong),
            XisfPropertyType.F32Vector => sizeof(float),
            XisfPropertyType.F64Vector => sizeof(double),

            XisfPropertyType.I8Matrix => sizeof(sbyte),
            XisfPropertyType.UI8Matrix => sizeof(byte),
            XisfPropertyType.I16Matrix => sizeof(short),
            XisfPropertyType.UI16Matrix => sizeof(ushort),
            XisfPropertyType.I32Matrix => sizeof(int),
            XisfPropertyType.UI32Matrix => sizeof(uint),
            XisfPropertyType.I64Matrix => sizeof(long),
            XisfPropertyType.UI64Matrix => sizeof(ulong),
            XisfPropertyType.F32Matrix => sizeof(float),
            XisfPropertyType.F64Matrix => sizeof(double),

            _ => throw new ArgumentException($"Unsupported property type: {type}", nameof(type)),
        };
    }

    internal static byte[] Decode(string text, DataEncoding encoding)
    {
        if (encoding == DataEncoding.Base64)
        {
            return Convert.FromBase64String(text);
        }
        else if (encoding == DataEncoding.Base16)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(text);
            return DecodeBase16(data);
        }
        else if (encoding == DataEncoding.None)
        {
            var data = System.Text.Encoding.UTF8.GetBytes(text);
            return data;
        }
        else
        {
            throw new NotSupportedException($"Unsupported encoding: {encoding}");
        }
    }
    
    private static byte[] DecodeBase16(ReadOnlySpan<byte> encodedData)
    {
        if (encodedData.IsEmpty)
            return [];

        byte[] decodedData = new byte[encodedData.Length * 2];

        for (int i = 0; i < decodedData.Length; i++)
        {
            byte high = HexCharToByte(encodedData[i * 2]);
            byte low = HexCharToByte(encodedData[i * 2 + 1]);
            decodedData[i] = (byte)((high << 4) | low);
        }

        return decodedData;
        
        static byte HexCharToByte(byte c)
        {
            if (c >= '0' && c <= '9') return (byte)(c - '0');
            if (c >= 'a' && c <= 'f') return (byte)(c - 'a' + 10);
            if (c >= 'A' && c <= 'F') return (byte)(c - 'A' + 10);
            throw new FormatException("Invalid hex character");
        }
    }

    internal static byte[] Shuffle(ReadOnlySpan<byte> unshuffled, int itemSize)
    {
        int size = unshuffled.Length;
        byte[] output = GC.AllocateUninitializedArray<byte>(size);
        int numberOfItems = size / itemSize;
        var items = unshuffled[..(numberOfItems * itemSize)];
        int s = numberOfItems * itemSize;

        for (int j = 0; j < itemSize; j++)
        {
            ByteShuffle.ExtractPlane(items, itemSize, j, output.AsSpan(j * numberOfItems, numberOfItems));
        }

        // Copy remaining bytes (size % itemSize)
        int remainder = size % itemSize;
        if (remainder > 0)
        {
            unshuffled.Slice(numberOfItems * itemSize, remainder).CopyTo(output.AsSpan(s));
        }

        return output;
    }

    /// <param name="input">The data to compress.</param>
    /// <param name="codec">The compression codec.</param>
    /// <param name="multithreaded">
    /// Whether the codec may use its own worker threads. Disable when compressing several subblocks in parallel.
    /// </param>
    internal static byte[] Compress(ReadOnlySpan<byte> input, CompressionCodec codec, bool multithreaded = true)
    {
        byte[] output;

        switch (codec)
        {
            case CompressionCodec.Lz4:
                output = new byte[LZ4Codec.MaximumOutputSize(input.Length)];
                int lz4CompressedSize = LZ4Codec.Encode(input, output, LZ4Level.L00_FAST);
                Array.Resize(ref output, lz4CompressedSize);
                break;

            case CompressionCodec.Lz4Hc:
                output = new byte[LZ4Codec.MaximumOutputSize(input.Length)];
                int lz4HcCompressedSize = LZ4Codec.Encode(input, output, LZ4Level.L06_HC);
                Array.Resize(ref output, lz4HcCompressedSize);
                break;

            case CompressionCodec.Zlib:
                using (var ms = new MemoryStream())
                {
                    // Disposing the zlib stream writes the final deflate block and the Adler-32 trailer.
                    using (var zlib = new ZLibStream(ms, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        zlib.Write(input);
                    }

                    output = ms.ToArray();
                }
                break;

            case CompressionCodec.Zstd:
                using (var zstd = new Compressor())
                {
                    if (multithreaded)
                        zstd.SetParameter(ZstdSharp.Unsafe.ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);

                    output = zstd.Wrap(input).ToArray();
                }
                break;

            default:
                throw new NotImplementedException();
        }

        return output;
    }

    /// <summary>
    /// Decompresses a single subblock into a buffer sized exactly to its uncompressed length.
    /// </summary>
    internal static void DecompressSubblock(ReadOnlySpan<byte> input, Span<byte> output, CompressionCodec codec)
    {
        int decompressedSize;

        switch (codec)
        {
            case CompressionCodec.None:
                if (input.Length != output.Length)
                    throw new XisfException($"Stored subblock is {input.Length} bytes, expected {output.Length}.");

                input.CopyTo(output);
                decompressedSize = input.Length;
                break;

            case CompressionCodec.Lz4 or CompressionCodec.Lz4Hc:
                decompressedSize = LZ4Codec.PartialDecode(input, output);
                break;

            case CompressionCodec.Zlib:
                unsafe
                {
                    // UnmanagedMemoryStream to avoid a copy of the input data to a byte[]
                    // because a MemoryStream only works on byte[].
                    fixed (byte* pInput = input)
                    using (var inputStream = new UnmanagedMemoryStream(pInput, input.Length))
                    using (var zlibStream = new ZLibStream(inputStream, CompressionMode.Decompress))
                    {
                        zlibStream.ReadExactly(output);
                    }
                }
                decompressedSize = output.Length;
                break;

            case CompressionCodec.Zstd:
                using (var zstd = new Decompressor())
                {
                    decompressedSize = zstd.Unwrap(input, output);
                }
                break;

            default:
                throw new NotImplementedException($"Compression codec {codec} is not supported");
        }

        if (decompressedSize != output.Length)
            throw new XisfException($"Decompressed subblock is {decompressedSize} bytes, expected {output.Length}.");
    }

    internal static IncrementalHash CreateIncrementalHash(ChecksumAlgorithm algorithm)
    {
        var hashName = algorithm switch
        {
            ChecksumAlgorithm.SHA1 => HashAlgorithmName.SHA1,
            ChecksumAlgorithm.SHA256 => HashAlgorithmName.SHA256,
            ChecksumAlgorithm.SHA512 => HashAlgorithmName.SHA512,
            ChecksumAlgorithm.SHA3_256 => HashAlgorithmName.SHA3_256,
            ChecksumAlgorithm.SHA3_512 => HashAlgorithmName.SHA3_512,
            _ => throw new XisfException($"Unsupported checksum algorithm: {algorithm}"),
        };

        return IncrementalHash.CreateHash(hashName);
    }

    /// <summary>
    /// Gets the algorithm name used in <c>checksum</c> attributes.
    /// </summary>
    internal static string ChecksumAlgorithmName(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.SHA1 => "sha1",
        ChecksumAlgorithm.SHA256 => "sha256",
        ChecksumAlgorithm.SHA512 => "sha512",
        ChecksumAlgorithm.SHA3_256 => "sha3-256",
        ChecksumAlgorithm.SHA3_512 => "sha3-512",
        _ => throw new NotImplementedException($"Unsupported checksum algorithm: {algorithm}"),
    };

    /// <summary>
    /// Gets the length of a digest in hexadecimal characters.
    /// </summary>
    internal static int ChecksumHexLength(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.SHA1 => 40,
        ChecksumAlgorithm.SHA256 or ChecksumAlgorithm.SHA3_256 => 64,
        ChecksumAlgorithm.SHA512 or ChecksumAlgorithm.SHA3_512 => 128,
        _ => throw new NotImplementedException($"Unsupported checksum algorithm: {algorithm}"),
    };

    internal static string ToLowerHex(ReadOnlySpan<byte> bytes)
    {
#if NET10_0_OR_GREATER
        return Convert.ToHexStringLower(bytes);
#else
        return Convert.ToHexString(bytes).ToLowerInvariant();
#endif
    }

    internal static byte[] HashData(ReadOnlySpan<byte> data, ChecksumAlgorithm algorithm)
    {
        return algorithm switch
        {
            ChecksumAlgorithm.None => [],
            ChecksumAlgorithm.SHA1 => SHA1.HashData(data),
            ChecksumAlgorithm.SHA256 => SHA256.HashData(data),
            ChecksumAlgorithm.SHA512 => SHA512.HashData(data),
            ChecksumAlgorithm.SHA3_256 => SHA3_256.HashData(data),
            ChecksumAlgorithm.SHA3_512 => SHA3_512.HashData(data),
            _ => throw new XisfException($"Unsupported checksum algorithm: {algorithm}"),
        };
    }

    internal static string GetChecksumString(ReadOnlySpan<byte> data, ChecksumAlgorithm algorithm)
    {
        byte[] digestBytes = HashData(data, algorithm);
#if NET10_0_OR_GREATER
        return Convert.ToHexStringLower(digestBytes);
#else
        return Convert.ToHexString(digestBytes).ToLowerInvariant();
#endif
    }
}
