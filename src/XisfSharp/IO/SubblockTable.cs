using System.Globalization;

namespace XisfSharp.IO;

/// <summary>
/// Describes one independently compressed subblock of a compressed XISF data block.
/// </summary>
/// <param name="CompressedOffset">Offset of the subblock within the compressed block.</param>
/// <param name="CompressedSize">Size in bytes of the subblock as stored.</param>
/// <param name="UncompressedOffset">Offset of the subblock within the uncompressed block.</param>
/// <param name="UncompressedSize">Size in bytes of the subblock after decompression.</param>
internal readonly record struct Subblock(
    long CompressedOffset,
    long CompressedSize,
    long UncompressedOffset,
    long UncompressedSize)
{
    /// <summary>
    /// Gets a value indicating whether the subblock is stored verbatim rather than compressed.
    /// </summary>
    /// <remarks>
    /// PixInsight stores a subblock uncompressed when the codec cannot compress it, for example zlib
    /// subblocks close to 4 GiB. Such subblocks are identified by equal compressed and uncompressed sizes.
    /// </remarks>
    public bool IsStored => CompressedSize == UncompressedSize;
}

/// <summary>
/// The ordered list of subblocks that make up a compressed XISF data block.
/// </summary>
/// <remarks>
/// Subblocks are compressed independently and stored contiguously. A compressed block without a
/// <c>subblocks</c> attribute is treated as a single subblock spanning the whole block.
/// </remarks>
internal sealed class SubblockTable
{
    private readonly Subblock[] _subblocks;

    private SubblockTable(Subblock[] subblocks)
    {
        _subblocks = subblocks;

        foreach (var subblock in subblocks)
        {
            CompressedSize += subblock.CompressedSize;
            UncompressedSize += subblock.UncompressedSize;
            MaxUncompressedSubblockSize = Math.Max(MaxUncompressedSubblockSize, subblock.UncompressedSize);
        }
    }

    public IReadOnlyList<Subblock> Subblocks => _subblocks;

    public int Count => _subblocks.Length;

    public Subblock this[int index] => _subblocks[index];

    /// <summary>
    /// Gets the total size in bytes of all subblocks as stored.
    /// </summary>
    public long CompressedSize { get; }

    /// <summary>
    /// Gets the total size in bytes of all subblocks after decompression.
    /// </summary>
    public long UncompressedSize { get; }

    /// <summary>
    /// Gets the size in bytes of the largest subblock after decompression.
    /// </summary>
    public long MaxUncompressedSubblockSize { get; }

    public static SubblockTable Single(long compressedSize, long uncompressedSize)
    {
        return new([new Subblock(0, compressedSize, 0, uncompressedSize)]);
    }

    /// <summary>
    /// Parses the value of a <c>subblocks</c> attribute with the syntax <c>c1,u1:c2,u2:...:cN,uN</c>.
    /// </summary>
    public static SubblockTable Parse(string value)
    {
        var entries = value.Split(':', StringSplitOptions.TrimEntries);
        var subblocks = new Subblock[entries.Length];
        long compressedOffset = 0;
        long uncompressedOffset = 0;

        for (int i = 0; i < entries.Length; i++)
        {
            var sizes = entries[i].Split(',', StringSplitOptions.TrimEntries);
            if (sizes.Length != 2
                || !long.TryParse(sizes[0], NumberStyles.None, CultureInfo.InvariantCulture, out long compressedSize)
                || !long.TryParse(sizes[1], NumberStyles.None, CultureInfo.InvariantCulture, out long uncompressedSize)
                || compressedSize <= 0
                || uncompressedSize <= 0)
            {
                throw new XisfException($"Invalid subblocks attribute: {value}");
            }

            subblocks[i] = new Subblock(compressedOffset, compressedSize, uncompressedOffset, uncompressedSize);
            compressedOffset = checked(compressedOffset + compressedSize);
            uncompressedOffset = checked(uncompressedOffset + uncompressedSize);
        }

        return new SubblockTable(subblocks);
    }

    /// <summary>
    /// Ensures the subblock sizes add up to the sizes declared for the block.
    /// </summary>
    public void Validate(long compressedSize, long uncompressedSize)
    {
        if (CompressedSize != compressedSize)
            throw new XisfException($"Subblock compressed sizes add up to {CompressedSize} bytes, expected {compressedSize}.");

        if (UncompressedSize != uncompressedSize)
            throw new XisfException($"Subblock uncompressed sizes add up to {UncompressedSize} bytes, expected {uncompressedSize}.");
    }

    /// <summary>
    /// Returns the index of the subblock that contains the given offset into the uncompressed block.
    /// </summary>
    public int IndexOfUncompressedOffset(long offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(offset, UncompressedSize);

        int lo = 0;
        int hi = _subblocks.Length - 1;
        while (lo < hi)
        {
            int mid = lo + (hi - lo + 1) / 2;
            if (_subblocks[mid].UncompressedOffset <= offset)
                lo = mid;
            else
                hi = mid - 1;
        }

        return lo;
    }
}
