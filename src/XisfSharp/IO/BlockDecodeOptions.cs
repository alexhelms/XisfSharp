namespace XisfSharp.IO;

/// <summary>
/// Tuning parameters for decoding data blocks.
/// </summary>
internal sealed record BlockDecodeOptions
{
    /// <summary>
    /// The smallest supported chunk size: eight items of the largest item size.
    /// </summary>
    public const int MinimumChunkSize = 64;

    public static BlockDecodeOptions Default { get; } = new();

    /// <summary>
    /// Gets the number of decoded bytes produced per step. Also bounds the per-lane read buffers.
    /// </summary>
    public int ChunkSize { get; init; } = 4 * 1024 * 1024;

    /// <summary>
    /// Gets the memory, in bytes, that decoding LZ4 blocks may use for decoded subblocks.
    /// </summary>
    /// <remarks>
    /// LZ4 subblocks cannot be decoded incrementally, so every lane holds a whole decoded subblock.
    /// When the lanes would need more than this limit, the block is first decompressed to a temporary
    /// file and the lanes read from it instead, which holds one subblock only while the stream opens.
    /// </remarks>
    public long LaneMemoryLimit { get; init; } = 1L << 30;

    /// <summary>
    /// Gets the directory for temporary files, or null to use <see cref="Path.GetTempPath"/>.
    /// </summary>
    public string? TempDirectory { get; init; }
}
