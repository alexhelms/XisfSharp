namespace XisfSharp;

public enum PixelStorage
{
    /// <summary>
    /// Each channel is stored in separate contiguous blocks, e.g. RRRGGGBBB.
    /// </summary>
    Planar,

    /// <summary>
    /// Each channel is stored in a continuous interleaved block, e.g. RGBRGBRGB.
    /// </summary>
    Normal,
}
