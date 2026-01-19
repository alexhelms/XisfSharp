namespace XisfSharp;

public enum ImageOrientation
{
    /// <summary>
    /// The default image orientation should be preserved, so do nothing.
    /// </summary>
    Default,

    /// <summary>
    /// Flip (reflect) the image horizontally.
    /// </summary>
    Flip,

    /// <summary>
    /// Rotate the image by 90 degrees, counter-clockwise direction.
    /// </summary>
    Rotate90,

    /// <summary>
    /// Rotate the image by 90 degrees in the counter-clockwise direction, then flip it horizontally.
    /// </summary>
    Rotate90Flip,

    /// <summary>
    /// Rotate the image by 90 degrees, clockwise direction.
    /// </summary>
    RotateMinus90,

    /// <summary>
    /// Rotate the image by 90 degrees in the clockwise direction, then flip it horizontally.
    /// </summary>
    RotateMinus90Flip,

    /// <summary>
    /// Rotate the image by 180 degrees.
    /// </summary>
    Rotate180,

    /// <summary>
    /// Rotate the image by 180 degrees, then flip it horizontally (equivalent to a vertical reflection).
    /// </summary>
    Rotate180Flip,
}
