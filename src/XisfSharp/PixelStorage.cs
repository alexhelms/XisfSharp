namespace XisfSharp;

public enum PixelStorage
{
    /// <summary>
    /// Each channel are stored in separate contiguous blocks, e.g. RRRGGGBBB.
    /// </summary>
    Planar,

    /// <summary>
    /// Each channel are stored in a continuous interleaved block, e.g. RGBRGBRGB.
    /// </summary>
    Normal,
}
