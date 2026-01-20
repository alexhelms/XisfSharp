namespace XisfSharp;

public sealed class Resolution
{
    public double Horizontal { get; }

    public double Vertical { get; }

    public ResolutionUnit? Unit { get; }

    public Resolution(double horizontal, double vertical)
        : this(horizontal, vertical, null)
    {
    }

    public Resolution(double horizontal, double vertical, ResolutionUnit? unit)
    {
        Horizontal = horizontal;
        Vertical = vertical;
        Unit = unit;
    }
}
