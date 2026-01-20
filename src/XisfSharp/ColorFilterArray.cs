namespace XisfSharp;

public sealed class ColorFilterArray
{
    /// <summary>
    /// Get the CFA pattern elements.
    /// Ordered top-down, left-right. E.g. RGGB.
    /// </summary>
    public CFAElement[] Pattern { get; }

    /// <summary>
    /// Width of the CFA matrix.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Height of the CFA matrix.
    /// </summary>
    public int Height { get; }

    public string? Name { get; }

    /// <summary>
    /// Create a new ColorFilterArray with specified pattern, width, and height.
    /// </summary>
    /// <param name="pattern"> The sequence of <see cref="CFAElement"/> values representing the color filter pattern.
    /// The number of elements must equal width multiplied by height and cannot be empty. Ordered top-down, left-right. e.g. RGGB.
    /// </param>
    /// <param name="width">The number of columns in the color filter array. Must be zero or greater.</param>
    /// <param name="height">The number of rows in the color filter array. Must be zero or greater.</param>
    public ColorFilterArray(IEnumerable<CFAElement> pattern, int width, int height)
        : this(pattern, width, height, null)
    {
    }

    /// <summary>
    /// Create a new ColorFilterArray with specified pattern, width, height, and optional name.
    /// </summary>
    /// <param name="pattern"> The sequence of <see cref="CFAElement"/> values representing the color filter pattern.
    /// The number of elements must equal width multiplied by height and cannot be empty. Ordered top-down, left-right. e.g. RGGB.
    /// </param>
    /// <param name="width">The number of columns in the color filter array. Must be zero or greater.</param>
    /// <param name="height">The number of rows in the color filter array. Must be zero or greater.</param>
    /// <param name="name">An optional name for the color filter array, or null to omit a name.</param>
    public ColorFilterArray(IEnumerable<CFAElement> pattern, int width, int height, string? name)
    {
        Pattern = pattern.ToArray();
        Width = width;
        Height = height;
        Name = name;

        ArgumentOutOfRangeException.ThrowIfNegative(width);
        ArgumentOutOfRangeException.ThrowIfNegative(height);

        if (Pattern.Length == 0)
        {
            throw new ArgumentException("Pattern cannot be empty.", nameof(pattern));
        }

        if (Pattern.Length != Width * Height)
        {
            throw new ArgumentException("Pattern length does not match width * height.", nameof(pattern));
        }
    }

    /// <summary>
    /// Create a new ColorFilterArray with specified pattern, width, height, and optional name.
    /// </summary>
    /// <param name="pattern"> The sequence of CFAElement values as a string representing the color filter pattern.
    /// The number of elements must equal width multiplied by height and cannot be empty. Ordered top-down, left-right. e.g. RGGB.
    /// </param>
    /// <param name="width">The number of columns in the color filter array. Must be zero or greater.</param>
    /// <param name="height">The number of rows in the color filter array. Must be zero or greater.</param>
    public ColorFilterArray(string pattern, int width, int height)
        : this(ConvertPattern(pattern), width, height, null)
    {
    }

    /// <summary>
    /// Create a new ColorFilterArray with specified pattern, width, height, and optional name.
    /// </summary>
    /// <param name="pattern"> The sequence of CFAElement values as a string representing the color filter pattern.
    /// The number of elements must equal width multiplied by height and cannot be empty. Ordered top-down, left-right. e.g. RGGB.
    /// </param>
    /// <param name="width">The number of columns in the color filter array. Must be zero or greater.</param>
    /// <param name="height">The number of rows in the color filter array. Must be zero or greater.</param>
    /// <param name="name">An optional name for the color filter array, or null to omit a name.</param>
    public ColorFilterArray(string pattern, int width, int height, string? name)
        : this(ConvertPattern(pattern), width, height, name)
    {
    }

    private static IEnumerable<CFAElement> ConvertPattern(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        foreach (var c in pattern)
        {
            CFAElement element = c switch
            {
                'R' or 'r' => CFAElement.R,
                'G' or 'g' => CFAElement.G,
                'B' or 'b' => CFAElement.B,
                'W' or 'w' => CFAElement.W,
                'C' or 'c' => CFAElement.C,
                'M' or 'm' => CFAElement.M,
                'Y' or 'y' => CFAElement.Y,
                _ => throw new ArgumentException($"Invalid character '{c}' in pattern string.", nameof(pattern)),
            };
            yield return element;
        }
    }
}
