namespace XisfSharp.Properties;

public static class XisfPropertyExtensions
{
    internal static int GetBytesPerElement(this XisfVectorProperty property)
        => XisfUtil.GetBytesPerElement(property.Type);

    internal static int GetBytesPerElement(this XisfMatrixProperty property)
        => XisfUtil.GetBytesPerElement(property.Type);
}