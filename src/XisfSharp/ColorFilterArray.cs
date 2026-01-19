namespace XisfSharp;

public sealed class ColorFilterArray
{
    public CFAElement[] Pattern { get; }

    public int Width { get; }

    public int Height { get; }

    public string? Name { get; }

    public ColorFilterArray(IEnumerable<CFAElement> pattern, int width, int height)
        : this(pattern, width, height, null)
    {
    }

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
}
