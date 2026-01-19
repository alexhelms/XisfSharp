namespace XisfSharp;

public sealed class DisplayFunction
{
    public string? Name { get; }

    public double[] Midtones { get; }
    
    public double[] Shadows { get; }

    public double[] Highlights { get; }

    public double[] ShadowDynamicRange { get; }

    public double[] HighlightDynamicRange { get; }

    public DisplayFunction(double[] midtones, double[] shadows, double[] highlights, double[] shadowDynamicRange, double[] highlightDynamicRange)
        : this(null, midtones, shadows, highlights, shadowDynamicRange, highlightDynamicRange)
    {
    }

    public DisplayFunction(string? name, double[] midtones, double[] shadows, double[] highlights, double[] shadowDynamicRange, double[] highlightDynamicRange)
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

        Name = name;
        Midtones = midtones;
        Shadows = shadows;
        Highlights = highlights;
        ShadowDynamicRange = shadowDynamicRange;
        HighlightDynamicRange = highlightDynamicRange;
    }
}
