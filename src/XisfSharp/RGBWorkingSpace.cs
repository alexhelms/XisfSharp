namespace XisfSharp;

public sealed class RGBWorkingSpace
{
    public string? Name { get; }

    /// <summary>
    /// Plain-text representation of a floating point number greater than 0, or "sRGB".
    /// </summary>
    public string Gamma { get; }

    /// <summary>
    /// 3-element array, x_R, x_G, x_B chromaticity coordinates.
    /// </summary>
    public double[] ChromaticityX { get; }

    /// <summary>
    /// 3-element array, y_R, y_G, y_B chromaticity coordinates.
    /// </summary>
    public double[] ChromaticityY { get; }

    /// <summary>
    /// 3-element array, Y_R, Y_G, Y_B luminance values.
    /// </summary>
    public double[] Luminance { get; }

    /// <summary>
    /// Create a new RGBWorkingSpace with chromaticity and luminance coefficients.
    /// </summary>
    /// <param name="gamma">Plain-text representation of a floating point number greater than 0, or "sRGB".</param>
    /// <param name="chromaticityX">3-element array, x_R, x_G, x_B chromaticity coordinates. Cannot be null and must have a length of 3.</param>
    /// <param name="chromaticityY">3-element array, y_R, y_G, y_B chromaticity coordinates. Cannot be null and must have a length of 3.</param>
    /// <param name="luminance">3-element array, Y_R, Y_G, Y_B luminance coordinates. Cannot be null and must have a length of 3.</param>
    public RGBWorkingSpace(string gamma, double[] chromaticityX, double[] chromaticityY, double[] luminance)
        : this(gamma, chromaticityX, chromaticityY, luminance, null)
    {
    }

    /// <summary>
    /// Create a new RGBWorkingSpace with chromaticity and luminance coefficients.
    /// </summary>
    /// <param name="gamma">Plain-text representation of a floating point number greater than 0, or "sRGB".</param>
    /// <param name="chromaticityX">3-element array, x_R, x_G, x_B chromaticity coordinates. Cannot be null and must have a length of 3.</param>
    /// <param name="chromaticityY">3-element array, y_R, y_G, y_B chromaticity coordinates. Cannot be null and must have a length of 3.</param>
    /// <param name="luminance">3-element array, Y_R, Y_G, Y_B luminance coordinates. Cannot be null and must have a length of 3.</param>
    /// <param name="name">An optional name for the RBB working space. Can be null.</param>
    public RGBWorkingSpace(string gamma, double[] chromaticityX, double[] chromaticityY, double[] luminance, string? name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gamma, nameof(gamma));
        ArgumentNullException.ThrowIfNull(chromaticityX, nameof(chromaticityX));
        ArgumentNullException.ThrowIfNull(chromaticityY, nameof(chromaticityY));
        ArgumentNullException.ThrowIfNull(luminance, nameof(luminance));
        ArgumentOutOfRangeException.ThrowIfNotEqual(chromaticityX.Length, 3, nameof(chromaticityX));
        ArgumentOutOfRangeException.ThrowIfNotEqual(chromaticityY.Length, 3, nameof(chromaticityY));
        ArgumentOutOfRangeException.ThrowIfNotEqual(luminance.Length, 3, nameof(luminance));

        Name = name;
        Gamma = gamma;
        ChromaticityX = chromaticityX;
        ChromaticityY = chromaticityY;
        Luminance = luminance;
    }
}
