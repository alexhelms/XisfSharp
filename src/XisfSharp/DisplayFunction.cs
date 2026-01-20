namespace XisfSharp;

public sealed class DisplayFunction
{
    public string? Name { get; }

    /// <summary>
    /// 4-element array of midtone coefficients for an MTF stretch display function.
    /// </summary>
    public double[] Midtones { get; }

    /// <summary>
    /// 4-element array of shadow coefficients for an MTF stretch display function.
    /// </summary>
    public double[] Shadows { get; }

    /// <summary>
    /// 4-element array of highlight coefficients for an MTF stretch display function.
    /// </summary>
    public double[] Highlights { get; }

    /// <summary>
    /// 4-element array of shadow dynamic range expansion coefficients for an MTF stretch display function.
    /// </summary>
    public double[] ShadowDynamicRange { get; }

    /// <summary>
    /// 4-element array of highlight dynamic range expansion coefficients for an MTF stretch display function.
    /// </summary>
    public double[] HighlightDynamicRange { get; }

    /// <summary>
    /// Create a new DisplayFunction class with the specified MTF coefficients.
    /// </summary>
    /// <param name="midtones">A 4-element array of midtone coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    /// <param name="shadows">A 4-element array of shadow coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    /// <param name="highlights">A 4-element array of highlights coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    /// <param name="shadowDynamicRange">A 4-element array of shadow dynamic range expansion coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    /// <param name="highlightDynamicRange">A 4-element array of highlight dynamic range expansion coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    public DisplayFunction(double[] midtones, double[] shadows, double[] highlights, double[] shadowDynamicRange, double[] highlightDynamicRange)
        : this(midtones, shadows, highlights, shadowDynamicRange, highlightDynamicRange, null)
    {
    }

    /// <summary>
    /// Create a new DisplayFunction class with the specified MTF coefficients.
    /// </summary>
    /// <param name="midtones">A 4-element array of midtone coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    /// <param name="shadows">A 4-element array of shadow coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    /// <param name="highlights">A 4-element array of highlights coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    /// <param name="shadowDynamicRange">A 4-element array of shadow dynamic range expansion coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    /// <param name="highlightDynamicRange">A 4-element array of highlight dynamic range expansion coefficients for an MTF stretch display function. Cannot be null and must have a length of 4.</param>
    /// <param name="name">An optional name for the display function. Can be null.</param>
    public DisplayFunction(double[] midtones, double[] shadows, double[] highlights, double[] shadowDynamicRange, double[] highlightDynamicRange, string? name)
    {
        ArgumentNullException.ThrowIfNull(midtones, nameof(midtones));
        ArgumentNullException.ThrowIfNull(shadows, nameof(shadows));
        ArgumentNullException.ThrowIfNull(highlights, nameof(highlights));
        ArgumentNullException.ThrowIfNull(shadowDynamicRange, nameof(shadowDynamicRange));
        ArgumentNullException.ThrowIfNull(highlightDynamicRange, nameof(highlightDynamicRange));
        ArgumentOutOfRangeException.ThrowIfNotEqual(midtones.Length, 4, nameof(midtones));
        ArgumentOutOfRangeException.ThrowIfNotEqual(shadows.Length, 4, nameof(shadows));
        ArgumentOutOfRangeException.ThrowIfNotEqual(highlights.Length, 4, nameof(highlights));
        ArgumentOutOfRangeException.ThrowIfNotEqual(shadowDynamicRange.Length, 4, nameof(shadowDynamicRange));
        ArgumentOutOfRangeException.ThrowIfNotEqual(highlightDynamicRange.Length, 4, nameof(highlightDynamicRange));

        Midtones = midtones;
        Shadows = shadows;
        Highlights = highlights;
        ShadowDynamicRange = shadowDynamicRange;
        HighlightDynamicRange = highlightDynamicRange;
        Name = name;
    }
}
