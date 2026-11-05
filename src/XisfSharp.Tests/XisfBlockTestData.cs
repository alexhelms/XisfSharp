using System.Buffers.Binary;
using System.IO.Compression;
using K4os.Compression.LZ4;
using ZstdSharp;

namespace XisfSharp.Tests;

/// <summary>
/// Builds XISF units with hand-encoded data blocks.
/// </summary>
/// <remarks>
/// Shuffling, compression and byte order are implemented here independently of XisfSharp
/// so that decoding tests do not share an implementation with the code under test.
/// </remarks>
internal static class XisfBlockTestData
{
    public const int AttachmentPosition = 4096;

    public const string Namespace = "http://www.pixinsight.com/xisf";

    /// <summary>
    /// Creates a monolithic XISF unit containing the given elements, with an optional attachment at <see cref="AttachmentPosition"/>.
    /// </summary>
    public static MemoryStream CreateUnit(string elementsXml, byte[]? attachment = null)
    {
        string xml = $"""<?xml version="1.0" encoding="UTF-8"?><xisf version="1.0" xmlns="{Namespace}">{elementsXml}</xisf>""";
        byte[] xmlBytes = System.Text.Encoding.UTF8.GetBytes(xml);

        int headerSize = 16 + xmlBytes.Length;
        int dataStart = attachment is null ? headerSize : AttachmentPosition;
        if (headerSize > dataStart)
            throw new InvalidOperationException("Test header does not fit before the attachment position.");

        byte[] unit = new byte[dataStart + (attachment?.Length ?? 0)];
        "XISF0100"u8.CopyTo(unit);
        BinaryPrimitives.WriteUInt32LittleEndian(unit.AsSpan(8, 4), (uint)xmlBytes.Length);
        xmlBytes.CopyTo(unit, 16);
        attachment?.CopyTo(unit, dataStart);

        return new MemoryStream(unit);
    }

    /// <summary>
    /// Creates little-endian sample data forming a smooth ramp, which compresses well.
    /// </summary>
    public static byte[] CreateRamp(SampleFormat sampleFormat, int count)
    {
        int size = SampleSize(sampleFormat);
        byte[] data = new byte[count * size];

        for (int i = 0; i < count; i++)
        {
            var sample = data.AsSpan(i * size, size);
            switch (sampleFormat)
            {
                case SampleFormat.UInt8: sample[0] = (byte)(i / 7); break;
                case SampleFormat.UInt16: BinaryPrimitives.WriteUInt16LittleEndian(sample, (ushort)(i * 3)); break;
                case SampleFormat.UInt32: BinaryPrimitives.WriteUInt32LittleEndian(sample, (uint)i * 100_003u); break;
                case SampleFormat.Float32: BinaryPrimitives.WriteSingleLittleEndian(sample, i * 0.25f); break;
                case SampleFormat.Float64: BinaryPrimitives.WriteDoubleLittleEndian(sample, i * 0.125 - 17.5); break;
                default: throw new ArgumentOutOfRangeException(nameof(sampleFormat));
            }
        }

        return data;
    }

    public static int SampleSize(SampleFormat sampleFormat) => sampleFormat switch
    {
        SampleFormat.UInt8 => 1,
        SampleFormat.UInt16 => 2,
        SampleFormat.UInt32 => 4,
        SampleFormat.Float32 => 4,
        SampleFormat.Float64 => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(sampleFormat)),
    };

    public static byte[] ReverseElements(byte[] data, int elementSize)
    {
        byte[] reversed = (byte[])data.Clone();
        for (int i = 0; i < reversed.Length; i += elementSize)
        {
            reversed.AsSpan(i, elementSize).Reverse();
        }
        return reversed;
    }

    /// <summary>
    /// Byte shuffling transform from the XISF specification: byte j of every item, for each j in turn,
    /// followed by the trailing bytes that do not form a complete item.
    /// </summary>
    public static byte[] Shuffle(byte[] data, int itemSize)
    {
        byte[] shuffled = new byte[data.Length];
        int numberOfItems = data.Length / itemSize;
        int s = 0;

        for (int j = 0; j < itemSize; j++)
        {
            for (int i = 0; i < numberOfItems; i++)
            {
                shuffled[s++] = data[i * itemSize + j];
            }
        }

        data.AsSpan(numberOfItems * itemSize).CopyTo(shuffled.AsSpan(s));
        return shuffled;
    }

    public static byte[] Compress(byte[] data, CompressionCodec codec)
    {
        switch (codec)
        {
            case CompressionCodec.Zlib:
                using (var output = new MemoryStream())
                {
                    using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
                    {
                        zlib.Write(data);
                    }
                    return output.ToArray();
                }

            case CompressionCodec.Lz4:
            case CompressionCodec.Lz4Hc:
                byte[] target = new byte[LZ4Codec.MaximumOutputSize(data.Length)];
                var level = codec == CompressionCodec.Lz4 ? LZ4Level.L00_FAST : LZ4Level.L06_HC;
                int size = LZ4Codec.Encode(data, target, level);
                return target[..size];

            case CompressionCodec.Zstd:
                using (var compressor = new Compressor())
                {
                    return compressor.Wrap(data).ToArray();
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(codec));
        }
    }

    public static string CodecName(CompressionCodec codec) => codec switch
    {
        CompressionCodec.Zlib => "zlib",
        CompressionCodec.Lz4 => "lz4",
        CompressionCodec.Lz4Hc => "lz4hc",
        CompressionCodec.Zstd => "zstd",
        _ => throw new ArgumentOutOfRangeException(nameof(codec)),
    };

    /// <summary>
    /// Splits <paramref name="data"/> at the given lengths, compresses each piece independently and
    /// returns the concatenated block with its <c>subblocks</c> attribute value.
    /// </summary>
    /// <param name="storedIndices">Indices of subblocks to store verbatim instead of compressing.</param>
    public static (byte[] Block, string Subblocks) CompressSubblocks(
        byte[] data, CompressionCodec codec, int[] subblockLengths, params int[] storedIndices)
    {
        if (subblockLengths.Sum() != data.Length)
            throw new ArgumentException("Subblock lengths must add up to the data length.", nameof(subblockLengths));

        using var block = new MemoryStream();
        var entries = new List<string>();
        int offset = 0;

        for (int i = 0; i < subblockLengths.Length; i++)
        {
            byte[] piece = data.AsSpan(offset, subblockLengths[i]).ToArray();
            byte[] encoded = storedIndices.Contains(i) ? piece : Compress(piece, codec);

            if (!storedIndices.Contains(i) && encoded.Length == piece.Length)
                throw new InvalidOperationException("Compressed subblock has the same size as its input and would read as stored.");

            block.Write(encoded);
            entries.Add($"{encoded.Length},{piece.Length}");
            offset += subblockLengths[i];
        }

        return (block.ToArray(), string.Join(':', entries));
    }
}
